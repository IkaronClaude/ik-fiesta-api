using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Fiesta.Api.Db;
using Fiesta.Api.Models;

namespace Fiesta.Api.Services;

/// <summary>
/// Admin-issued API keys for headless / CLI access. The plaintext key is shown
/// once at creation; only its SHA-256 hash is stored, so a leaked DB can't be
/// used to recover working keys. Validating a key bypasses the registration
/// captcha (see AccountEndpoints).
/// </summary>
public class ApiKeyService
{
    private const string Prefix = "fk_";   // "fiesta key"
    private readonly DbConnectionFactory _db;

    public ApiKeyService(DbConnectionFactory db) => _db = db;

    public async Task<ApiKeyCreatedResponse> CreateAsync(string label)
    {
        var key  = Prefix + Base64Url(RandomNumberGenerator.GetBytes(32));
        var hash = Sha256Hex(key);

        await using var conn = await _db.OpenAccountAsync();
        await using var cmd = new SqlCommand(
            "INSERT INTO tApiKey (sLabel, sKeyHash) OUTPUT INSERTED.nKeyNo VALUES (@label, @hash)", conn);
        cmd.Parameters.AddWithValue("@label", label);
        cmd.Parameters.AddWithValue("@hash", hash);
        var keyNo = (int)(await cmd.ExecuteScalarAsync())!;

        return new ApiKeyCreatedResponse(keyNo, label, key);
    }

    /// <summary>True if the presented key matches a live (non-revoked) key. Stamps last-used.</summary>
    public async Task<bool> ValidateAsync(string? presentedKey)
    {
        if (string.IsNullOrWhiteSpace(presentedKey)) return false;
        var hash = Sha256Hex(presentedKey.Trim());

        await using var conn = await _db.OpenAccountAsync();
        await using var cmd = new SqlCommand("""
            UPDATE tApiKey SET dLastUsed = GETDATE() WHERE sKeyHash = @hash AND bRevoked = 0;
            SELECT @@ROWCOUNT;
            """, conn);
        cmd.Parameters.AddWithValue("@hash", hash);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    public async Task<List<ApiKeyResponse>> ListAsync()
    {
        var list = new List<ApiKeyResponse>();
        await using var conn = await _db.OpenAccountAsync();
        await using var cmd = new SqlCommand(
            "SELECT nKeyNo, sLabel, dCreated, dLastUsed, bRevoked FROM tApiKey ORDER BY nKeyNo", conn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new ApiKeyResponse(
                r.GetInt32(0), r.GetString(1), r.GetDateTime(2),
                r.IsDBNull(3) ? null : r.GetDateTime(3), r.GetBoolean(4)));
        return list;
    }

    /// <summary>Revoke a key by id. Returns false if no such key.</summary>
    public async Task<bool> RevokeAsync(int keyNo)
    {
        await using var conn = await _db.OpenAccountAsync();
        await using var cmd = new SqlCommand(
            "UPDATE tApiKey SET bRevoked = 1 WHERE nKeyNo = @id AND bRevoked = 0", conn);
        cmd.Parameters.AddWithValue("@id", keyNo);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static string Sha256Hex(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();

    private static string Base64Url(byte[] b) =>
        Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

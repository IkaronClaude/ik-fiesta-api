namespace Fiesta.Api.Models;

public record CreateApiKeyRequest(string Label);

/// <summary>Returned once, at creation — carries the plaintext key.</summary>
public record ApiKeyCreatedResponse(int KeyNo, string Label, string Key);

/// <summary>Listing shape — metadata only, never the key.</summary>
public record ApiKeyResponse(int KeyNo, string Label, DateTime Created, DateTime? LastUsed, bool Revoked);

using Microsoft.Data.SqlClient;

namespace Fiesta.Api.Db;

public static class DbInit
{
    public static async Task EnsureCreatedAsync(IServiceProvider services)
    {
        var factory = services.GetRequiredService<DbConnectionFactory>();
        await using var conn = await factory.OpenAccountAsync();

        await using var cmd = new SqlCommand("""
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tWebCredential')
            CREATE TABLE tWebCredential (
                nUserNo       int          NOT NULL PRIMARY KEY,
                sPasswordHash nvarchar(80) NOT NULL,
                dCreated      datetime     NOT NULL DEFAULT GETDATE(),
                dLastLogin    datetime     NULL,
                CONSTRAINT FK_WebCred_User FOREIGN KEY (nUserNo) REFERENCES tUser(nUserNo)
            );

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tApiKey')
            CREATE TABLE tApiKey (
                nKeyNo    int           IDENTITY(1,1) NOT NULL PRIMARY KEY,
                sLabel    nvarchar(100) NOT NULL,
                sKeyHash  varchar(64)   NOT NULL UNIQUE,  -- SHA-256 hex of the key; the key itself is never stored
                dCreated  datetime      NOT NULL DEFAULT GETDATE(),
                dLastUsed datetime      NULL,
                bRevoked  bit           NOT NULL DEFAULT 0
            );
            """, conn);

        await cmd.ExecuteNonQueryAsync();
    }
}

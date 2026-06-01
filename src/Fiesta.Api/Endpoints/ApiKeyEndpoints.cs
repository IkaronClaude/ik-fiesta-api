using Fiesta.Api.Models;
using Fiesta.Api.Services;

namespace Fiesta.Api.Endpoints;

public static class ApiKeyEndpoints
{
    public static void MapApiKeyEndpoints(this WebApplication app)
    {
        // POST /api/apikeys — mint a key (admin only). The plaintext key is in the
        // response ONCE; only its hash is stored.
        app.MapPost("/api/apikeys", async (CreateApiKeyRequest req, ApiKeyService keys) =>
        {
            if (string.IsNullOrWhiteSpace(req.Label))
                return Results.BadRequest(new { error = "Label is required." });
            var created = await keys.CreateAsync(req.Label.Trim());
            return Results.Created($"/api/apikeys/{created.KeyNo}", created);
        })
        .WithTags("Admin")
        .WithSummary("Create an API key")
        .WithDescription("Admin only. Returns the plaintext key once — store it now; only its hash is kept. "
            + "Send it as the 'X-Api-Key' header to bypass the registration captcha for CLI/automation.")
        .Produces<ApiKeyCreatedResponse>(StatusCodes.Status201Created)
        .RequireAuthorization("admin");

        // GET /api/apikeys — list key metadata (admin only).
        app.MapGet("/api/apikeys", async (ApiKeyService keys) => Results.Ok(await keys.ListAsync()))
            .WithTags("Admin")
            .WithSummary("List API keys")
            .Produces<List<ApiKeyResponse>>()
            .RequireAuthorization("admin");

        // DELETE /api/apikeys/{id} — revoke a key (admin only).
        app.MapDelete("/api/apikeys/{id:int}", async (int id, ApiKeyService keys) =>
            await keys.RevokeAsync(id) ? Results.NoContent() : Results.NotFound())
            .WithTags("Admin")
            .WithSummary("Revoke an API key")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("admin");
    }
}

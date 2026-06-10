using Microsoft.Data.SqlClient;
using Fiesta.Api.Models;
using Fiesta.Api.Services;

namespace Fiesta.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this WebApplication app)
    {
        // POST /api/accounts — create account. Captcha verified if a provider is
        // configured, UNLESS the caller is trusted (admin JWT, or a valid API key).
        app.MapPost("/api/accounts", async (CreateAccountRequest req, HttpContext ctx,
            AccountService accounts, CaptchaService captcha) =>
        {
            // Trusted callers (admin JWT or valid X-Api-Key) skip the captcha AND
            // the register rate limit. Computed once in middleware (Program.cs)
            // and stashed, so the limiter policy and this handler agree.
            bool bypass = ctx.Items.TryGetValue("RlBypass", out var v) && v is true;

            if (!bypass && !await captcha.VerifyAsync(req.CaptchaToken))
                return Results.Json(new { error = "Captcha verification failed." },
                    statusCode: StatusCodes.Status400BadRequest);

            // GM/auth level is honoured ONLY for trusted callers (admin JWT or a
            // valid X-Api-Key — the same `bypass` that skips captcha/rate-limit).
            // Public registrants can't self-grant GM even if they send the field.
            var ingameGmLevel = bypass ? req.IngameGmLevel : null;

            try
            {
                var account = await accounts.CreateAccountAsync(req, ingameGmLevel);
                return Results.Created($"/api/accounts/{account.UserNo}", account);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                return Results.Conflict(new { error = "Username already exists." });
            }
        })
        .WithTags("Accounts")
        .WithSummary("Register a new account")
        .WithDescription("Creates a game account with separate web and in-game passwords. Captcha required if configured, unless the caller presents an admin JWT or a valid X-Api-Key.")
        .Produces<AccountResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status429TooManyRequests)
        .RequireRateLimiting("register");

        // GET /api/accounts/me — own account info (JWT required)
        app.MapGet("/api/accounts/me", async (HttpContext ctx, AccountService accounts) =>
        {
            int userNo = GetUserNo(ctx);
            var account = await accounts.GetAccountAsync(userNo);
            return account is null ? Results.NotFound() : Results.Ok(account);
        })
        .WithTags("Accounts")
        .WithSummary("Get own account info")
        .Produces<AccountResponse>()
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization();

        // GET /api/accounts/me/characters — own character list (JWT required)
        app.MapGet("/api/accounts/me/characters",
            async (HttpContext ctx, CharacterService chars) =>
            {
                int userNo = GetUserNo(ctx);
                var characters = await chars.GetCharactersByUserAsync(userNo);
                return Results.Ok(characters);
            })
            .WithTags("Accounts")
            .WithSummary("List own characters")
            .Produces<List<CharacterResponse>>()
            .RequireAuthorization();

        // GET /api/accounts/me/cash — own premium balance (JWT required)
        app.MapGet("/api/accounts/me/cash", async (HttpContext ctx, AccountService accounts) =>
        {
            int userNo = GetUserNo(ctx);
            var cash = await accounts.GetCashAsync(userNo);
            return cash is null ? Results.NotFound() : Results.Ok(cash);
        })
        .WithTags("Accounts")
        .WithSummary("Get own cash balance")
        .Produces<CashResponse>()
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization();

        // POST /api/accounts/me/cash — add cash to own account (admin required)
        app.MapPost("/api/accounts/me/cash",
            async (AddCashRequest req, HttpContext ctx, AccountService accounts) =>
            {
                int userNo = GetUserNo(ctx);
                var cash = await accounts.AddCashAsync(userNo, req.Amount);
                return cash is null ? Results.NotFound() : Results.Ok(cash);
            })
            .WithTags("Admin")
            .WithSummary("Add cash to own account")
            .WithDescription("Adds premium currency to the authenticated admin's account.")
            .Produces<CashResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("admin");

        // POST /api/accounts/{id}/cash — add cash to any account (admin required)
        app.MapPost("/api/accounts/{id:int}/cash",
            async (int id, AddCashRequest req, AccountService accounts) =>
            {
                var cash = await accounts.AddCashAsync(id, req.Amount);
                return cash is null ? Results.NotFound() : Results.Ok(cash);
            })
            .WithTags("Admin")
            .WithSummary("Add cash to any account")
            .WithDescription("Adds premium currency to the specified account by user ID.")
            .Produces<CashResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .RequireAuthorization("admin");

        // POST /api/accounts/me/inventory — give item to own account (admin required)
        app.MapPost("/api/accounts/me/inventory",
            async (GiveItemRequest req, HttpContext ctx, AccountService accounts) =>
            {
                int userNo = GetUserNo(ctx);
                await accounts.GiveItemAsync(userNo, req.GoodsNo, req.Amount);
                return Results.NoContent();
            })
            .WithTags("Admin")
            .WithSummary("Give item to own account")
            .WithDescription("Inserts a shop item into the authenticated admin's inventory.")
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization("admin");

        // POST /api/accounts/{id}/inventory — give item to any account (admin required)
        app.MapPost("/api/accounts/{id:int}/inventory",
            async (int id, GiveItemRequest req, AccountService accounts) =>
            {
                await accounts.GiveItemAsync(id, req.GoodsNo, req.Amount);
                return Results.NoContent();
            })
            .WithTags("Admin")
            .WithSummary("Give item to any account")
            .WithDescription("Inserts a shop item into the specified account's inventory.")
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization("admin");
    }

    private static int GetUserNo(HttpContext ctx) =>
        int.Parse(ctx.User.FindFirst("userNo")!.Value);
}

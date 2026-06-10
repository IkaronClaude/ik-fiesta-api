namespace Fiesta.Api.Models;

public record CreateAccountRequest(
    string Username,
    string WebPassword,
    string IngamePassword,
    string? Email = null,
    string? CaptchaToken = null,
    // In-game GM/auth level (tUser.nAuthID). HONOURED ONLY for trusted callers
    // (admin JWT or a valid X-Api-Key) — ignored for public registration. Lets
    // automation (e.g. the bot framework) provision a GM-enabled account in one
    // call. Null = leave the SP default (1 = normal player). 9 = admin/GM.
    int? IngameGmLevel = null);

public record LoginRequest(string Username, string Password, string? CaptchaToken = null);

public record LoginResponse(string? Token, bool RequiresCaptcha = false);

public record SetIngamePasswordRequest(string NewPassword);

public record SetWebPasswordRequest(string NewPassword);

public record AddCashRequest(int Amount);

public record AccountResponse(int UserNo, string Username, string? Email, DateTime Created);

public record CashResponse(int UserNo, int Cash);

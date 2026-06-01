# ik-fiesta-api

A standalone REST API for **Fiesta Online** private servers — accounts,
authentication, characters, leaderboard, and a cash/item shop, talking directly
to the game's SQL databases. JWT auth, BCrypt web-password hashing, optional
captcha, and optional Let's Encrypt TLS.

Split out of [Project Mimir](https://github.com/IkaronClaude/ProjectMimir) (the
content toolkit) so it can be deployed, versioned, and scaled on its own. It has
no dependency on the Mimir content tools — it's a plain ASP.NET Core service over
the existing Account/Character databases.


## Endpoints

- `POST /api/accounts`, `GET /api/accounts/me`, `/me/characters`, `/me/cash`,
  `/me/inventory` (+ admin `/{id}/cash`, `/{id}/inventory`)
- `POST /api/auth/login`, `/auth/set-web-password`, `/auth/set-ingame-password`
- `GET /api/leaderboard`, `GET /api/characters/{charNo}`
- `GET /api/shop`, `GET /api/shop/{goodsNo}`
- `GET /api/config` (public site config for the webapp)

Swagger UI is available when `ENABLE_SWAGGER=1`.

## Configuration (env / appsettings)

| Key | Purpose |
| --- | --- |
| `ACCOUNT_CONN` | Connection string to the Account DB (required). |
| `CHARACTER_CONN` | Connection string to the Character DB (required). |
| `JWT_SECRET` | Signing key for issued JWTs (required; use a long random value). |
| `CORS_ORIGINS` | Comma-separated allowed origins (e.g. your webapp URL). |
| `ENABLE_SWAGGER` | `1` to expose Swagger UI. |
| `ASPNETCORE_PATHBASE` | Path prefix when hosted behind a sub-path. |
| `RECAPTCHA_SECRET` / `RECAPTCHA_SITE_KEY` | Google reCAPTCHA (registration). |
| `TURNSTILE_SECRET` / `TURNSTILE_SITE_KEY` | Cloudflare Turnstile (alternative). |
| `LETSENCRYPT_DOMAIN` / `LETSENCRYPT_EMAIL` / `LETSENCRYPT_CERT_DIR` | Auto TLS via ACME. |
| `HTTPS_CERT_PATH` / `HTTPS_CERT_PASSWORD` | Manual PFX cert instead of ACME. |
| `ASPNETCORE_URLS` | Bind addresses (image defaults to `http://+:5000`). |

Secrets belong in env vars / your orchestrator's secret store — never commit them.

## Build & run

```bash
dotnet build ik-fiesta-api.slnx -c Release
ACCOUNT_CONN='Server=...;Database=Account;...' \
CHARACTER_CONN='Server=...;Database=World00_Character;...' \
JWT_SECRET='<long-random>' \
dotnet run --project src/Fiesta.Api
```

## Docker

```bash
docker build -t ik-fiesta-api .                    # Linux
docker build -t ik-fiesta-api -f Dockerfile.windows .   # Windows
docker run -p 5000:5000 \
  -e ACCOUNT_CONN=... -e CHARACTER_CONN=... -e JWT_SECRET=... ik-fiesta-api
```

CI publishes `ghcr.io/<owner>/ik-fiesta-api:latest` on pushes to `main`.

## License

[Apache License 2.0](LICENSE).

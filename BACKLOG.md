# ik-fiesta-api — backlog / future work

Tracked items that aren't started yet. Move to a commit + delete the entry when done.

## Permissions / authorization model for users & API keys  (security)

**Problem.** "Trusted caller" is currently **binary**: an admin JWT (`role=admin`,
i.e. `tUser.nAuthID == 9`) *or any* valid `X-Api-Key` is fully trusted — it skips
the captcha + register rate-limit, and (as of the GM-provisioning change) can set
a new account's in-game GM level (`tUser.nAuthID`) to an arbitrary value. There is
no scoping: every API key can do everything a key can do, and there's no audit of
privilege grants.

**Hard rule this must enforce.** A normal player must *never* be able to elevate
their own (or anyone's) privileges — e.g. a hypothetical `POST /api/me/set-gm-level/99`
must be impossible for a non-privileged caller. Self-service endpoints must never
be a privilege-escalation path. (The current GM-on-create field is gated to trusted
callers only, which holds the line for now, but the model below is what makes it
robust.)

**Wanted.**
- **Scoped API keys.** Add scopes/capabilities to `tApiKey` (e.g.
  `create-account`, `grant-gm`, `give-item`, `read-only`). The create-account
  handler checks for `grant-gm` before honouring `IngameGmLevel`; a plain key
  can register accounts but not mint GMs. Optionally cap the max grantable
  `nAuthID` per scope.
- **RBAC for user roles.** Formalize `nAuthID` tiers (player / GM tiers / admin)
  instead of the magic `== 9` check scattered across the code; centralize the
  "is this caller allowed to do X" decision.
- **Audit trail.** Log every privilege grant (who/what key, target account, old →
  new nAuthID, when) to `AccountLog` or a dedicated table.
- **Key lifecycle.** Per-key rate limits, expiry, and rotation; today a key is
  all-or-nothing and never expires.

**Why now.** Raised 2026-06-10 alongside the bot framework's GM-account
provisioning (ik-fiesta-bots task 18). The GM-on-create field is safe under the
current binary trust model, but that model doesn't scale to multiple integrations
with different trust levels.

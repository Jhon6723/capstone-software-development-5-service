# Security Fixes — PixPro

> Generated: 2026-06-12. All issues verified against the current codebase.
> Fix in priority order. Items 1–4 must be resolved before any production deployment.

---

## 🔴 High-risk (fix before production)

---

### Fix 1 — `IsActive` block check missing in `LoginAsync`

**File:** `src/Services/Auth/Application/Services/Implementations/AuthService.cs` — lines 148–198

**Problem:** After verifying the password, the code immediately generates a JWT with no check on `user.IsActive`. A blocked user can log in normally.

**What to do:**

1. Ensure the `User` entity has the `IsActive` property (DB migration if not already applied):
   ```sql
   ALTER TABLE users ADD COLUMN IF NOT EXISTS is_active BOOLEAN NOT NULL DEFAULT TRUE;
   ```

2. Add the block check in `LoginAsync`, immediately after the password verification succeeds:
   ```csharp
   // After BCrypt.Verify passes:
   if (!user.IsActive)
   {
       return Error.Unauthorized(
           "Auth.UserBlocked",
           "This account has been suspended.");
   }
   ```

3. Also add a lightweight **per-request middleware** that rejects an otherwise-valid JWT if the user is currently blocked (covers tokens issued before the user was blocked):
   - Add an internal endpoint `GET /api/auth/users/{id}/is-active` (internal, not exposed through Gateway) OR
   - Add a middleware in the Gateway that, after JWT validation, calls Auth Service to verify `IsActive` — OR at minimum stores the block in **Redis** so each service can check it without a DB round-trip.
   - The simplest interim solution: reduce JWT expiry (see Fix 5) so the window is short enough to be acceptable without the per-request check.

**Acceptance criteria:** A blocked user gets `401 Auth.UserBlocked` on login. An existing JWT for a blocked user is rejected within the configured expiry window.

---

### Note 3 — NanaBanana: callback authentication (only applies if migrating to webhook)

**Current status: ✅ No attack surface.**

The NanaBanana processor uses **internal polling** — PixPro repeatedly calls `GET /record-info?taskId=` until `successFlag=1`. NanaBanana never calls back to PixPro, so no public endpoint is exposed.

**When it would apply:** If in the future polling is replaced with a callback approach (to eliminate the ~60 iterations every 3s), a public FastAPI endpoint will be exposed that NanaBanana POSTs results to. At that point it must be secured:

- Include a secret in the `callBackUrl`: `?secret=<NANOBANANA_CALLBACK_SECRET>`
- Verify the secret in the handler before processing the payload
- Validate that `resultImageUrl` comes from an expected domain (`nanobananaapi.ai` or Cloudinary)
- Add `NANOBANANA_CALLBACK_SECRET` to `.env` (min 32 chars, generated with `openssl rand -hex 32`)

**Action required now:** None.

---

### Fix 4 — Swagger exposed unconditionally en el Gateway

**File:** `src/Gateway/API/Program.cs` — lines 221–228

**Problem:** `app.UseSwagger()` y `app.UseSwaggerUI()` se invocan sin condición. Los servicios Auth, Projects y Notifications ya protegen Swagger con `if (app.Environment.IsDevelopment())` — el Gateway no lo hace. El schema completo de la API queda público en producción.

**Approach adoptado — separación por archivo de Compose:**

El proyecto ya tiene dos archivos con propósitos distintos:

| Archivo | Entorno | `ASPNETCORE_ENVIRONMENT` | Swagger |
|---------|---------|--------------------------|---------|
| `docker-compose.yml` | **Development** (local) | cambiarlo a `Development` | ✅ activo |
| `docker-compose.coolify.yml` | **Production** (Coolify/Hetzner) | mantener `Production` | ❌ bloqueado |

**Paso 1 — Wrap en `Program.cs` (Gateway):**

```csharp
// Antes (INSEGURO — líneas 221-228):
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "PixPro API Gateway v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "PixPro API Gateway";
});

// Después (SEGURO):
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "PixPro API Gateway v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "PixPro API Gateway";
    });
}
```

**Paso 2 — Cambiar `docker-compose.yml` a `Development`:**

En `docker-compose.yml`, el `gateway`, `auth`, `projects` y `notifications` actualmente tienen `ASPNETCORE_ENVIRONMENT=Production`. Cambiarlo a `Development` para que Swagger quede habilitado en local:

```yaml
# docker-compose.yml — gateway (y repetir en auth, projects, notifications)
environment:
  - ASPNETCORE_ENVIRONMENT=Development   # era Production
```

**Paso 3 — `docker-compose.coolify.yml` no requiere cambios:**

Ya tiene `ASPNETCORE_ENVIRONMENT=Production` en todos los servicios. Con el wrap del Paso 1, Swagger queda automáticamente bloqueado en Coolify sin tocar nada más.

**Resultado final:**

```
docker compose up -d                        → Development → Swagger en /swagger
docker compose -f docker-compose.coolify.yml up -d  → Production  → Swagger 404
```

**Acceptance criteria:** `GET /swagger/index.html` retorna `404` cuando se levanta con `docker-compose.coolify.yml`. Retorna `200` cuando se levanta con `docker-compose.yml` en local.

---

## 🟠 Medium-risk

---

### Fix 5 — JWT expiry is 24 hours with no revocation mechanism

**File:** `src/Services/Auth/Application/Services/Implementations/JwtTokenGenerator.cs` — line 27

**Problem:** Default expiry is 1440 minutes (24 hours). With no per-request `IsActive` check (Fix 1) and no token blacklist, a blocked user's JWT remains valid for up to 24 hours.

**What to do (choose one or both):**

**Option A — Reduce expiry + add refresh tokens (recommended long-term):**
- Reduce JWT access token expiry to 15–30 minutes.
- Issue a separate opaque refresh token (stored in DB or Redis) with a longer TTL (e.g. 7 days).
- Add `POST /api/auth/refresh` endpoint that validates the refresh token and issues a new access token.
- On user block, delete/invalidate the refresh token so the user cannot renew.

**Option B — Redis token blacklist (simpler interim):**
- When a user is blocked, write their `userId` (or `jti` claim) to Redis with a TTL equal to the remaining token lifetime.
- Add middleware that checks Redis on every authenticated request and rejects blacklisted tokens.

**Minimum viable fix (unblock the block feature):**
- Change default expiry from `1440` to `30` in `appsettings` or `.env`:
  ```
  Jwt__ExpirationMinutes=30
  ```
- This alone doesn't require code changes and immediately limits the blast radius of Fix 1.

**Acceptance criteria:** A blocked user's token is rejected within a configurable, short time window. A legitimate user is not logged out unexpectedly during normal use.

---

### Fix 6 — No rate limiting on `/api/auth/login` and `/api/auth/register`

**File:** `src/Gateway/API/Program.cs` and/or `src/Services/Auth/API/Program.cs`

**Problem:** No brute-force or credential-stuffing protection exists on the auth endpoints. BCrypt adds compute delay but does not prevent automated attacks.

**What to do:**

Use ASP.NET Core's built-in rate limiter (available since .NET 7, no extra package needed):

1. In `Gateway/API/Program.cs`, add:
   ```csharp
   builder.Services.AddRateLimiter(options =>
   {
       options.AddFixedWindowLimiter("AuthPolicy", config =>
       {
           config.PermitLimit = 10;           // max 10 requests
           config.Window = TimeSpan.FromMinutes(1);
           config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
           config.QueueLimit = 0;
       });
       options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
   });
   ```

2. Apply the limiter to the YARP routes for auth endpoints in `appsettings.json` (or via code):
   ```csharp
   app.UseRateLimiter();
   ```

3. Alternatively, apply `[EnableRateLimiting("AuthPolicy")]` directly on `AuthController.Login` and `AuthController.Register` in the Auth Service.

**Acceptance criteria:** After 10 login attempts per minute from the same IP, subsequent requests return `429 Too Many Requests` until the window resets.

---

### Fix 7 — `flux-schnell` has no per-user request throttle

**File:** `src/Services/Projects/` — credit check logic (before RabbitMQ publish)

**Problem:** `flux-schnell` bypasses the credit gate entirely and is unlimited by design, but there is no per-user daily cap. A single compromised or malicious account can trigger unlimited Pixazo API calls, with direct cost impact.

**What to do:**

1. Add a `flux_schnell_daily_count` column (or a dedicated Redis key) to track daily usage per user:
   ```sql
   ALTER TABLE user_credits ADD COLUMN flux_daily_count INT NOT NULL DEFAULT 0;
   ALTER TABLE user_credits ADD COLUMN flux_daily_reset_at TIMESTAMPTZ;
   ```

2. In the Projects Service credit check, before publishing `ImageUploadedEvent` for `flux-schnell`:
   - If `flux_daily_reset_at` is null or in the past, reset `flux_daily_count = 0` and set `flux_daily_reset_at = now() + 24h`.
   - If `flux_daily_count >= FLUX_DAILY_LIMIT` (e.g. 200), return `429`.
   - Otherwise increment atomically.

3. Make the daily limit configurable via `appsettings` so it can be adjusted without redeployment.

**Acceptance criteria:** A single user cannot exceed the configured daily `flux-schnell` limit. Admins are exempt (consistent with existing admin bypass logic).

---

### Fix 8 — CORS silently falls back to `localhost:3000` on misconfiguration

**File:** `src/Gateway/API/Program.cs` — lines 184–185

**Problem:** If `Cors:AllowedOrigins` is absent from config, the fallback `new[] { "http://localhost:3000" }` silently takes effect in production, blocking the real frontend with no visible error.

**What to do:**

Fail hard if the config key is missing in non-development environments:

```csharp
// Before:
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };

// After:
string[] allowedOrigins;
if (builder.Environment.IsDevelopment())
{
    allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:3000", "http://localhost:4200" };
}
else
{
    allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? throw new InvalidOperationException("Cors:AllowedOrigins must be configured in production.");
}
```

**Acceptance criteria:** Starting the Gateway in production without `Cors:AllowedOrigins` throws a startup exception rather than silently using a dev fallback.

---

## 🟡 Low-risk / design notes

---

### Note 9 — JWT in WebSocket query string

**File:** `src/Gateway/API/Program.cs` — WebSocket `OnMessageReceived` handler

**Risk:** `?access_token=<jwt>` appears in server access logs, reverse proxy logs, and browser history. This is standard practice for WebSocket auth (browsers cannot send custom headers on WS upgrade), but log scrubbing is required.

**What to do:**
- Configure your logging pipeline to redact the `access_token` query parameter.
- In production, ensure reverse proxy (Nginx / Caddy / Cloudflare) access logs do not store query strings, or use a log filter to strip `access_token=*`.
- No code change needed in the service itself.

---

### Note 10 — IA Service binds to `0.0.0.0` by default

**File:** `src/Services/IA/app/main.py` — line 15

**Risk:** `HOST = os.getenv("HOST", "0.0.0.0")`. In local dev (services on host, not in Docker), the IA FastAPI service is reachable from any interface with no authentication layer — including the local network.

**What to do:**
- Change the default to `127.0.0.1` for local dev:
  ```python
  HOST: str = os.getenv("HOST", "127.0.0.1")
  ```
- In Docker, override with `HOST=0.0.0.0` via the `docker-compose.yml` environment block (this is already effectively isolated by Docker's internal network, but explicit is better).

---

### Note 11 — Admin block enforcement only in Auth Service

**Risk:** `Auth.CannotBlockAdmin` is enforced in the Auth Service, but there is no cross-service guard. A direct DB manipulation on the Projects Service DB could escalate a user's `subscription_tier` to `unlimited` without touching the Auth Service.

**What to do (long-term):**
- Never expose the Projects DB port externally in production (enforce this via Docker Compose `ports` config — do not publish `5433:5432` publicly).
- Consider adding an admin audit log table that records all credit/subscription mutations with the requesting admin's ID.
- For the CannotBlockAdmin logic: rely on the Gateway's JWT-based role check as the single source of truth. No service should trust a `userId` without a validated JWT.

---

---

### Fix 12 — WebSocket admin endpoints have no authorization

**File:** `src/Services/Notifications/API/Controllers/WebSocketController.cs` — lines 70–101

**Problem:** Three endpoints on `WebSocketController` are completely unauthenticated:

| Endpoint | Issue |
|----------|-------|
| `GET /api/websocket/status` | Leaks total active connection count publicly |
| `GET /api/websocket/banned` | **Publicly lists every banned IP** — attacker can check if their own IP is banned and monitor ban effectiveness |
| `DELETE /api/websocket/banned/{ipAddress}` | **Critical: anyone can unban any IP with no token**, completely defeating the IP ban mechanism |

The `connect` endpoint correctly has `[Authorize]`, but these three management endpoints do not.

**What to do:**

`status` is informational — protect it with plain `[Authorize]`:
```csharp
[HttpGet("status")]
[Authorize]
public IActionResult GetStatus() { ... }
```

`banned` (GET) and `banned/{ip}` (DELETE) are operational security controls — they must be admin-only:
```csharp
[HttpGet("banned")]
[Authorize(Policy = "Admin")]
public IActionResult GetBannedIps() { ... }

[HttpDelete("banned/{ipAddress}")]
[Authorize(Policy = "Admin")]
public IActionResult UnbanIp(string ipAddress) { ... }
```

**Acceptance criteria:**
- `GET /api/websocket/status` returns `401` without a valid JWT.
- `GET /api/websocket/banned` and `DELETE /api/websocket/banned/{ip}` return `403` for non-admin users and `401` for unauthenticated requests.
- Admin users can still list and unban IPs normally.

---

## Summary checklist

| # | Severity | File(s) | Status |
|---|----------|---------|--------|
| 1 | 🔴 Critical | `AuthService.cs` | ✅ Done |
| 3 | ✅ N/A | `IA/app/nanobanana_processor.py` — usa polling, no callback | No aplica hoy |
| 4 | 🔴 High | `Gateway/API/Program.cs:221` + `docker-compose.yml` | ✅ Done |
| 12 | 🔴 High | `Notifications/WebSocketController.cs:70-101` | ✅ Done |
| 5 | 🟠 Medium | `JwtTokenGenerator.cs:27` + `appsettings` | ✅ Done |
| 6 | 🟠 Medium | `Auth/API/Controllers/AuthController.cs` | ✅ Done |
| 7 | 🟠 Medium | `Projects Service` credit logic | ✅ Done |
| 8 | 🟠 Medium | `Gateway/API/Program.cs:184` | ✅ Done |
| 9 | 🟡 Low | Logging pipeline config | ⬜ Not done |
| 10 | 🟡 Low | `IA/app/main.py:15` | ⬜ Not done |
| 11 | 🟡 Low | DB access config + audit log | ⬜ Not done |

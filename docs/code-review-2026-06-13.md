# Code Review: Auth Rate Limiting, JWT Blacklist & Flux-Schnell Throttle

**Date:** 2026-06-13
**Commits reviewed:** `HEAD~5..HEAD` (ef050d7, 38826f6, 7ec67cf, 04094e7, 96229a5)
**Reviewer:** AI Assistant

---

## 1. Critical Issues

### 1.1 Race Condition in `CreditService.TryIncrementFluxDailyAsync`

**File:** `src/Services/Projects/Application/Services/CreditService.cs`

Two concurrent requests for the same user can:
- Both read `credit is null`, both insert a new `UserCredit` with `ModelTier.Pixazo`, and one fails with a unique constraint violation on `(UserId, ModelTier)`.
- Both read `FluxDailyCount = 199`, both pass the limit check, and both increment — exceeding the 200 cap.

**Recommended Fix:** Use an atomic database operation (e.g., `INSERT ... ON CONFLICT DO UPDATE` in PostgreSQL, or optimistic concurrency with a row-version/timestamp).

---

### 1.2 Unknown Models Bypass Paid Credits

**File:** `src/Services/Projects/Application/Services/ImageService.cs`

```csharp
if (ModelTierMap.TryGetValue(request.Parameters.Model, out var modelTier))
{
    // paid path
}
else
{
    // ANY unrecognized model falls into the free flux path
    var fluxResult = await _creditService.TryIncrementFluxDailyAsync(...);
}
```

A malicious user can send an arbitrary/unrecognized `Model` value to avoid credit deduction entirely. The `else` branch should explicitly check for `flux-schnell` (or whatever the free model is named), and reject unknown models.

**Recommended Fix:** Only allow the known free model(s) into the flux path; return a validation error for unknown models.

---

### 1.3 `AuthService` Constructor Change Breaks Unit Tests

**File:** `tests/PixPro.Auth.UnitTests/Services/Auth/AuthServiceTests.cs`

The test constructor still calls:
```csharp
_authService = new AuthService(_userRepositoryMock.Object, _jwtTokenGeneratorMock.Object);
```

But `AuthService` now requires `IConnectionMultiplexer` and `IConfiguration`. Tests will fail to compile.

**Recommended Fix:** Update `AuthServiceTests` to mock `IConnectionMultiplexer` and `IConfiguration`, and pass them to the `AuthService` constructor.

---

### 1.4 `ImageServiceTests` Will Throw at Runtime for Flux Paths

**File:** `tests/PixPro.Projects.UnitTests/Services/Projects/ImageServiceTests.cs`

The test setup only mocks `TryDeductCreditAsync`:
```csharp
_creditServiceMock
    .Setup(x => x.TryDeductCreditAsync(...))
    .ReturnsAsync(Result<bool>.Success(true));
```

When the SUT hits the new `else` branch (for models not in `ModelTierMap`), Moq returns `null` for `Task<Result<bool>>`, causing a `NullReferenceException` on `fluxResult.IsSuccess`.

**Recommended Fix:** Add a default mock for `TryIncrementFluxDailyAsync` in the test constructor, or add explicit test cases for the flux-schnell path.

---

## 2. Moderate Issues

### 2.1 Redis Connection Created Synchronously at Startup

**Files:**
- `src/Gateway/API/Program.cs`
- `src/Services/Auth/API/Program.cs`

```csharp
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(redisConnectionString));
```

If Redis is unreachable at startup, the service crashes immediately and will never recover without a restart even after Redis comes back.

**Recommended Fix:** Consider wrapping in a factory or using a lazy initialization pattern so the app can start without Redis and retry connections later.

---

### 2.2 Misleading Documentation: "Sliding 24-Hour Rate Limit"

**Files:**
- `src/Services/Projects/Domain/Entities/UserCredit.cs`
- `src/Services/Projects/Application/Services/CreditService.cs`

`TryIncrementFluxDaily` resets the counter and sets `FluxDailyResetAt = now.AddHours(24)`. This is a **fixed rolling window** (resets 24h after the last reset), not a sliding window.

**Recommended Fix:** Rename the comment/docs to "fixed rolling 24-hour window" to avoid confusion.

---

### 2.3 `Refund` Signature is a Binary-Breaking Change

**File:** `src/Services/Projects/Domain/Entities/UserCredit.cs`

Changing `public void Refund()` to `public void Refund(int amount = 1)` is source-compatible but **binary-breaking**. Any downstream assembly compiled against the old 0-parameter signature must be recompiled.

**Impact:** Low, since this is an internal microservice. If shared libraries are versioned independently, note the breaking change.

---

## 3. Minor / Style Notes

- **`src/Services/Auth/API/Program.cs`** — `app.UseRateLimiter()` is placed after auth/authorization. For unauthenticated endpoints like `login`/`register`, rate limiting still works because ASP.NET Core rate limiters run per-endpoint regardless of auth state, but placing it earlier in the pipeline is more conventional.

- **`src/Services/Projects/Infrastructure/Persistence/Configurations/UserCreditConfiguration.cs`** — `HasColumnType("int")` is used for `FluxDailyCount`, while other integer columns in the same entity use `"integer"`. PostgreSQL aliases them, but consistency is preferred.

---

## Summary Table

| Priority | Issue | Location |
|----------|-------|----------|
| **Critical** | Race condition on flux daily counter | `CreditService.TryIncrementFluxDailyAsync` |
| **Critical** | Unknown models bypass credit deduction | `ImageService.UploadAsync` |
| **Critical** | Auth tests won't compile | `AuthServiceTests.cs` |
| **Critical** | Image tests NRE on flux path | `ImageServiceTests.cs` |
| **Moderate** | Redis startup crash | `Program.cs` (Gateway + Auth) |
| **Moderate** | Misleading "sliding window" docs | `UserCredit.cs`, `CreditService.cs` |

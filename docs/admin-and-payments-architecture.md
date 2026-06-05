## **Admin Features & Payment Service Architecture**

---

## **1. Auth Service — Admin User Management**

### **New: `IsActive` field on `User` entity**

The `User` entity gains an `IsActive` flag to support blocking/unblocking users. A blocked user cannot log in and, if they have an active JWT, is rejected at the middleware level.

**Updated `User` entity (Auth Service — Domain layer):**

```csharp
public sealed class User
{
    public Guid Id { get; private set; }
    public string? Auth0Id { get; private set; }
    public string Email { get; private set; }
    public string Password { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }        // NEW
    public DateTimeOffset CreatedAt { get; private set; }

    public void Block()   => IsActive = false;        // NEW
    public void Unblock() => IsActive = true;         // NEW
}
```

**Default value:** `IsActive = true` on `User` constructor.

**DB migration required:**
```sql
ALTER TABLE users ADD COLUMN is_active BOOLEAN NOT NULL DEFAULT TRUE;
```

### **Block check in `LoginAsync`**

After verifying the password, before generating the JWT:

```
IF user.IsActive == false
    RETURN Error.Unauthorized("Auth.UserBlocked", "This account has been suspended.")
```

### **Block check in JWT middleware**

A blocked user with an existing valid JWT must be rejected. Options:

| Approach | Pros | Cons |
|----------|------|------|
| **DB check on every request** | Immediate effect | Extra DB query per request |
| **Short JWT expiry (e.g. 15 min)** | No extra query | Delay up to expiry window |
| **Token blacklist in Redis** | Immediate + no per-request DB query | Requires Redis + blacklist management |

**Recommended:** DB check on every request for now (simpler, Redis can be added later). Add a lightweight `GET /users/{id}/is-active` internal endpoint or a middleware that hits the Auth DB.

---

### **New Admin Endpoints — Auth Service**

#### `GET /api/auth/users` — List all users (paginated)

```
[Authorize(Policy = "Admin")]
GET /api/auth/users?page=1&pageSize=20&search={email}

Response 200:
{
  "items": [
    {
      "id": "uuid",
      "email": "user@example.com",
      "role": "User",
      "isActive": true,
      "createdAt": "2025-01-01T00:00:00Z"
    }
  ],
  "totalCount": 150,
  "page": 1,
  "pageSize": 20
}
```

#### `PATCH /api/auth/users/{id}/status` — Block or unblock a user

```
[Authorize(Policy = "Admin")]
PATCH /api/auth/users/{id}/status

Body:
{
  "isActive": false   // false = block, true = unblock
}

Response 200:
{
  "id": "uuid",
  "email": "user@example.com",
  "isActive": false,
  "updatedAt": "2025-06-04T00:00:00Z"
}
```

**Error codes:**
| Code | HTTP | Description |
|------|------|-------------|
| `Auth.UserNotFound` | 404 | User ID does not exist |
| `Auth.CannotBlockAdmin` | 400 | Admin cannot block another admin |
| `Auth.CannotBlockSelf` | 400 | Admin cannot block themselves |

---

### **Updated `IAuthService` interface**

```csharp
public interface IAuthService
{
    Task<Result<UserResponse>> RegisterUserAsync(RegisterUserRequest request, CancellationToken ct = default);
    Task<Result<UserResponse>> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    // NEW
    Task<Result<PagedResult<UserResponse>>> GetAllUsersAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Result<UserResponse>> SetUserStatusAsync(Guid userId, bool isActive, Guid requestingAdminId, CancellationToken ct = default);
}
```

### **Block/Unblock Flow**

```mermaid
sequenceDiagram
    participant A as Admin
    participant GW as Gateway (YARP)
    participant AUTH as Auth Service
    participant DB as Auth DB

    A->>GW: PATCH /api/auth/users/{id}/status { isActive: false }
    GW->>AUTH: Route (JWT validated, role=Admin)
    AUTH->>AUTH: Guard: cannot block self or another admin
    AUTH->>DB: UPDATE users SET is_active = false WHERE id = ?
    DB-->>AUTH: 1 row affected
    AUTH-->>A: 200 { id, email, isActive: false }

    Note over AUTH,DB: Next login attempt by blocked user
    A->>AUTH: POST /api/auth/login
    AUTH->>DB: SELECT * FROM users WHERE email = ?
    DB-->>AUTH: user (is_active = false)
    AUTH-->>A: 401 Auth.UserBlocked
```

---

## **2. Projects Service — Admin Subscription Management**

Until the Payment Service is live, the admin can manually assign or change a user's subscription tier.

### **New Admin Endpoint — Projects Service**

#### `PATCH /api/projects/admin/users/{userId}/subscription` — Assign subscription manually

```
[Authorize(Policy = "Admin")]
PATCH /api/projects/admin/users/{userId}/subscription

Body:
{
  "subscriptionTier": "pro",
  "creditsLow": 100,
  "creditsKontext": 50,
  "creditsHigh": 10,
  "resetAt": "2025-07-04T00:00:00Z"   // null for unlimited tier
}

Response 200:
{
  "userId": "uuid",
  "subscriptionTier": "pro",
  "credits": {
    "gpt_mini_low": 100,
    "kontext": 50,
    "gpt_mini_high": 10
  },
  "resetAt": "2025-07-04T00:00:00Z"
}
```

#### `GET /api/projects/admin/users/{userId}/credits` — View user credit balance

```
[Authorize(Policy = "Admin")]
GET /api/projects/admin/users/{userId}/credits

Response 200:
{
  "userId": "uuid",
  "subscriptionTier": "free",
  "credits": [
    { "modelTier": "gpt_mini_low",  "remaining": 3, "total": 5 },
    { "modelTier": "kontext",       "remaining": 1, "total": 3 },
    { "modelTier": "gpt_mini_high", "remaining": 0, "total": 1 }
  ],
  "resetAt": null
}
```

---

## **3. Payment Service — Autonomous Microservice**

### **Overview**

The Payment Service is a fully autonomous microservice. It:
1. Receives webhooks from a payment provider (Stripe or MercadoPago)
2. Validates and records the payment
3. Publishes a `SubscriptionActivatedEvent` to RabbitMQ

The Projects Service consumes that event and activates the subscription with no human intervention. No service calls another directly — everything is event-driven.

### **Tech Stack**

| Concern | Choice |
|---------|--------|
| Language | .NET (consistent with Auth and Projects) |
| Framework | ASP.NET Core (webhook endpoint) |
| DB | PostgreSQL (payment records) |
| Messaging | RabbitMQ (publish `payment-events` exchange) |
| Provider | Stripe or MercadoPago (TBD) |

### **Project Structure**

```
src/Services/Payment/
├── API/
│   ├── Controllers/
│   │   └── WebhookController.cs       # Receives provider webhooks
│   └── Program.cs
├── Application/
│   ├── Services/
│   │   ├── Interfaces/
│   │   │   └── IPaymentService.cs
│   │   └── Implementations/
│   │       └── PaymentService.cs
│   └── DTOs/
│       ├── StripeWebhookPayload.cs
│       └── MercadoPagoWebhookPayload.cs
├── Domain/
│   ├── Entities/
│   │   └── PaymentRecord.cs
│   └── Enums/
│       ├── PaymentStatus.cs
│       └── SubscriptionTier.cs
├── Infrastructure/
│   ├── Database/
│   │   └── PaymentDbContext.cs
│   └── Messaging/
│       └── RabbitMQPublisher.cs
└── alembic/                           # or EF migrations
```

### **Database Table: `payment_records`**

```sql
payment_records
├── id                  UUID            PRIMARY KEY
├── user_id             UUID            NOT NULL
├── provider            VARCHAR(50)     NOT NULL   (stripe | mercadopago)
├── provider_payment_id VARCHAR(255)    NOT NULL   UNIQUE
├── subscription_tier   VARCHAR(50)     NOT NULL   (basic | pro | unlimited)
├── amount              DECIMAL(10,2)   NOT NULL
├── currency            VARCHAR(10)     NOT NULL
├── status              VARCHAR(50)     NOT NULL   (pending | completed | failed | refunded)
├── valid_from          TIMESTAMPTZ     NOT NULL
├── valid_until         TIMESTAMPTZ     NULLABLE   -- NULL for unlimited
└── created_at          TIMESTAMPTZ     NOT NULL   DEFAULT now()
```

### **`SubscriptionActivatedEvent` (RabbitMQ)**

Published to `payment-events` Fanout Exchange. Consumed by the Projects Service.

```json
{
  "EventType": "SubscriptionActivatedEvent",
  "UserId": "uuid",
  "SubscriptionTier": "pro",
  "CreditsLow": 100,
  "CreditsKontext": 50,
  "CreditsHigh": 10,
  "ValidFrom": "2025-06-04T00:00:00Z",
  "ValidUntil": "2025-07-04T00:00:00Z",
  "PaymentProvider": "stripe",
  "PaymentRecordId": "uuid"
}
```

> Credits per tier (`CreditsLow`, `CreditsKontext`, `CreditsHigh`) are defined in the Payment Service config, not hardcoded per event. This allows changing tier allocations without redeploying Projects Service.

### **Webhook Flow**

```mermaid
sequenceDiagram
    participant P as Payment Provider<br/>(Stripe / MercadoPago)
    participant PAY as Payment Service
    participant DB as Payment DB
    participant R as RabbitMQ
    participant PROJ as Projects Service
    participant UDB as Projects DB

    P->>PAY: POST /api/webhooks/stripe (or /mercadopago)
    PAY->>PAY: Validate webhook signature
    PAY->>PAY: Parse event type (payment_intent.succeeded etc.)
    PAY->>DB: INSERT payment_records (status=completed)
    PAY->>R: Publish SubscriptionActivatedEvent → payment-events exchange
    R-->>PROJ: Consume SubscriptionActivatedEvent
    PROJ->>UDB: UPSERT user_credits SET subscription_tier, credits_remaining, reset_at
    PROJ-->>R: (no reply needed — fire and forget)
```

### **Webhook Endpoint**

```
POST /api/webhooks/stripe
POST /api/webhooks/mercadopago

Headers:
  Stripe-Signature: {signature}       // validated with webhook secret
  X-MercadoPago-Signature: {sig}      // validated with webhook secret

Response:
  200 OK  → event received and queued
  400     → invalid signature
  422     → unprocessable event type (not a subscription payment)
```

### **Idempotency**

The Payment Service uses `provider_payment_id` as a `UNIQUE` constraint. If the same webhook is received twice (providers retry on non-200), the `INSERT` will fail silently and no duplicate `SubscriptionActivatedEvent` is published.

---

## **4. Updated RabbitMQ Exchanges**

Adding `payment-events` exchange to the existing setup:

| Exchange | Type | Publisher | Consumer | Events |
|----------|------|-----------|----------|--------|
| `image-events` | Fanout | Projects Service | IA Service | `ImageUploadedEvent` |
| `processed-image-events` | Fanout | IA Service | Notifications Service | `ImageProcessingCompletedEvent`, `ImageProcessingFailedEvent` |
| `payment-events` | Fanout | Payment Service | Projects Service | `SubscriptionActivatedEvent` |

```mermaid
flowchart LR
    subgraph "Exchanges"
        IE[image-events<br/>Fanout]
        PIE[processed-image-events<br/>Fanout]
        PE[payment-events<br/>Fanout]
    end

    subgraph "Publishers"
        PS[Projects Service]
        IA[IA Service]
        PAY[Payment Service]
    end

    subgraph "Consumers"
        IAC[IA Service Consumer]
        NS[Notifications Service]
        PSC[Projects Service Consumer]
    end

    PS -->|ImageUploadedEvent| IE
    IE --> IAC

    IA -->|ImageProcessingCompletedEvent<br/>ImageProcessingFailedEvent| PIE
    PIE --> NS

    PAY -->|SubscriptionActivatedEvent| PE
    PE --> PSC
```

---

## **5. Updated C2 Container Diagram**

```mermaid
flowchart TB
    subgraph "User Layer"
        F[Frontend<br/>Angular App]
    end

    subgraph "PixPro Platform"
        G[API Gateway<br/>YARP]
        AUTH[Auth Service<br/>.NET]
        PS[Projects Service<br/>.NET]
        IA[IA Service<br/>Python/FastAPI]
        NS[Notifications Service<br/>.NET]
        PAY[Payment Service<br/>.NET]
    end

    subgraph "Data Stores"
        R[(RabbitMQ<br/>Message Broker)]
        AUTHDB[(Auth DB<br/>PostgreSQL)]
        PROJDB[(Projects DB<br/>PostgreSQL)]
        IADB[(IA DB<br/>PostgreSQL)]
        PAYDB[(Payment DB<br/>PostgreSQL)]
        RED[(Redis<br/>Notifications Cache)]
    end

    subgraph "External"
        PX[Pixazo.ai]
        OA[OpenAI]
        PO[Pollinations.ai]
        CL[Cloudinary]
        STRIPE[Stripe / MercadoPago]
    end

    F -->|HTTP| G
    F -->|WebSocket| G
    G -->|HTTP| AUTH
    G -->|HTTP| PS
    G -->|HTTP / WebSocket| NS

    AUTH --- AUTHDB

    PS -->|Publish image-events| R
    PS --- PROJDB
    R -->|Consume image-processing-events| IA
    IA -->|Publish processed-image-events| R
    R -->|Consume processed-image-events| NS
    NS --- RED

    IA --- IADB
    IA -->|HTTP| PX
    IA -->|HTTP| OA
    IA -->|HTTP| PO

    PS -->|Upload original| CL
    IA -->|Upload result| CL

    STRIPE -->|Webhook| PAY
    PAY -->|Publish payment-events| R
    R -->|Consume SubscriptionActivatedEvent| PS
    PAY --- PAYDB
```

---

## **6. End-to-End Subscription Activation Flow**

```mermaid
flowchart TD
    A[User selects subscription plan<br/>on Frontend] --> B[Frontend redirects to<br/>Stripe / MercadoPago checkout]
    B --> C[User completes payment<br/>on provider page]
    C --> D[Provider sends webhook<br/>POST /api/webhooks/stripe]
    D --> E{Signature valid?}
    E -->|No| F[Return 400 — ignore]
    E -->|Yes| G[Parse event type]
    G --> H{Is subscription payment?}
    H -->|No| I[Return 422 — not relevant]
    H -->|Yes| J[INSERT payment_records]
    J --> K{Duplicate payment_id?}
    K -->|Yes - already processed| L[Return 200 — idempotent skip]
    K -->|No| M[Publish SubscriptionActivatedEvent<br/>→ payment-events exchange]
    M --> N[Projects Service consumes event]
    N --> O[UPSERT user_credits<br/>SET tier, credits_remaining, reset_at]
    O --> P[User can now use expanded credit pool]
```

---

## **7. Summary: New Components**

| Component | Service | Type | Status |
|-----------|---------|------|--------|
| `IsActive` on `User` | Auth Service | DB field + domain method | New |
| `GET /api/auth/users` | Auth Service | Admin endpoint | New |
| `PATCH /api/auth/users/{id}/status` | Auth Service | Admin endpoint | New |
| `GetAllUsersAsync` / `SetUserStatusAsync` | Auth Service | Service methods | New |
| `PATCH /api/projects/admin/users/{id}/subscription` | Projects Service | Admin endpoint | New |
| `GET /api/projects/admin/users/{id}/credits` | Projects Service | Admin endpoint | New |
| `SubscriptionActivatedEvent` consumer | Projects Service | RabbitMQ consumer | New |
| **Payment Service** | New microservice | Webhook receiver + event publisher | New |
| `payment_records` table | Payment DB | PostgreSQL | New |
| `payment-events` exchange | RabbitMQ | Fanout exchange | New |
| `SubscriptionActivatedEvent` | RabbitMQ | Event message | New |

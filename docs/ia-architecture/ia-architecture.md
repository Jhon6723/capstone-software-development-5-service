## **Simplified IA Microservice Architecture**

### **Proposed Structure (3 Layers)**

```
src/Services/IA/
├── app/
│   ├── __init__.py
│   ├── main.py                    # FastAPI entry point
│   ├── config.py                  # Configuration
│   ├── models/                    # Data models
│   │   ├── __init__.py
│   │   ├── events.py              # RabbitMQ event models
│   │   ├── database.py            # SQLAlchemy models
│   │   └── schemas.py             # Pydantic schemas
│   ├── services/                  # Business logic
│   │   ├── __init__.py
│   │   ├── image_processor.py     # Image processing orchestrator
│   │   ├── guardrails.py          # Content moderation / safety filter
│   │   ├── cloudinary_service.py  # Cloudinary operations
│   │   └── database_service.py    # Database CRUD operations
│   ├── infrastructure/            # External integrations
│   │   ├── __init__.py
│   │   ├── database.py            # PostgreSQL connection
│   │   ├── rabbitmq.py            # RabbitMQ consumer/publisher
│   │   └── cloudinary.py          # Cloudinary client
│   └── processors/                # AI processors (plug-in architecture)
│       ├── __init__.py
│       ├── base.py                # Base processor interface
│       ├── pixazo.py              # Pixazo.ai Flux Schnell (text-to-image)
│       ├── openai.py              # OpenAI GPT Image (image-to-image)
│       └── pollinations.py        # Pollinations.ai Kontext (image-to-image)
├── alembic/                       # DB migrations
├── requirements.txt
├── Dockerfile
└── docker-compose.yml
```

### **Layer Responsibilities**

**Models Layer** - Pure data structures
- Events (RabbitMQ messages)
- Database tables (SQLAlchemy)
- Pydantic schemas (validation)

**Services Layer** - Business logic
- `ImageProcessor`: Orchestrate processing workflow, route to AI providers
- `Guardrails`: Content moderation, safety filtering (sexual, violence, hate, self-harm)
- `CloudinaryService`: Upload/delete images
- `DatabaseService`: CRUD operations for job records

**Processors Layer** - AI provider integrations (plug-in architecture)
- `PixazoProcessor`: Text-to-image generation with Flux Schnell
- `OpenAIProcessor`: Image-to-image editing with GPT Image
- `PollinationsProcessor`: Image-to-image editing with FLUX.1 Kontext

**Infrastructure Layer** - External systems
- Database connection (PostgreSQL)
- RabbitMQ consumer/publisher
- Cloudinary SDK wrapper



## **Recommendation: PostgreSQL**

**Reasons:**

**1. Consistency with existing services**
- Auth, Projects, Notifications already use PostgreSQL
- Same database stack = easier operations, monitoring, backups
- Shared infrastructure costs

**2. Data is relational**
- ProcessingJob → ProcessedImage (one-to-many relationship)
- User → ProcessingJob (one-to-many)
- Foreign keys ensure data integrity

**3. PostgreSQL JSON support**
- `JSONB` type for flexible fields (parameters, results)
- Query JSON fields efficiently
- No need for NoSQL

**4. Strong transaction support**
- ACID guarantees for job status updates
- Prevents race conditions during processing

**5. Better for analytics**
- Complex queries (aggregations, joins)
- Window functions for time-series analysis
- Materialized views for reporting

## **AI Model Selection: Multi-Model Strategy**

The IA service supports **3 tiers** across two providers (OpenAI + Pollinations.ai). The split is pragmatic: OpenAI is reliable and pay-per-call (so we use it at low and high quality for the cheapest and the most capable tiers), and Pollinations gives us a modern FLUX.1 Kontext img2img model in the middle.

**Default Model: GPT Image 1 Mini (low quality)**
- Model key: `gpt-image-1-mini-low`
- Provider: OpenAI (`POST /v1/images/edits`, `quality=low`)
- Purpose: **Cheapest reliable image editing** — same OpenAI pipeline as the AI Reasoning tier, just at low quality to keep cost down
- Cost: ~$0.011 per image
- Why we use it as Default: already paid for in this project, predictable pricing, no minimum top-up, no separate API key to manage

### **Available Models**

| Tier | Model key | Provider | Task | Cost / image | Quality |
|------|-----------|----------|------|--------------|---------|
| **Budget** ⭐ | `flux-schnell` | Pixazo.ai | **text-to-image** | **~$0.0012** | ⭐⭐⭐⭐ |
| Default | `gpt-image-1-mini-low` | OpenAI (`quality=low`) | image-to-image | ~$0.011 | ⭐⭐⭐⭐ |
| Standard | `kontext` (FLUX.1 Kontext) | Pollinations.ai | image-to-image | ~$0.04 | ⭐⭐⭐⭐⭐ |
| AI Reasoning | `gpt-image-1-mini-high` | OpenAI (`quality=high`) | image-to-image | ~$0.167 | ⭐⭐⭐⭐⭐ |

### **Model Comparison**

| Model | Parameters | Purpose | Provider | Quality |
|-------|-------------|---------|----------|---------|
| **GPT Image 1 Mini** ⭐ | N/A (closed) | Reasoning-based image editing | OpenAI | ⭐⭐⭐⭐⭐ |
| FLUX.1 Kontext | ~12B | Instruction-based image editing | Pollinations.ai (FLUX family) | ⭐⭐⭐⭐⭐ |

### **Price Comparison**

All figures below are per single output image. Pollinations prices verified live against `GET https://gen.pollinations.ai/models`.

| Tier | Model | Provider | Task | Approx. cost per image | Approx. latency | Notes |
|------|-------|----------|------|------------------------|-----------------|-------|
| **Budget** ⭐ | `flux-schnell` | Pixazo.ai | **text-to-image** | **~$0.0012** | ~4–6 s | Cheapest option, no source image needed |
| Default | `gpt-image-1-mini` (low) | OpenAI | image-to-image | ~$0.011 | ~5–8 s | Already paid OpenAI account |
| Standard | `kontext` (FLUX.1 Kontext) | Pollinations.ai | image-to-image | ~$0.04 (0.04 Pollen) | ~6–10 s | $2 ≈ 50 edits |
| AI Reasoning | `gpt-image-1-mini` (high) | OpenAI | image-to-image | ~$0.167 | ~20–30 s | Premium quality |

**Bottom line:**
- For *text-to-image generation*: **`flux-schnell` on Pixazo** — ~$0.0012 each, cheapest option, no source image required.
- For *everyday cheap edits*: **`gpt-image-1-mini-low`** — ~$0.011 each, predictable, already on OpenAI billing.
- For *high-quality instruction-based editing*: **`kontext` (FLUX.1 Kontext) on Pollinations** — best img2img model in the FLUX family.
- For *complex reasoning prompts*: **`gpt-image-1-mini-high`** (AI Reasoning tier).

### **Advantages & Disadvantages**

| Model | Advantages | Disadvantages |
|-------|------------|---------------|
| **GPT Image 1 Mini (low)** ⭐ | • Cheapest reliable hosted img2img (~$0.011)<br>• Same SDK as AI Reasoning tier (one client, two qualities)<br>• Predictable per-image pricing<br>• Already on the project's OpenAI account | • Closed source / vendor lock-in<br>• Fixed sizes only (1024², 1024×1536, 1536×1024)<br>• Lower visual fidelity than `medium`/`high` |
| FLUX.1 Kontext (Pollinations) | • State-of-the-art instruction-based img2img<br>• Open-source, non-corporate platform<br>• OpenAI-compatible REST API<br>• Pay-as-you-go in Pollen credits, no minimum top-up | • ~4× more expensive than Default<br>• Pollinations is in beta — daily grants are tiny, real use needs a top-up |
| GPT Image 1 Mini (high) | • Best results on complex / natural prompts<br>• Reliable hosted API<br>• Predictable per-image pricing | • Most expensive option ($0.167/img)<br>• Slowest (~20–30 s)<br>• Closed source / vendor lock-in |

### **Why GPT Image 1 Mini (low) as Default**

- **Cheap and predictable** — ~$0.011/image, billed per call on the OpenAI account already used by the project
- **No new provider to manage** — same SDK and key as the AI Reasoning tier; only the `quality` param changes
- **Reliable hosted API** — OpenAI is production-grade, unlike beta platforms with tiny daily grants
- **Same image-edit endpoint** as the high-quality tier, so behavior is consistent across tiers
- **No minimum top-up** — you spend exactly what you generate, down to fractions of a cent

### **Model Selection Logic**

```python
class ModelTier(Enum):
    PIXAZO       = "flux-schnell"           # Pixazo Flux Schnell                   (~$0.0012/img)  [text-to-image]
    DEFAULT      = "gpt-image-1-mini-low"   # OpenAI gpt-image-1-mini, quality=low  (~$0.011/img)   [image-to-image]
    STANDARD     = "kontext"                # Pollinations FLUX.1 Kontext           (~$0.04/img)    [image-to-image]
    AI_REASONING = "gpt-image-1-mini-high"  # OpenAI gpt-image-1-mini, quality=high (~$0.167/img)   [image-to-image]
```

The user can explicitly select a tier via the `model` parameter in the upload event. If no model is specified, the system uses **`gpt-image-1-mini-low`** — the cheapest reliable option, paid for on the existing OpenAI account.


## **Credit System Architecture**

### **Overview**

Users start on a **Free tier** with a fixed number of credits per model. Each credit = 1 image generation. Admins have unlimited attempts. Subscriptions (future) grant a larger monthly credit pool per tier.

Credit enforcement lives in the **Projects Service** — it acts as the gate *before* publishing to RabbitMQ. The IA Service stays purely a processing worker with no billing awareness.

### **Free Tier Credit Allocation**

| Model | Free Credits | Credits per Image | Notes |
|-------|-------------|-------------------|-------|
| `gpt-image-1-mini-low` | **5** | 1 | Cheapest img2img option |
| `kontext` (Pollinations) | **3** | 1 | Standard quality |
| `gpt-image-1-mini-high` | **1** | 1 | Premium quality |
| `flux-schnell` (Pixazo) | **Unlimited** | — | Text-to-image, cheapest tier, no credit gate |

### **Subscription Tiers (Future)**

Subscriptions expand the monthly credit pool. Exact pricing TBD.

| Tier | `gpt-image-1-mini-low` | `kontext` | `gpt-image-1-mini-high` | Reset |
|------|------------------------|-----------|--------------------------|-------|
| **Free** | 5 total | 3 total | 1 total | Never (one-time grant) |
| **Basic** | N / month | N / month | N / month | Monthly |
| **Pro** | N / month | N / month | N / month | Monthly |
| **Unlimited** | ∞ | ∞ | ∞ | — |

> `N` values to be defined when subscription pricing is established.

> For credit top-up (pay-as-you-go) pricing, flow and event design, see [`admin-and-payments-architecture.md`](../admin-and-payments-architecture.md) — Section 3: Payment Service.

### **Admin Role**

Admins bypass all credit checks entirely — no deduction, no limit on any model.

### **Database Table: `user_credits`**

Owned by the **Projects Service** database (same DB that holds User → Project relationships).

```sql
user_credits
├── id                UUID          PRIMARY KEY
├── user_id           UUID          NOT NULL  FK → users.id
├── model_tier        ENUM          NOT NULL  (gpt_mini_low | kontext | gpt_mini_high)
├── credits_remaining INT           NOT NULL  DEFAULT 0
├── credits_total     INT           NOT NULL  DEFAULT 0   -- max for current subscription period
├── subscription_tier ENUM          NOT NULL  DEFAULT 'free'  (free | basic | pro | unlimited)
├── reset_at          TIMESTAMPTZ   NULLABLE  -- NULL for free tier, monthly date for subscriptions
└── updated_at        TIMESTAMPTZ   NOT NULL  DEFAULT now()
```

**Indexes:**
- `UNIQUE (user_id, model_tier)` — one row per user per model tier
- `INDEX (user_id)` — fast credit lookup per user

### **Credit Check Gate (Projects Service)**

The credit check happens **before** publishing to RabbitMQ. No message is sent if the user has insufficient credits.

```
HTTP Request → Projects Service
  1. Identify user role
     ├─ Admin → skip all credit logic → publish event
     └─ Regular user → continue
  2. Identify model_tier from request parameters
     ├─ flux-schnell → skip credit check (unlimited) → publish event
     └─ gpt_mini_low | kontext | gpt_mini_high → continue
  3. SELECT credits_remaining FROM user_credits
     WHERE user_id = ? AND model_tier = ?
     ├─ credits_remaining = 0 → return 402 INSUFFICIENT_CREDITS
     └─ credits_remaining > 0 → continue
  4. Atomically decrement: UPDATE user_credits
     SET credits_remaining = credits_remaining - 1
     WHERE user_id = ? AND model_tier = ? AND credits_remaining > 0
     ├─ 0 rows affected (race condition) → return 402 INSUFFICIENT_CREDITS
     └─ 1 row affected → publish ImageUploadedEvent to RabbitMQ
  5. If IA Service returns ImageProcessingFailedEvent
     → Refund: UPDATE user_credits SET credits_remaining = credits_remaining + 1
```

### **Credit Refund on AI Failure**

When the IA Service publishes `ImageProcessingFailedEvent`, the Projects Service consumes it and **refunds 1 credit** to the user for the model tier used. This ensures users are not penalized for provider-side errors.

Refund applies to all error codes **except** `CONTENT_MODERATION_VIOLATION` — if the user submitted blocked content, the credit is consumed (intentional abuse deterrent).

| Error Code | Credit Refunded? |
|------------|-----------------|
| `CONTENT_MODERATION_VIOLATION` | ❌ No |
| `MODEL_NOT_FOUND` | ✅ Yes |
| `PROCESSING_ERROR` | ✅ Yes |
| `UPLOAD_ERROR` | ✅ Yes |

### **New Error Codes**

Added to the Projects Service HTTP layer:

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `INSUFFICIENT_CREDITS` | 402 | User has 0 credits remaining for the requested model tier |

### **Credit System Flow**

```mermaid
flowchart TD
    A[User Request: POST /api/projects] --> B{Is Admin?}
    B -->|Yes| G[Skip credit check]
    B -->|No| C{model = flux-schnell?}
    C -->|Yes| G
    C -->|No| D[Query user_credits table]
    D --> E{credits_remaining > 0?}
    E -->|No| F[Return 402 INSUFFICIENT_CREDITS]
    E -->|Yes| H[Atomic decrement credits_remaining]
    H --> I{Decrement succeeded?}
    I -->|No - race condition| F
    I -->|Yes| G
    G --> J[Publish ImageUploadedEvent to RabbitMQ]
    J --> K[IA Service processes image]
    K --> L{Processing result?}
    L -->|ImageProcessingCompletedEvent| M[Done - credit consumed]
    L -->|ImageProcessingFailedEvent| N{ErrorCode?}
    N -->|CONTENT_MODERATION_VIOLATION| O[Credit NOT refunded]
    N -->|Other errors| P[Refund 1 credit to user]
```

### **Architecture Placement Summary**

| Concern | Service | Layer |
|---------|---------|-------|
| Credit balance storage | Projects Service DB | `user_credits` table |
| Credit check & deduction | Projects Service | Business logic (before RabbitMQ publish) |
| Credit refund on failure | Projects Service | Consumes `ImageProcessingFailedEvent` |
| Subscription tier tracking | Projects Service DB | `user_credits.subscription_tier` |
| Admin bypass | Projects Service | Role check in request handler |
| IA Service | **No awareness of credits** | Pure processing worker |

---

## **System Flows (Mermaid Diagrams)**

### **1. Text-to-Image vs Image-to-Image Flow Comparison**

```mermaid
flowchart TB
    subgraph "Text-to-Image (Budget Tier)"
        A1[User sends prompt] --> B1[Frontend<br/>Angular]
        B1 -->|HTTP /api/projects| C1[Gateway<br/>YARP]
        C1 -->|HTTP| D1[Projects Service<br/>.NET]
        D1 -->|Publish| E1[Image Events Exchange]
        E1 --> F1[IA Service<br/>Python]
        F1 --> G1{Content Check}
        G1 -->|Pass| H1[Pixazo Flux Schnell]
        H1 --> I1[Download generated image]
        I1 --> J1[Cloudinary Upload]
        J1 --> K1[Save to PostgreSQL]
        K1 --> L1[Publish completion]
        G1 -->|Blocked| M1[Return error: CONTENT_MODERATION_VIOLATION]
    end

    subgraph "Image-to-Image (Default/Standard/AI Reasoning)"
        A2[User uploads image + prompt] --> B2[Frontend<br/>Angular]
        B2 -->|HTTP /api/projects| C2[Gateway<br/>YARP]
        C2 -->|HTTP| D2[Projects Service<br/>.NET]
        D2 --> E2[Cloudinary: Store original]
        D2 -->|Publish| F2[Image Events Exchange]
        F2 --> G2[IA Service<br/>Python]
        G2 --> H2{Content Check}
        H2 -->|Pass| I2{Model Selection}
        I2 -->|flux-schnell| J2[Pixazo]
        I2 -->|gpt-image-1-mini-low| K2[OpenAI Low Quality]
        I2 -->|kontext| L2[Pollinations]
        I2 -->|gpt-image-1-mini-high| M2[OpenAI High Quality]
        J2 & K2 & L2 & M2 --> N2[Cloudinary: Store result]
        N2 --> O2[PostgreSQL: Save record]
        O2 --> P2[Publish completion]
        H2 -->|Blocked| Q2[Return error: CONTENT_MODERATION_VIOLATION]
    end
```

---

### **2. IA Service Internal Processing Flow**

```mermaid
sequenceDiagram
    participant R as RabbitMQ
    participant C as Consumer
    participant G as Guardrails
    participant P as ImageProcessor
    participant M as ModelRouter
    participant AI as AI Provider
    participant CL as Cloudinary
    participant DB as PostgreSQL
    participant OUT as Output Exchange

    R->>C: ImageUploadedEvent
    C->>C: Validate event
    Note over C: ImageUrl optional<br/>for text-to-image
    
    alt Content violates policy
        C->>G: Check prompt
        G-->>C: ContentModerationError
        C->>DB: Save as FAILED
        C->>OUT: ImageProcessingFailedEvent
        Note over OUT: ErrorCode: CONTENT_MODERATION_VIOLATION
    else Content safe
        C->>G: Check prompt
        G-->>C: Pass
        C->>P: process(event)
        P->>M: get_processor(model)
        
        alt model = flux-schnell
            M-->>P: PixazoProcessor
            P->>AI: POST /flux-1-schnell
            AI-->>P: JSON {output: url}
            P->>AI: GET image from url
            AI-->>P: image bytes
        else model = gpt-image-1-mini-*
            M-->>P: OpenAIProcessor
            P->>AI: POST /v1/images/edits
            AI-->>P: image bytes
        else model = kontext
            M-->>P: PollinationsProcessor
            P->>AI: POST /v1/images/edits
            AI-->>P: image bytes
        end
        
        P->>CL: upload_image(bytes)
        CL-->>P: secure_url
        P->>DB: update_job(COMPLETED)
        P->>OUT: ImageProcessingCompletedEvent
    end
```

---

### **3. Model Selection Decision Tree**

```mermaid
flowchart TD
    A[Request received] --> B{Has ImageUrl?}
    B -->|No| C[Text-to-Image]
    B -->|Yes| D[Image-to-Image]
    
    C --> E{model parameter}
    E -->|flux-schnell| F[Pixazo Flux Schnell<br/>$0.0012/img]
    E -->|other| G[Invalid: text-to-image<br/>only supports flux-schnell]
    
    D --> H{model parameter}
    H -->|gpt-image-1-mini-low| I[OpenAI Low Quality<br/>$0.011/img]
    H -->|kontext| J[Pollinations FLUX.1 Kontext<br/>$0.04/img]
    H -->|gpt-image-1-mini-high| K[OpenAI High Quality<br/>$0.167/img]
    H -->|flux-schnell| L[Pixazo Flux Schnell<br/>$0.0012/img]
    H -->|none/default| M[OpenAI Low Quality<br/>$0.011/img]
    
    F & I & J & K & L & M --> N[Content Moderation Check]
    N -->|Pass| O[Process with AI]
    N -->|Fail| P[Return CONTENT_MODERATION_VIOLATION]
```

---

### **4. End-to-End Complete Flow**

```mermaid
flowchart LR
    subgraph "User Layer"
        U[User]
        F[Frontend<br/>Angular]
    end

    subgraph "API Layer"
        G[Gateway<br/>YARP]
        PS[Projects Service<br/>.NET]
    end

    subgraph "Storage"
        CL[Cloudinary<br/>Image CDN]
        R[(RabbitMQ<br/>Message Broker)]
    end

    subgraph "IA Processing"
        IA[IA Service<br/>Python/FastAPI]
        GR[Guardrails<br/>Content Filter]
        PR[Processors<br/>AI Integrations]
        DB[(PostgreSQL<br/>IA Database)]
    end

    subgraph "AI Providers"
        PX[Pixazo.ai<br/>Flux Schnell]
        OA[OpenAI<br/>GPT Image]
        PO[Pollinations.ai<br/>Kontext]
    end

    subgraph "Notifications"
        NS[Notifications Service<br/>.NET]
    end

    U -->|1. Upload image /<br/>Enter prompt| F
    F -->|2. HTTP /api/projects| G
    G -->|3. Route| PS
    
    PS -->|4a. Store original| CL
    PS -->|4b. Publish image-events| R
    
    R -->|5. Consume| IA
    IA -->|6. Check content| GR
    GR -->|7. If safe| PR
    
    PR -->|8a. Text2Image| PX
    PR -->|8b. Image2Image| OA
    PR -->|8c. Image2Image| PO
    
    PX & OA & PO -->|9. Return image| PR
    PR -->|10. Upload result| CL
    PR -->|11. Save job status| DB
    PR -->|12. Publish processed-image-events| R
    
    R -->|13. Consume| NS
    NS -->|14. WebSocket /api/websocket| G
    G -->|15. WebSocket| F
    F -->|16. Display result| U
```

---

### **5. Content Moderation Flow**

```mermaid
sequenceDiagram
    participant U as User
    participant IA as IA Service
    participant G as Guardrails
    participant AI as AI Provider

    U->>IA: Submit prompt for processing
    IA->>G: check_prompt(prompt)
    
    G->>G: Check sexual keywords
    G->>G: Check violence keywords
    G->>G: Check hate speech
    G->>G: Check self-harm
    
    alt Any category matched
        G-->>IA: ContentModerationError
        IA-->>U: Error: Content blocked by safety filter
        Note over IA: ErrorCode: CONTENT_MODERATION_VIOLATION
    else All checks passed
        G-->>IA: Prompt passed safety check
        IA->>AI: Call AI provider
        AI-->>IA: Generated image
        IA-->>U: Return processed image URL
    end
```

---

### **6. RabbitMQ Message Flow Detail**

```mermaid
flowchart LR
    subgraph "Exchanges"
        IE[image-events<br/>Fanout Exchange]
        PIE[processed-image-events<br/>Fanout Exchange]
    end

    subgraph "Queues"
        IPQ[image-processing-events<br/>Queue]
        PIQ[processed-image-events<br/>Queue]
    end

    subgraph "Publishers"
        PS[Projects Service<br/>.NET]
        IAS[IA Service<br/>Python]
    end

    subgraph "Consumers"
        IAC[IA Service<br/>Consumer]
        NS[Notifications Service<br/>.NET Consumer]
    end

    %% Flow: Projects Service publishes to image-events
    PS -->|1. Publish<br/>ImageUploadedEvent| IE
    IE -->|Bind| IPQ
    IAC -->|2. Consume| IPQ

    %% Flow: IA Service publishes results to processed-image-events
    IAS -->|3. Publish<br/>ImageProcessingCompletedEvent| PIE
    IAS -->|3. Publish<br/>ImageProcessingFailedEvent| PIE
    PIE -->|Bind| PIQ
    NS -->|4. Consume| PIQ

    %% Notifications Service also listens to image-events for upload notifications
    IE -.->|Bind| PIQ
    NS -.->|Consume image-events| PIQ
```

**Message Types:**

| Event | Publisher | Consumer | Key Fields |
|-------|-----------|----------|------------|
| `ImageUploadedEvent` | Projects Service (.NET) | IA Service (Python) | `ImageId`, `OwnerId`, `Prompt`, `ImageUrl` (optional), `Parameters` |
| `ImageProcessingCompletedEvent` | IA Service (Python) | Notifications Service (.NET) | `ImageId`, `ProcessedImageUrl`, `ModelUsed`, `ProcessingTimeMs` |
| `ImageProcessingFailedEvent` | IA Service (Python) | Notifications Service (.NET) | `ImageId`, `ErrorCode`, `ErrorMessage` |

**Error Codes:**
- `CONTENT_MODERATION_VIOLATION` - Prompt blocked by guardrails
- `MODEL_NOT_FOUND` - Requested model not available
- `PROCESSING_ERROR` - AI provider error
- `UPLOAD_ERROR` - Cloudinary upload failed

---

## **C4 Architecture Diagrams**

### **C1: System Context Diagram**

```mermaid
flowchart TB
    subgraph "External Systems"
        U[User<br/>Content Creator]
    end

    subgraph "PixPro Platform"
        PP[PixPro AI Image<br/>Processing System]
    end

    subgraph "External AI & Storage"
        OA[OpenAI<br/>gpt-image-1-mini]
        PA[Pixazo.ai<br/>flux-schnell]
        PL[Pollinations.ai<br/>kontext]
        CL[Cloudinary<br/>Image CDN]
    end

    U -->|Upload images /<br/>Enter prompts| PP
    PP -->|Process image<br/>editing| OA
    PP -->|Generate images<br/>from text| PA
    PP -->|Advanced image<br/>editing| PL
    PP -->|Store all images| CL
    CL -->|Serve images| U
```

---

### **C2: Container Diagram**

```mermaid
flowchart TB
    subgraph "User Layer"
        F[Frontend<br/>Angular App]
    end

    subgraph "PixPro Platform"
        G[API Gateway<br/>YARP]
        PS[Projects Service<br/>.NET]
        IA[IA Service<br/>Python/FastAPI]
        NS[Notifications Service<br/>.NET]

        subgraph "IA Service Internals"
            GR[Guardrails<br/>Content Filter]
            PR[Processors<br/>AI Integrations]
        end
    end

    subgraph "Data Stores"
        R[(RabbitMQ<br/>Message Broker)]
        DB[(PostgreSQL<br/>IA Database)]
        RED[(Redis<br/>Notifications Cache)]
    end

    subgraph "External AI Providers"
        PX[Pixazo.ai<br/>Flux Schnell]
        OA[OpenAI<br/>GPT Image]
        PO[Pollinations.ai<br/>Kontext]
    end

    subgraph "External Storage"
        CL[Cloudinary<br/>Image CDN]
    end

    %% HTTP Flows
    F -->|HTTP /api/projects| G
    F -->|HTTP /api/notifications| G
    F -->|WebSocket /api/websocket| G
    G -->|HTTP| PS
    G -->|HTTP / WebSocket| NS

    %% RabbitMQ Flows - Only Services publish/consume
    PS -->|Publish image-events| R
    R -->|Consume image-processing-events| IA
    IA -->|Publish processed-image-events| R
    R -->|Consume processed-image-events| NS

    %% WebSocket to Frontend
    NS -->|WebSocket notification| F

    %% IA Service internal
    IA -->|Check content| GR
    IA -->|Route| PR

    %% AI Provider calls
    PR -->|HTTP POST /flux-1-schnell| PX
    PR -->|HTTP POST /v1/images/edits| OA
    PR -->|HTTP POST /v1/images/edits| PO

    %% Storage
    PR -->|Upload result| CL
    PS -->|Upload original| CL
    IA -->|Save job status| DB
    NS -->|Cache notifications| RED
```

---

### **C3: Component Diagram - IA Service**

```mermaid
flowchart TB
    subgraph "IA Service Container"
        API[FastAPI<br/>Health Check API]

        subgraph "RabbitMQ Layer"
            CON[RabbitMQ Consumer<br/>Message Handler]
            PUB[RabbitMQ Publisher<br/>Event Emitter]
        end

        subgraph "Business Logic"
            IP[Image Processor<br/>Orchestrator]
            GR[Guardrails Service<br/>Content Moderation]
            MR[Model Router<br/>Tier Selection]
        end

        subgraph "AI Processors"
            PP[Pixazo Processor<br/>Flux Schnell]
            OP[OpenAI Processor<br/>gpt-image-1-mini]
            POL[Pollinations Processor<br/>kontext]
        end

        subgraph "Infrastructure"
            DB[(PostgreSQL<br/>Job Repository)]
            CL[Cloudinary Client<br/>Image Storage]
        end
    end

    subgraph "External"
        R[(RabbitMQ)]
        PX[Pixazo.ai API]
        OA[OpenAI API]
        PO[Pollinations API]
    end

    R -->|Consume| CON
    CON -->|Validate & Route| IP
    IP -->|Check prompt| GR
    GR -->|Pass/Fail| IP
    IP -->|Select model| MR
    MR -->|Route| PP
    MR -->|Route| OP
    MR -->|Route| POL

    PP -->|HTTP| PX
    OP -->|HTTP| OA
    POL -->|HTTP| PO

    PP & OP & POL -->|Bytes| CL
    CL -->|URL| IP
    IP -->|Save status| DB
    IP -->|Publish event| PUB
    PUB -->|Publish| R

    API -.->|Health check| DB
    API -.->|Health check| R
```

---

### **C4: Code Diagram - Image Processing Flow**

```mermaid
flowchart TD
    subgraph "Entry Point"
        ON_MSG[_on_message<br/>message: bytes]
        PROC_EVT[_process_event<br/>event: ImageUploadedEvent]
    end

    subgraph "Processing Decision"
        CHECK{Guardrails.<br/>check_prompt}
        HAS_IMG{Has<br/>ImageUrl?}
    end

    subgraph "Text-to-Image Path"
        T2I[PixazoProcessor.<br/>process]
        T2I_API[POST /flux-1-schnell]
        T2I_DL[Download from<br/>output URL]
    end

    subgraph "Image-to-Image Path"
        I2I_DL[Download<br/>original image]
        I2I[Processor.<br/>process]
    end

    subgraph "Completion"
        UP[Cloudinary.<br/>upload_image]
        SAVE[Database.<br/>update_job]
        PUB[RabbitMQ.<br/>publish_completed]
    end

    ON_MSG -->|decode & validate| PROC_EVT
    PROC_EVT --> CHECK

    CHECK -->|Fail| ERR[Publish<br/>ImageProcessingFailedEvent]
    CHECK -->|Pass| HAS_IMG

    HAS_IMG -->|No| T2I
    T2I --> T2I_API
    T2I_API -->|JSON {output}| T2I_DL
    T2I_DL --> UP

    HAS_IMG -->|Yes| I2I_DL
    I2I_DL --> I2I
    I2I --> UP

    UP --> SAVE
    SAVE --> PUB
```

---

### **Components Overview**

**PixPro Platform (our system):**

| Component | Technology | Responsibility |
|-----------|------------|----------------|
| **Frontend** | Angular | Web app where users upload images and enter prompts |
| **Gateway** | YARP (Yet Another Reverse Proxy) | Routes HTTP requests and WebSocket connections |
| **Projects Service** | .NET | Handles image uploads, stores to Cloudinary, publishes to RabbitMQ |
| **IA Service** | Python/FastAPI | Consumes events, runs AI processing with guardrails |
| **Guardrails** | Python | Content safety filtering before AI processing |
| **Notifications Service** | .NET | Consumes completion events, sends WebSocket notifications |
| **IA Database** | PostgreSQL | Stores processing job records and status |
| **Notifications Cache** | Redis | Caches notifications for fast retrieval |

**External AI Providers:**
- **Pixazo.ai** - Flux Schnell for text-to-image ($0.0012/img)
- **OpenAI** - GPT Image 1 Mini for image editing ($0.011-$0.167/img)
- **Pollinations.ai** - FLUX.1 Kontext for image editing ($0.04/img)

**Infrastructure:**
- **Cloudinary** - Stores all images (original and generated)
- **RabbitMQ** - Message broker connecting services (Fanout exchanges: `image-events`, `processed-image-events`)

---

## **Processing Parameters**

When sending an image for processing, you can customize the AI behavior with these parameters:

### **Parameter Reference**

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `width` | int | 64-2048 | 512 | Output image width in pixels |
| `height` | int | 64-2048 | 512 | Output image height in pixels |
| `num_inference_steps` | int | 1-100 | 20 | AI refinement steps (Pixazo uses fixed 4 steps) |
| `strength` | float | 0.0-1.0 | 0.75 | How much AI can change the original image (image-to-image only) |
| `guidance_scale` | float | 1.0-20.0 | 7.5 | How closely AI follows the prompt (Pollinations only) |
| `quantity` | int | 1-10 | 1 | Number of image variations to generate |
| `model` | string | - | `gpt-image-1-mini-low` | AI model to use. Options: `flux-schnell`, `gpt-image-1-mini-low`, `kontext`, `gpt-image-1-mini-high` |

### **Usage Examples**

**Text-to-Image (Budget Tier - Cheapest at $0.0012/img):**
```json
{
  "ImageId": "uuid-here",
  "OwnerId": "user-uuid",
  "Prompt": "A majestic dragon flying over a medieval castle at sunset",
  "Parameters": {
    "width": 512,
    "height": 512,
    "quantity": 1,
    "model": "flux-schnell"
  }
}
```
*Note: No `ImageUrl` required for text-to-image generation*

**Image-to-Image subtle edit (Default Tier):**
```json
{
  "ImageId": "uuid-here",
  "OwnerId": "user-uuid",
  "ImageUrl": "https://cloudinary.com/original.jpg",
  "Prompt": "Make this look like a watercolor painting",
  "Parameters": {
    "width": 512,
    "height": 512,
    "strength": 0.3,
    "quantity": 1,
    "model": "gpt-image-1-mini-low"
  }
}
```

**Image-to-Image creative transformation (Standard Tier):**
```json
{
  "ImageId": "uuid-here",
  "OwnerId": "user-uuid",
  "ImageUrl": "https://cloudinary.com/original.jpg",
  "Prompt": "Transform into cyberpunk city at night, neon lights, futuristic",
  "Parameters": {
    "width": 768,
    "height": 512,
    "strength": 0.85,
    "guidance_scale": 12,
    "quantity": 1,
    "model": "kontext"
  }
}
```

**Premium quality edit (AI Reasoning Tier):**
```json
{
  "ImageId": "uuid-here",
  "OwnerId": "user-uuid",
  "ImageUrl": "https://cloudinary.com/original.jpg",
  "Prompt": "Add intricate details, professional photography lighting, 8K quality",
  "Parameters": {
    "width": 1024,
    "height": 1024,
    "strength": 0.5,
    "quantity": 1,
    "model": "gpt-image-1-mini-high"
  }
}
```

**Generate multiple variations:**
```json
{
  "ImageId": "uuid-here",
  "OwnerId": "user-uuid",
  "ImageUrl": "https://cloudinary.com/original.jpg",
  "Prompt": "Portrait with different artistic styles",
  "Parameters": {
    "width": 512,
    "height": 768,
    "quantity": 4,
    "model": "gpt-image-1-mini-low"
  }
}
```

**Content that will be blocked by Guardrails:**
```json
{
  "ImageId": "uuid-here",
  "OwnerId": "user-uuid",
  "Prompt": "Generate a violent scene with blood and weapons",
  "Parameters": {
    "model": "flux-schnell"
  }
}
```
*Result: Error `CONTENT_MODERATION_VIOLATION`*

### **Visual Guide: Strength Parameter**

```
strength=0.2          strength=0.5          strength=0.9
    ↓                    ↓                    ↓
┌──────────┐        ┌──────────┐        ┌──────────┐
│ Original │   →    │  Slight  │   →    │  Heavy   │
│  Photo   │        │ Changes  │        │ Transform│
└──────────┘        └──────────┘        └──────────┘
   20% AI             50% AI              90% AI
   influence         influence          influence
```

### **Model Selection Tips**

| Model | Best For | Task | Cost | Quality |
|-------|----------|------|------|---------|
| `flux-schnell` | Text-to-image generation | Text → Image | ⭐⭐⭐⭐⭐ Cheapest | ⭐⭐⭐⭐ |
| `gpt-image-1-mini-low` | Everyday image editing | Image → Image | ⭐ Cheapest | ⭐⭐⭐⭐ |
| `kontext` | High-quality instruction editing | Image → Image | 💰💰 Medium | ⭐⭐⭐⭐⭐ |
| `gpt-image-1-mini-high` | Complex reasoning edits | Image → Image | 💰💰💰 Premium | ⭐⭐⭐⭐⭐ |

---

## **Credit System Diagrams**

### **1. Credit System — Request Flow (with all bypass rules)**

Full flow from an HTTP request through credit check, deduction, and the RabbitMQ publish gate.

```mermaid
flowchart TD
    A([HTTP POST /api/images/upload]) --> B{JWT valid?}
    B -->|No| B1[401 Unauthorized]
    B -->|Yes| C{Is Admin?}

    C -->|Yes ✓| BYPASS[Skip credit check]
    C -->|No| D{model = flux-schnell?}

    D -->|Yes ✓ free tier| BYPASS
    D -->|No| E[Query user_credits table\nWHERE user_id + model_tier]

    E --> F{Row exists?}
    F -->|No — first time| G[SeedFreeCredits\ngpt_mini_low=5\nkontext=3\ngpt_mini_high=1]
    G --> E

    F -->|Yes| H{credits_remaining > 0?}
    H -->|No| I[402 Payment Required\nINSUFFICIENT_CREDITS]
    H -->|Yes| J[Atomic UPDATE\nSET credits_remaining = credits_remaining - 1\nWHERE credits_remaining > 0]

    J --> K{Rows affected = 1?}
    K -->|0 — race condition| I
    K -->|1 ✓| BYPASS

    BYPASS --> L[Publish ImageUploadedEvent\n→ RabbitMQ image-events]
    L --> M[IA Service processes image]

    M --> N{Result?}
    N -->|ImageProcessingCompletedEvent| O[✅ Credit consumed\nImage saved to DB]
    N -->|ImageProcessingFailedEvent| P{ErrorCode?}

    P -->|CONTENT_MODERATION_VIOLATION| Q[❌ Credit NOT refunded\nintentional abuse deterrent]
    P -->|MODEL_NOT_FOUND\nPROCESSING_ERROR\nUPLOAD_ERROR| R[♻️ Refund 1 credit\nSET credits_remaining = credits_remaining + 1]
```

---

### **2. Free Credit Seeding — First-Time User Flow**

Shows when and how free credits are initialized for a new user.

```mermaid
sequenceDiagram
    participant U as User
    participant IC as ImagesController
    participant IS as ImageService
    participant CS as CreditService
    participant DB as PostgreSQL user_credits

    U->>IC: POST /api/images/upload (first ever request)
    IC->>IS: UploadAsync(request, isAdmin=false)
    IS->>CS: TryDeductCreditAsync(userId, GptMiniLow)
    CS->>DB: SELECT WHERE user_id=? AND model_tier=GptMiniLow
    DB-->>CS: (empty — no rows)

    CS->>CS: SeedFreeCreditsAsync(userId)
    CS->>DB: INSERT user_credits (GptMiniLow, remaining=5, total=5)
    CS->>DB: INSERT user_credits (Kontext, remaining=3, total=3)
    CS->>DB: INSERT user_credits (GptMiniHigh, remaining=1, total=1)
    DB-->>CS: 3 rows inserted

    CS->>DB: SELECT WHERE user_id=? AND model_tier=GptMiniLow
    DB-->>CS: { remaining: 5 }

    CS->>DB: UPDATE SET remaining=4 WHERE remaining > 0
    DB-->>CS: 1 row affected ✓
    CS-->>IS: Result.Success(true)
    IS->>IS: Continue with image processing...
    IS-->>U: 201 Created
```

---

### **3. End-to-End Flow with Credit System**

Complete flow from frontend to IA Service, including credit gate and refund path.

```mermaid
flowchart LR
    subgraph "User Layer"
        U([User])
        F[Frontend\nAngular]
    end

    subgraph "API Layer"
        GW[Gateway\nYARP]
        PS[Projects Service\n.NET]
        subgraph "Credit Gate"
            CG{Admin or\nflux-schnell?}
            CC[Check user_credits]
            CD[Deduct 1 credit\natomically]
        end
    end

    subgraph "Message Broker"
        MQ[(RabbitMQ)]
    end

    subgraph "IA Processing"
        IA[IA Service\nPython]
        GR[Guardrails]
        PR[AI Processor]
    end

    subgraph "Storage"
        CL[Cloudinary]
        DB[(PostgreSQL\nuser_credits)]
    end

    U -->|Upload + prompt| F
    F -->|HTTP POST /api/images/upload| GW
    GW -->|Route| PS
    PS --> CG

    CG -->|bypass| MQ
    CG -->|check| CC
    CC -->|0 credits| PS
    PS -->|402| F
    CC -->|has credits| CD
    CD -->|success| MQ

    MQ -->|ImageUploadedEvent| IA
    IA --> GR
    GR -->|pass| PR
    PR -->|result image| CL
    CL -->|url| PR

    PR -->|ImageProcessingCompletedEvent| MQ
    MQ -->|consume| PS
    PS -->|save image record| DB

    GR -->|CONTENT_MODERATION_VIOLATION| MQ
    PR -->|PROCESSING_ERROR / UPLOAD_ERROR| MQ
    MQ -->|ImageProcessingFailedEvent| PS
    PS -->|ErrorCode ≠ MODERATION\n→ Refund 1 credit| DB
```

---

### Important note
All file python always should be typed with proper type hints, the code cant allow "any" types usage.

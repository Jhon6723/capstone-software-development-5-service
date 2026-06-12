# PixPro

PixPro is an AI-powered image processing platform built on a microservices architecture. Users can generate images from text prompts or edit existing images using multiple AI models, organized in projects. The platform includes a credit system, subscription tiers, real-time notifications via WebSockets, and an admin panel for user and payment management.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Microservices](#microservices)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Running the System](#running-the-system)
- [API Reference](#api-reference)
- [Credit System](#credit-system)
- [Payment & Subscriptions](#payment--subscriptions)
- [Real-time Notifications](#real-time-notifications)
- [Admin Panel](#admin-panel)
- [Messaging (RabbitMQ)](#messaging-rabbitmq)
- [Cloudflare Tunnel](#cloudflare-tunnel)
- [Documentation](#documentation)

---

## Architecture Overview

```
Frontend (Angular) ──HTTP/WSS──► API Gateway (YARP)
                                      │
              ┌───────────────────────┼───────────────────────┐
              │                       │                       │
         Auth Service          Projects Service       Notifications Service
         (PostgreSQL)          (PostgreSQL)            (Redis + SignalR)
                                      │
                                 RabbitMQ
                                 /    |    \
                           IA Service  Payment Service
                           (FastAPI/   (.NET/
                           PostgreSQL) PostgreSQL)
```

**Single entry point:** everything goes through the Gateway.

- **Local dev:** `http://localhost:5000`
- **Docker:** `http://localhost:8080`

---

## Microservices

| Service | Technology | Port (local) | Responsibility |
|---------|-----------|-------------|----------------|
| **Gateway** | .NET 9 / YARP | `5000` | Reverse proxy, JWT validation, routing |
| **Auth Service** | .NET 9 / ASP.NET Core | `5001` | Registration, login, user management |
| **Projects Service** | .NET 9 / ASP.NET Core | `5002` | Projects, image uploads, credit enforcement |
| **Notifications Service** | .NET 9 / SignalR | `5003` | WebSocket push notifications |
| **IA Service** | Python / FastAPI | — | AI image processing (RabbitMQ consumer) |
| **Payment Service** | .NET 9 / ASP.NET Core | — | Webhook receiver, subscription & credit top-up events |

---

## Tech Stack

| Concern | Choice |
|---------|--------|
| Backend services | .NET 9 (ASP.NET Core) |
| IA service | Python, FastAPI |
| Gateway | YARP (Yet Another Reverse Proxy) |
| Message broker | RabbitMQ |
| Databases | PostgreSQL (Auth, Projects, IA, Payment), MongoDB (Notifications), Redis (Notifications cache) |
| Image storage | Cloudinary |
| AI providers | Pixazo.ai (Flux Schnell), OpenAI (GPT Image 1 Mini), Pollinations.ai (FLUX.1 Kontext) |
| Payments | Stripe / MercadoPago |
| Real-time | WebSockets (WSS) via SignalR |
| Containerization | Docker + Docker Compose |
| CI/CD | GitLab CI |

---

## Prerequisites

- .NET 9 SDK
- Docker and Docker Compose
- A `.env` file in the project root (see `.env.example`)

---

## Running the System

### Option 1 — Local Development (services on host, databases in Docker)

**Step 1:** Start the databases and RabbitMQ in Docker:

```bash
docker-compose up -d db projects-db notifications-db rabbitmq
```

**Step 2:** Build all projects:

```bash
dotnet build
```

**Step 3:** Start each service in a separate terminal:

```bash
# Terminal 1 – Auth Service  → http://localhost:5001
dotnet run --project src/Services/Auth/API

# Terminal 2 – Projects Service  → http://localhost:5002
dotnet run --project src/Services/Projects/API

# Terminal 3 – Notifications Service  → http://localhost:5003
dotnet run --project src/Services/Notifications/API

# Terminal 4 – IA Service (Python)
cd src/Services/IA && uvicorn app.main:app --reload

# Terminal 5 – Gateway  → http://localhost:5000
dotnet run --project src/Gateway/API
```

**Step 4:** Verify with a health check:

```
GET http://localhost:5000/health
```

Expected response: `Healthy`

**Stop everything:**

```bash
# Ctrl+C in each terminal, then:
docker compose down
```

---

### Option 2 — Full Docker

**Step 1:** Build all images:

```bash
docker compose build
```

**Step 2:** Start all services:

```bash
docker compose up -d
```

**Step 3:** Verify all containers are running:

```bash
docker-compose ps
```

**Step 4:** Verify with a health check:

```
GET http://localhost:8080/health
```

**View logs:**

```bash
docker-compose logs -f              # all services
docker-compose logs -f gateway      # gateway only
docker-compose logs -f auth         # auth only
docker-compose logs --tail=50 projects
```

**Stop and clean up:**

```bash
docker-compose down         # stop containers
docker-compose down -v      # stop and delete volumes (wipes databases)
```

---

### Port Reference

**Local development (host):**

| Service | URL |
|---------|-----|
| Gateway | `http://localhost:5000` |
| Auth (debug only) | `http://localhost:5001` |
| Projects (debug only) | `http://localhost:5002` |
| Notifications (debug only) | `http://localhost:5003` |

**Docker:**

| Service | URL |
|---------|-----|
| Gateway (**only external entry point**) | `http://localhost:8080` |

**Infrastructure (both modes):**

| Service | URL |
|---------|-----|
| PostgreSQL (Auth) | `localhost:5432` |
| PostgreSQL (Projects) | `localhost:5433` |
| MongoDB (Notifications) | `localhost:27017` |
| RabbitMQ Management UI | `http://localhost:15672` |

---

### Troubleshooting

| Problem | Solution |
|---------|----------|
| `Port already in use` | `sudo lsof -i :<port>` then `kill -9 <PID>` |
| `Cannot connect to database` | `docker-compose ps db projects-db notifications-db` and check logs |
| `JWT validation failed` | Ensure `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE` are identical across all services in `.env`. Secret must be ≥ 32 characters |
| Microservices can't reach each other in Docker | Use service names (e.g. `http://auth:8081`), not `localhost` |

---

## API Reference

All requests go through the Gateway. Replace `<base>` with `http://localhost:5000` (local) or `http://localhost:8080` (Docker).

### Authentication

**Register:**
```
POST <base>/api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Test123!@#",
  "firstName": "Test",
  "lastName": "User"
}
```

**Login:**
```
POST <base>/api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Test123!@#"
}
```

Copy the `token` from the response and use it as `Authorization: Bearer <token>` on all protected endpoints.

### Images

**Generate from text (Feature 0 — Generator):**
```
POST <base>/api/images/upload
Authorization: Bearer <token>
Content-Type: application/json

{
  "projectId": "<project-uuid>",
  "prompt": "a cat with a microphone singing",
  "feature": 0
}
```

**Edit an existing image (Feature 1 — Editor):**
```
POST <base>/api/images/upload
Authorization: Bearer <token>
Content-Type: multipart/form-data

file=@your-image.jpg
projectId=<project-uuid>
prompt=make this cyberpunk style
feature=1
```

Both endpoints return `202 Accepted` immediately. The processed image arrives via WebSocket notification.

### Projects

```
GET <base>/api/projects
Authorization: Bearer <token>
```

---

## Credit System

Credit enforcement lives in the **Projects Service**, before any message is published to RabbitMQ. The IA Service has no billing awareness.

### Free Tier Allocation

| Model | Free Credits | Notes |
|-------|-------------|-------|
| `kontext` (FLUX.1 Kontext) | 5 | Default img2img — Pollinations.ai, ~$0.005/image |
| `gpt-image-1.5-low` | 3 | OpenAI flagship low — ~$0.009/image |
| `nanobanana-low` (NanaBanana V1) | 3 | Entry NanaBanana — Gemini 2.5 Flash, ~$0.020/image |
| `nanobanana-medium` (NanaBanana V2) | 2 | Mid NanaBanana — Gemini 3.1 Flash, ~$0.040/image |
| `nanobanana-max` (NanaBanana Pro) | 1 | Max NanaBanana — Gemini 3 Pro, ~$0.090/image |
| `gpt-image-1.5-medium` | 1 | OpenAI flagship medium — ~$0.034/image |
| `flux-schnell` (Budget) | **Unlimited** | Text-to-image — Pixazo.ai, ~$0.0012/image |

### AI Models

| Tier | Model | Provider | Task | Cost/image |
|------|-------|----------|------|------------|
| Budget | `flux-schnell` | Pixazo.ai | Text-to-image | ~$0.0012 |
| Default ⭐ | `kontext` (FLUX.1 Kontext) | Pollinations.ai | Image-to-image | ~$0.005 |
| GPT15 Low | `gpt-image-1.5-low` | OpenAI | Image-to-image | ~$0.009 |
| NanaBanana Low | `nanobanana-low` (Gemini 2.5 Flash) | NanaBanana API | Image-to-image | ~$0.020 (1K · 4 credits) |
| NanaBanana Medium | `nanobanana-medium` (Gemini 3.1 Flash) | NanaBanana API | Image-to-image | ~$0.040 (1K · 8 credits) |
| NanaBanana Max | `nanobanana-max` (Gemini 3 Pro) | NanaBanana API | Image-to-image | ~$0.090 (1K · 18 credits) |
| GPT15 Medium | `gpt-image-1.5-medium` | OpenAI | Image-to-image | ~$0.034 |

### Future Model Candidates

Models not yet integrated but tracked for future adoption:

| Model | Provider | Task | Cost/image | Elo (editing) | Blocker |
|-------|----------|------|------------|----------------|---------|
| `grok-imagine-image` | xAI | Image-to-image | ~$0.022 (output $0.020 + input $0.002) | 1239 (#5 leaderboard) | No xAI balance — separate billing account required |

> `grok-imagine-image` would slot between `nb-low` ($0.020) and `nb-medium` ($0.040) with near-`nb-medium` quality at half the price. OpenAI-compatible API — trivial to integrate when xAI billing is available.

### Credit Logic

- **Admins** bypass all credit checks entirely.
- **`flux-schnell`** is always unlimited.
- If a user has `0` credits remaining, the request returns `402 INSUFFICIENT_CREDITS`.
- If the IA Service returns `ImageProcessingFailedEvent`, the credit is **refunded** — unless the error is `CONTENT_MODERATION_VIOLATION`.

---

## Payment & Subscriptions

### Subscription Tiers

| Tier | Price | `kontext` | `gpt15-low` | `nb-low` | `nb-medium` | `nb-max` | `gpt15-medium` | Reset |
|------|-------|-----------|-------------|----------|-------------|----------|----------------|-------|
| Free | $0 | 5 | 3 | 3 | 2 | 1 | 1 | Never (one-time) |
| Basic | $4.99/mo | 140 | 100 | 35 | 17 | 7 | 10 | Monthly |
| Pro | $14.99/mo | 420 | 300 | 105 | 52 | 21 | 30 | Monthly |
| Unlimited | TBD | ∞ | ∞ | ∞ | ∞ | ∞ | ∞ | — |

> IA cost per tier: Basic ~$3.61 (65% margin on $4.99) · Pro ~$10.83 (65% margin on $14.99)

### Credit Top-Up (Pay-as-you-go)

Users can purchase credit packs at any time for **$1 per pack**. Top-up prices carry a ~2× markup over API cost to incentivize subscriptions:

| Model Tier | Credits per $1 | Cost per image | API cost | Markup |
|------------|----------------|----------------|----------|--------|
| `kontext` | 100 | ~$0.010 | $0.005 | 2× |
| `gpt15_low` | 55 | ~$0.018 | $0.009 | 2× |
| `nanobanana_low` | 25 | ~$0.040 | $0.020 | 2× |
| `nanobanana_medium` | 12 | ~$0.083 | $0.040 | 2× |
| `nanobanana_max` | 3 | ~$0.333 | $0.090 | 3.7× |
| `gpt15_medium` | 14 | ~$0.071 | $0.034 | 2.1× |

```
POST <base>/api/payments/credits/checkout
Authorization: Bearer <token>
Content-Type: application/json

{
  "modelTier": "nanobanana_medium"  // kontext | gpt15_low | nanobanana_low | nanobanana_medium | nanobanana_max | gpt15_medium
}

Response 200:
{
  "checkoutUrl": "https://checkout.stripe.com/..."
}
```

Purchased credits **never expire** and stack with subscription credits.

### Payment Flow

The Payment Service receives webhooks from Stripe / MercadoPago, records the payment, and publishes an event to RabbitMQ. The Projects Service consumes the event and activates credits — no direct service-to-service calls.

**Webhook idempotency:** `provider_payment_id` is a `UNIQUE` constraint — duplicate webhooks are safely ignored.

---

## Real-time Notifications

The Notifications Service forwards processing events to clients via **WebSocket Secure (WSS)**. Authentication uses JWT passed as a query string parameter: `?access_token=<jwt>`.

### WebSocket Message Format

```json
{
  "type": "IMAGE_PROCESSING_COMPLETED",
  "notification": {
    "id": "notif-uuid",
    "userId": "user-uuid",
    "title": "Image Processing Completed",
    "message": "Your image has been processed successfully!",
    "metadata": {
      "eventType": "ImageProcessingCompleted",
      "imageId": "img-uuid",
      "projectId": "proj-uuid",
      "processedImageUrls": ["url1", "url2"]
    }
  },
  "timestamp": "2026-05-31T10:30:00Z"
}
```

### Notification Types

| Type | Trigger |
|------|---------|
| `IMAGE_UPLOADED` | Image upload accepted |
| `IMAGE_PROCESSING_COMPLETED` | AI processing finished |
| `IMAGE_PROCESSING_FAILED` | AI processing error |

The WebSocket API is formally documented in `docs/asyncapi.yaml` (AsyncAPI 3.x). To generate the HTML docs:

```bash
npx @asyncapi/generator docs/asyncapi.yaml @asyncapi/html-template -o docs/asyncapi-html/
```

---

## Admin Panel

All admin endpoints require `[Authorize(Policy = "Admin")]`.

| Capability | Method | Endpoint | Service |
|------------|--------|----------|---------|
| List all users | `GET` | `/api/auth/users?page=1&pageSize=20` | Auth |
| Block user | `PUT` | `/api/auth/users/{id}/block` | Auth |
| Unblock user | `PUT` | `/api/auth/users/{id}/unblock` | Auth |
| View user credit balance | `GET` | `/api/projects/admin/users/{id}/credits` | Projects |
| Manually assign subscription | `PATCH` | `/api/projects/admin/users/{id}/subscription` | Projects |
| View user payment history | `GET` | `/api/payments/admin/users/{id}/history` | Payment |
| Global revenue summary | `GET` | `/api/payments/admin/summary` | Payment |

**Block/unblock user:**
```
PUT /api/auth/users/{id}/block     → 204 No Content
PUT /api/auth/users/{id}/unblock   → 204 No Content
```

No request body required. Admin accounts cannot be blocked (`Auth.CannotBlockAdmin`). A blocked user cannot log in (`Auth.UserBlocked`).

---

## Messaging (RabbitMQ)

| Exchange | Type | Publisher | Consumer | Events |
|----------|------|-----------|----------|--------|
| `image-events` | Fanout | Projects Service | IA Service | `ImageUploadedEvent` |
| `processed-image-events` | Fanout | IA Service | Notifications Service, Projects Service | `ImageProcessingCompletedEvent`, `ImageProcessingFailedEvent` |
| `payment-events` | Fanout | Payment Service | Projects Service | `SubscriptionActivatedEvent`, `CreditPurchasedEvent` |

---

## Cloudflare Tunnel

To expose the local backend publicly (e.g. for mobile/frontend testing) using a free Cloudflare Tunnel:

```bash
# Start tunnel in background
screen -S tunnel -dm cloudflared tunnel --url http://localhost:8080

# Wait and view the public URL
sleep 3 && screen -r tunnel
# Look for: https://xxxx.trycloudflare.com
# Press Ctrl+A then D to detach without killing the tunnel

# Stop the tunnel
screen -X -S tunnel quit
```

Swagger is available at `https://xxxx.trycloudflare.com/swagger/index.html` (only when `ASPNETCORE_ENVIRONMENT=Development`).

> The public URL changes on every restart.

---

## Documentation

| Document | Description |
|----------|-------------|
| [`docs/PASOS-PARA-CORRER.md`](docs/PASOS-PARA-CORRER.md) | Step-by-step guide to run the full system (local & Docker) |
| [`docs/admin-and-payments-architecture.md`](docs/admin-and-payments-architecture.md) | Admin features, Payment Service architecture, subscription flows |
| [`docs/ia-architecture/ia-architecture.md`](docs/ia-architecture/ia-architecture.md) | IA Service architecture, AI model selection, credit system design |
| [`docs/image-processing-integration.md`](docs/image-processing-integration.md) | Event-driven image processing flow, RabbitMQ schema, DB schema |
| [`docs/asyncapi.yaml`](docs/asyncapi.yaml) | AsyncAPI 3.x specification for WebSocket channels |
| [`docs/asyncapi-html/`](docs/asyncapi-html/) | Generated HTML API documentation for WebSocket contracts |
| [`docs/CLOUDFLARE_TUNNEL.md`](docs/CLOUDFLARE_TUNNEL.md) | Quick guide for exposing local backend with Cloudflare Tunnel |
| [`docs/wss-user-histories/`](docs/wss-user-histories/) | User stories for WSS security and AsyncAPI CI/CD integration |

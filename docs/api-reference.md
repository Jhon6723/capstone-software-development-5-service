# PixPro — Complete API Reference

All requests go through the Gateway. Use `http://localhost:8080` (Docker, preferred) or `http://localhost:5000` (local dev).

Protected endpoints require `Authorization: Bearer <token>` unless stated otherwise.

---

## Table of Contents

- [Auth](#auth)
- [Projects](#projects)
- [Images](#images)
- [Credits](#credits)
- [Notifications](#notifications)
- [WebSocket](#websocket)
- [Response Shapes](#response-shapes)
- [Error Handling](#error-handling)
- [YARP Routing & Internal Addresses](#yarp-routing--internal-addresses)
- [CORS](#cors)

---

## Auth

Base path: `/api/auth` → proxied to `http://auth:8081`

Auth routes are **public** — no JWT required unless noted.

### `POST /api/auth/register`

```
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Test123!@#",
  "firstName": "Test",
  "lastName": "User"
}
```

| Status | Body |
|--------|------|
| `201 Created` | `UserResponse` |
| `400 Bad Request` | `ErrorResponse` (validation) |
| `409 Conflict` | `ErrorResponse` (email already exists) |
| `429 Too Many Requests` | `ErrorResponse` (rate limit: 10 req/min per IP) |

---

### `POST /api/auth/login`

```
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Test123!@#"
}
```

| Status | Body |
|--------|------|
| `200 OK` | `LoginResponse` |
| `401 Unauthorized` | `ErrorResponse` (`Auth.InvalidCredentials` or `Auth.UserBlocked`) |
| `429 Too Many Requests` | `ErrorResponse` (rate limit: 10 req/min per IP) |

---

### `GET /api/auth/me` 🔒

Returns claims from the validated JWT for the currently authenticated user.

```
GET /api/auth/me
Authorization: Bearer <token>
```

| Status | Body |
|--------|------|
| `200 OK` | `CurrentUserResponse` |
| `401 Unauthorized` | — |

---

### `GET /api/auth/users/{email}` 🔒 Admin

```
GET /api/auth/users/user@example.com
Authorization: Bearer <admin-token>
```

| Status | Body |
|--------|------|
| `200 OK` | `UserResponse` |
| `404 Not Found` | `ErrorResponse` |

> **Note:** This is the only implemented user-lookup endpoint. The paginated `GET /api/auth/users?page=1&pageSize=20` listed in admin architecture docs is **planned but not yet implemented**.

---

### `PUT /api/auth/users/{userId}/block` 🔒 Admin

Block a user account. Blocked users cannot log in, and any existing valid JWT is immediately revoked via Redis blacklist (TTL = JWT expiry).

```
PUT /api/auth/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/block
Authorization: Bearer <admin-token>
```

| Status | Body |
|--------|------|
| `204 No Content` | — |
| `400 Bad Request` | `ErrorResponse` (`Auth.CannotBlockAdmin` — admin accounts cannot be blocked) |
| `404 Not Found` | `ErrorResponse` |

---

### `PUT /api/auth/users/{userId}/unblock` 🔒 Admin

Restore access for a previously blocked user.

```
PUT /api/auth/users/3fa85f64-5717-4562-b3fc-2c963f66afa6/unblock
Authorization: Bearer <admin-token>
```

| Status | Body |
|--------|------|
| `204 No Content` | — |
| `404 Not Found` | `ErrorResponse` |

---

## Projects

Base path: `/api/projects` → proxied to `http://projects:8082`

All endpoints require authentication.

### `GET /api/projects`

List projects for the authenticated user. Optional search filter.

```
GET /api/projects?search=my-project
Authorization: Bearer <token>
```

| Status | Body |
|--------|------|
| `200 OK` | `List<ProjectResponse>` |

---

### `GET /api/projects/{id}`

```
GET /api/projects/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer <token>
```

| Status | Body |
|--------|------|
| `200 OK` | `ProjectResponse` |
| `400 Bad Request` | `{ "error": "Invalid project ID format." }` |
| `404 Not Found` | `{ "error": "Project not found." }` |

---

### `POST /api/projects`

Create a new project.

```
POST /api/projects
Authorization: Bearer <token>
Content-Type: application/json

{
  "name": "My Project",
  "description": "Optional description"
}
```

| Status | Body |
|--------|------|
| `201 Created` | `ProjectResponse` |
| `400 Bad Request` | `{ "error": "..." }` |

---

### `GET /api/projects/{id}/images`

Paginated list of images in a project.

```
GET /api/projects/3fa85f64-5717-4562-b3fc-2c963f66afa6/images?page=1&limit=12
Authorization: Bearer <token>
```

| Query param | Default | Description |
|-------------|---------|-------------|
| `page` | `1` | Page number |
| `limit` | `12` | Items per page |

| Status | Body |
|--------|------|
| `200 OK` | `ImageListResponse` |
| `400 Bad Request` | `{ "error": "Invalid project ID format." }` |

---

### `GET /api/projects/admin/users/{userId}/credits` 🔒 Admin

See `docs/admin-and-payments-architecture.md`.

### `PATCH /api/projects/admin/users/{userId}/subscription` 🔒 Admin

See `docs/admin-and-payments-architecture.md`.

---

## Images

Base path: `/api/images` → proxied to `http://projects:8082`

Gateway timeout for this route: **10 minutes** (AI processing).

All endpoints require authentication.

### `POST /api/images/upload`

Accepts **both** Feature 0 (Generator) and Feature 1 (Editor) via the same endpoint.
Always sent as `multipart/form-data`.

**Feature 0 — Text-to-Image (Generator):**

```
POST /api/images/upload
Authorization: Bearer <token>
Content-Type: multipart/form-data

prompt=a cat with a microphone singing
feature=0
projectId=3fa85f64-5717-4562-b3fc-2c963f66afa6
parameters={"model":"flux-schnell"}
```

- `file` is optional for Feature 0.
- Default model is `kontext` if `model` is omitted from `parameters`.

**Feature 1 — Image-to-Image (Editor):**

```
POST /api/images/upload
Authorization: Bearer <token>
Content-Type: multipart/form-data

file=@your-image.jpg
prompt=make this cyberpunk style
feature=1
projectId=3fa85f64-5717-4562-b3fc-2c963f66afa6
parameters={"model":"kontext","strength":0.75}
```

- `file` is **required** for Feature 1.
- `model` inside `parameters` is **required** for Feature 1. Valid values: `kontext`, `gpt-image-1.5-low`, `gpt-image-1.5-medium`, `nanobanana-low`, `nanobanana-medium`, `nanobanana-max`.

**Form fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `file` | File | Required for Feature 1 | Source image |
| `prompt` | string | Yes | Processing prompt |
| `feature` | int | Yes | `0` = Generator, `1` = Editor |
| `projectId` | UUID | Yes | Target project |
| `parameters` | JSON string | Optional | `ProcessingParameters` (see below) |

**`parameters` JSON fields:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `model` | string | `kontext` | AI model key |
| `width` | int | 512 | Output width (64–2048) |
| `height` | int | 512 | Output height (64–2048) |
| `num_inference_steps` | int | 20 | AI refinement steps (1–100) |
| `strength` | float | 0.75 | How much AI changes the original (img2img only, 0.0–1.0) |
| `guidance_scale` | float | 7.5 | Prompt adherence (Pollinations only, 1.0–20.0) |
| `quantity` | int | 1 | Number of variations (1–10) |

| Status | Body |
|--------|------|
| `201 Created` | `ImageUploadResponse` |
| `400 Bad Request` | `{ "error": "..." }` (validation) |
| `401 Unauthorized` | `{ "error": "Invalid token: user ID not found." }` |
| `402 Payment Required` | `{ "error": "INSUFFICIENT_CREDITS" }` |
| `429 Too Many Requests` | `{ "error": "FLUX_DAILY_LIMIT_EXCEEDED" }` (flux-schnell daily cap reached) |

> The image is processed asynchronously. The result arrives via WebSocket (`IMAGE_PROCESSING_COMPLETED`).

---

### `DELETE /api/images/{id}`

Delete an image. Admins can delete any image; regular users can only delete their own.

```
DELETE /api/images/3fa85f64-5717-4562-b3fc-2c963f66afa6
Authorization: Bearer <token>
```

| Status | Body |
|--------|------|
| `204 No Content` | — |
| `403 Forbidden` | `{ "error": "You do not own this image." }` |
| `404 Not Found` | `{ "error": "Image not found." }` |

---

## Credits

Base path: `/api/credits` → proxied to `http://projects:8082`

### `GET /api/credits/me` 🔒

Get the credit balance of the authenticated user for all model tiers.

```
GET /api/credits/me
Authorization: Bearer <token>
```

| Status | Body |
|--------|------|
| `200 OK` | `CreditBalanceResponse` |
| `401 Unauthorized` | — |

**Example response:**

```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "subscriptionTier": "Free",
  "credits": [
    {
      "modelTier": "Kontext",
      "remaining": 4,
      "total": 5,
      "resetAt": null
    },
    {
      "modelTier": "Gpt15Low",
      "remaining": 3,
      "total": 3,
      "resetAt": null
    }
  ]
}
```

> Credits are seeded lazily on first use per model tier. If a tier is not listed, it hasn't been used yet. `flux-schnell` (Pixazo/Generator) is free but capped at **200 requests per rolling 24-hour window** — tracked via `UserCredit.Pixazo` row.

---

## Notifications

Base path: `/api/notifications` → proxied to `http://notifications:8083`

Notifications Service uses **CQRS with MediatR**. All endpoints require authentication.

### `POST /api/notifications` 🔒

Create a notification manually (internal use).

```
POST /api/notifications
Authorization: Bearer <token>
Content-Type: application/json

{
  "userId": "user-uuid",
  "title": "...",
  "message": "..."
}
```

| Status | Body |
|--------|------|
| `201 Created` | Notification object |

---

### `GET /api/notifications/{id}` 🔒

```
GET /api/notifications/notif-uuid
Authorization: Bearer <token>
```

| Status | Body |
|--------|------|
| `200 OK` | Notification object |
| `404 Not Found` | `{ "error": "..." }` |

---

### `GET /api/notifications/user/{userId}` 🔒

Paginated list of notifications for a user.

```
GET /api/notifications/user/user-uuid?pageSize=20&page=1
Authorization: Bearer <token>
```

| Query param | Default |
|-------------|---------|
| `pageSize` | `20` |
| `page` | `1` |

| Status | Body |
|--------|------|
| `200 OK` | Paginated notification list |

---

### `GET /api/notifications/user/{userId}/unread` 🔒

All unread notifications for a user.

```
GET /api/notifications/user/user-uuid/unread
Authorization: Bearer <token>
```

---

### `GET /api/notifications/user/{userId}/unread-count` 🔒

```
GET /api/notifications/user/user-uuid/unread-count
Authorization: Bearer <token>
```

**Response:**
```json
{ "count": 3 }
```

---

### `PUT /api/notifications/{id}/read` 🔒

Mark a single notification as read.

```
PUT /api/notifications/notif-uuid/read
Authorization: Bearer <token>
```

| Status | Body |
|--------|------|
| `204 No Content` | — |

---

### `PUT /api/notifications/user/{userId}/read-all` 🔒

Mark all notifications for a user as read.

```
PUT /api/notifications/user/user-uuid/read-all
Authorization: Bearer <token>
```

| Status | Body |
|--------|------|
| `204 No Content` | — |

---

### `DELETE /api/notifications/{id}` 🔒

```
DELETE /api/notifications/notif-uuid
Authorization: Bearer <token>
```

| Status | Body |
|--------|------|
| `204 No Content` | — |

---

## WebSocket

Base path: `/api/websocket` → proxied to `http://notifications:8083`

### `GET /api/websocket/connect` 🔒

Upgrade to WebSocket. JWT must be passed as query param.

```
ws://localhost:8080/api/websocket/connect?access_token=<jwt>
```

| Response | Meaning |
|----------|---------|
| WebSocket upgrade | Connected |
| `400` | Not a WebSocket request |
| `401` | Invalid/missing token |
| `429` | Connection limit exceeded |

**Security features:**
- Per-IP connection rate limiting enforced by `WebSocketConnectionManager`.
- IPs that exceed limits are automatically banned.

---

### `GET /api/websocket/status` 🔒

Requires a valid JWT. Returns service health and active connection count.

```json
{
  "status": "WebSocket service is running",
  "activeConnections": 12,
  "timestamp": "2026-06-12T13:00:00Z"
}
```

---

### `GET /api/websocket/banned` 🔒 Admin

Returns the list of currently banned IP addresses.

```json
{
  "bannedIps": ["192.168.1.50"],
  "timestamp": "2026-06-12T13:00:00Z"
}
```

---

### `DELETE /api/websocket/banned/{ipAddress}` 🔒 Admin

Manually unban an IP address.

```
DELETE /api/websocket/banned/192.168.1.50
Authorization: Bearer <admin-token>
```

| Status | Body |
|--------|------|
| `200 OK` | `{ "message": "IP '...' has been unbanned" }` |
| `404 Not Found` | `{ "message": "IP '...' is not banned" }` |

---

## Response Shapes

### `UserResponse`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "auth0Id": null,
  "email": "user@example.com",
  "createdAt": "2026-01-01T00:00:00Z"
}
```

### `LoginResponse`

```json
{
  "token": "eyJhbGci...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "user": { /* UserResponse */ }
}
```

### `CurrentUserResponse` (`GET /api/auth/me`)

```json
{
  "sub": "user-uuid-or-auth0-sub",
  "email": "user@example.com",
  "name": null,
  "picture": null,
  "role": "User",
  "authProvider": "Local",
  "authenticatedAt": "2026-06-12T13:00:00Z"
}
```

> `authProvider` is `"Auth0"` when a `name` claim is present (Auth0 token), otherwise `"Local"`.

### `ProjectResponse`

```json
{
  "id": "uuid",
  "name": "My Project",
  "description": "Optional",
  "createdAt": "2026-01-01T00:00:00Z",
  "updatedAt": null,
  "imageCount": 5
}
```

### `ImageUploadResponse`

Returned immediately on upload acceptance. The processed result arrives later via WebSocket.

```json
{
  "imageId": "uuid",
  "fileName": "original.jpg",
  "url": "https://res.cloudinary.com/...",
  "secureUrl": "https://res.cloudinary.com/...",
  "format": "jpg",
  "sizeInBytes": 204800,
  "width": 1024,
  "height": 1024,
  "uploadedAt": "2026-06-12T13:00:00Z",
  "feature": 1
}
```

### `ImageResponse`

```json
{
  "id": "uuid",
  "projectId": "uuid",
  "fileName": "processed_abc.jpg",
  "contentType": "image/jpeg",
  "filePath": "https://res.cloudinary.com/...",
  "cloudinaryPublicId": "pixpro/abc123",
  "secureUrl": "https://res.cloudinary.com/...",
  "format": "jpg",
  "sizeInBytes": 0,
  "width": 0,
  "height": 0,
  "status": "processed",
  "ownerId": "uuid",
  "createdAt": "2026-06-12T13:00:00Z"
}
```

### `ImageListResponse`

```json
{
  "data": [ /* ImageResponse[] */ ],
  "total": 45,
  "page": 1,
  "pageSize": 12,
  "hasMore": true
}
```

### `CreditBalanceResponse`

```json
{
  "userId": "uuid",
  "subscriptionTier": "Free",
  "credits": [
    {
      "modelTier": "Kontext",
      "remaining": 4,
      "total": 5,
      "resetAt": null
    }
  ]
}
```

### `ErrorResponse`

```json
{
  "code": "Auth.InvalidCredentials",
  "message": "Invalid email or password.",
  "type": "Unauthorized"
}
```

Error types: `Validation` → `400`, `NotFound` → `404`, `Conflict` → `409`, `Unauthorized` → `401`.

---

## Error Handling

| HTTP Status | Meaning |
|-------------|---------|
| `400 Bad Request` | Validation error or bad input |
| `401 Unauthorized` | Missing or invalid JWT |
| `402 Payment Required` | `INSUFFICIENT_CREDITS` — user has 0 credits for requested model |
| `403 Forbidden` | Authenticated but not authorized (e.g. deleting another user's image) |
| `404 Not Found` | Resource does not exist |
| `409 Conflict` | Duplicate resource (e.g. email already registered) |
| `429 Too Many Requests` | Rate limit exceeded (auth: 10 req/min per IP; WebSocket connection limit) |
| `500 Internal Server Error` | Unexpected server error |

---

## YARP Routing & Internal Addresses

The Gateway (YARP) proxies all requests. **Internal Docker service addresses:**

| Service | Internal address | Routes |
|---------|-----------------|--------|
| Auth | `http://auth:8081` | `/api/auth/**` |
| Projects | `http://projects:8082` | `/api/projects/**`, `/api/images/**`, `/api/credits/**` |
| Notifications | `http://notifications:8083` | `/api/notifications/**`, `/api/websocket/**` |
| IA | `http://ia:8084` | `/api/ia/**` |

Gateway timeouts:
- Default routes: standard ASP.NET timeout
- `/api/images/**`: **10 minutes** (AI processing time)
- `/api/projects/**`: **5 minutes**

Health checks run every 30 seconds per cluster. Failed destinations are automatically removed (passive policy: `TransportFailureRate`, reactivation: 1 minute).

---

## CORS

Allowed origins configured in Gateway:

| Origin | Environment |
|--------|-------------|
| `http://localhost:3000` | Local dev |
| `http://localhost:5173` | Local dev (Vite) |
| `http://localhost:4200` | Local dev (Angular) |
| `http://localhost` | Local dev |
| `https://capstone-software-development-5-cli.vercel.app` | Production frontend |

---

## Implementation Notes

- **Payment Service** is designed and documented in `docs/admin-and-payments-architecture.md` but **not yet implemented** (no source directory under `src/Services/`). The endpoints `POST /api/payments/credits/checkout`, `GET /api/payments/admin/**` are planned.
- **Admin `GET /api/auth/users` (paginated list)** described in admin architecture docs is **not yet implemented**. Only `GET /api/auth/users/{email}` exists in code.
- Credits are seeded lazily on first use per model tier (not on registration).
- `UserIdHelper.DeriveGuid` is used in Projects and Notifications services to normalize Auth0 `sub` strings (which may not be valid GUIDs) into deterministic GUIDs.

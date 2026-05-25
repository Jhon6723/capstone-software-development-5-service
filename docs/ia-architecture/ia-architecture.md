## **Simplified IA Microservice Architecture**

### **Proposed Structure (3 Layers)**

```
src/Services/IA/
├── app/
│   ├── __init__.py
│   ├── main.py                    # FastAPI entry point
│   ├── config.py                 # Configuration
│   ├── models/                   # Data models
│   │   ├── __init__.py
│   │   ├── events.py             # RabbitMQ event models
│   │   ├── database.py           # Database models
│   │   └── schemas.py           # Pydantic schemas
│   ├── services/                 # Business logic
│   │   ├── __init__.py
│   │   ├── image_processor.py    # Image processing logic
│   │   ├── cloudinary_service.py # Cloudinary operations
│   │   └── database_service.py   # Database operations
│   ├── infrastructure/           # External integrations
│   │   ├── __init__.py
│   │   ├── database.py          # DB connection
│   │   ├── rabbitmq.py          # RabbitMQ consumer/publisher
│   │   └── cloudinary.py         # Cloudinary client
│   └── processors/              # AI processors (plug-in)
│       ├── __init__.py
│       ├── base.py
│       ├── huggingface.py
│       └── mock.py
├── alembic/                      # DB migrations
├── requirements.txt
└── Dockerfile
```

### **Layer Responsibilities**

**Models Layer** - Pure data structures
- Events (RabbitMQ messages)
- Database tables (SQLAlchemy)
- Pydantic schemas (validation)

**Services Layer** - Business logic
- `ImageProcessor`: Orchestrate processing workflow
- `CloudinaryService`: Upload/delete images
- `DatabaseService`: CRUD operations

**Infrastructure Layer** - External systems
- Database connection
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
| **Default** ⭐ | `gpt-image-1-mini-low` | OpenAI (`quality=low`) | image edit | ~$0.011 | ⭐⭐⭐⭐ |
| Standard | `kontext` (FLUX.1 Kontext) | Pollinations.ai | image-to-image | ~$0.04 | ⭐⭐⭐⭐⭐ |
| AI Reasoning | `gpt-image-1-mini-high` | OpenAI (`quality=high`) | image edit | ~$0.167 | ⭐⭐⭐⭐⭐ |

### **Model Comparison**

| Model | Parameters | Purpose | Provider | Quality |
|-------|-------------|---------|----------|---------|
| **GPT Image 1 Mini** ⭐ | N/A (closed) | Reasoning-based image editing | OpenAI | ⭐⭐⭐⭐⭐ |
| FLUX.1 Kontext | ~12B | Instruction-based image editing | Pollinations.ai (FLUX family) | ⭐⭐⭐⭐⭐ |

### **Price Comparison**

All figures below are per single output image. Pollinations prices verified live against `GET https://gen.pollinations.ai/models`.

| Tier | Model | Provider | Approx. cost per image | Approx. latency | Notes |
|------|-------|----------|------------------------|-----------------|-------|
| **Default** ⭐ | `gpt-image-1-mini` (low) | OpenAI | **$0.011** | ~5–8 s | Already paid OpenAI account |
| Standard | `kontext` (FLUX.1 Kontext) | Pollinations.ai | ~$0.04 (0.04 Pollen) | ~6–10 s | $2 ≈ 50 edits |
| AI Reasoning | `gpt-image-1-mini` (high) | OpenAI | $0.167 | ~20–30 s | Premium quality |

**Bottom line:**
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
    DEFAULT      = "gpt-image-1-mini-low"   # OpenAI gpt-image-1-mini, quality=low  (~$0.011/img)
    STANDARD     = "kontext"                # Pollinations FLUX.1 Kontext           (~$0.04/img)
    AI_REASONING = "gpt-image-1-mini-high"  # OpenAI gpt-image-1-mini, quality=high (~$0.167/img)
```

The user can explicitly select a tier via the `model` parameter in the upload event. If no model is specified, the system uses **`gpt-image-1-mini-low`** — the cheapest reliable option, paid for on the existing OpenAI account.


## **System Flows**

### **1. Image Upload Flow**

This is what happens when a user uploads an image for AI editing:

```
User → Frontend → Gateway → Projects Service → Cloudinary (store original image)
                                                      ↓
                                                RabbitMQ (send event)
                                                      ↓
                                                IA Service (process)
```

Steps:
1. User uploads image with prompt in the Frontend
2. Request goes through the API Gateway
3. Projects Service receives and saves the original image to Cloudinary
4. Projects Service sends a message to RabbitMQ saying "new image to process"
5. IA Service receives the message and starts processing

---

### **2. Image Processing Flow**

This is what happens inside the IA Service when processing an image:

```
RabbitMQ → Consumer → Event Processor → Image Processor → HuggingFace API
                                                              ↓
                                        Image Processor ← AI result
                                                              ↓
                                        Cloudinary (store result)
                                                              ↓
                                        PostgreSQL (save job record)
                                                              ↓
                                        RabbitMQ (send completion)
```

Steps:
1. Consumer gets the event from RabbitMQ
2. Event Processor validates the data
3. Image Processor calls the AI model (HuggingFace or OpenAI)
4. AI returns the edited image
5. Result is uploaded to Cloudinary
6. Job status is saved to PostgreSQL
7. Completion message is sent to RabbitMQ

---

### **3. Notification Flow**

This is how the user knows when their image is ready:

```
RabbitMQ → Notifications Service → WebSocket → Frontend → User sees result
```

Steps:
1. Notifications Service gets the completion event
2. Sends real-time notification via WebSocket
3. Frontend receives it and shows the processed image to the user

---

### **4. Complete End-to-End Flow**

Putting it all together:

```
1. User uploads image → Frontend → Projects Service → Cloudinary

2. Projects Service → RabbitMQ → IA Service

3. IA Service → HuggingFace/OpenAI → Gets edited image

4. IA Service → Cloudinary (save result) + PostgreSQL (save record) + RabbitMQ (notify)

5. RabbitMQ → Notifications Service → Frontend → User sees the result
```

---

### **Components Overview**

**PixPro Platform (our system):**
- **Frontend** - React web app where users interact
- **Gateway** - Routes all requests to the right service
- **Projects Service** - Handles image uploads
- **IA Service** - Runs the AI image editing
- **Notifications Service** - Sends real-time updates
- **IA Database** - PostgreSQL storing processing jobs

**External Systems:**
- **Cloudinary** - Stores all images (original and edited)
- **RabbitMQ** - Message broker connecting services
- **HuggingFace** - Provides AI models (SD, FLUX)
- **OpenAI** - Provides GPT Image model

---

## **Processing Parameters**

When sending an image for processing, you can customize the AI behavior with these parameters:

### **Parameter Reference**

| Parameter | Type | Range | Default | Description |
|-----------|------|-------|---------|-------------|
| `width` | int | 64-2048 | 512 | Output image width in pixels |
| `height` | int | 64-2048 | 512 | Output image height in pixels |
| `num_inference_steps` | int | 1-100 | 20 | AI refinement steps. Higher = better quality but slower |
| `strength` | float | 0.0-1.0 | 0.75 | How much AI can change the original image. 0.2 = subtle, 1.0 = complete transformation |
| `guidance_scale` | float | 1.0-20.0 | 7.5 | How closely AI follows the prompt. Higher = more literal |
| `quantity` | int | 1-10 | 1 | Number of image variations to generate |
| `model` | string | - | `stable-diffusion-v1-5` | AI model to use. Options: `stable-diffusion-v1-5`, `flux-2-dev`, `flux-1-kontext-dev` |

### **Usage Examples**

**Subtle style change (keep original composition):**
```json
{
  "Prompt": "Make this look like a watercolor painting",
  "Parameters": {
    "width": 512,
    "height": 512,
    "strength": 0.3,
    "num_inference_steps": 20,
    "model": "stable-diffusion-v1-5"
  }
}
```

**Creative transformation:**
```json
{
  "Prompt": "Transform into cyberpunk city at night, neon lights, futuristic",
  "Parameters": {
    "width": 768,
    "height": 512,
    "strength": 0.85,
    "guidance_scale": 12,
    "num_inference_steps": 50,
    "model": "flux-2-dev"
  }
}
```

**Generate multiple options:**
```json
{
  "Prompt": "Portrait with different artistic styles",
  "Parameters": {
    "width": 512,
    "height": 768,
    "quantity": 4,
    "num_inference_steps": 30,
    "model": "stable-diffusion-v1-5"
  }
}
```

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

| Model | Best For | Cost | Quality |
|-------|----------|------|---------|
| `stable-diffusion-v1-5` | High volume, quick edits | ⭐ Cheapest | ⭐⭐⭐⭐ |
| `flux-2-dev` | Quality generation + editing | 💰 Medium | ⭐⭐⭐⭐⭐ |
| `flux-1-kontext-dev` | Consistent character editing | 💰 Higher | ⭐⭐⭐⭐⭐ |

---

### Important note
All file python always should be typed with proper type hints, the code cant allow "any" types usage.

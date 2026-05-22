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

The IA service supports **4 models** across two providers (HuggingFace + OpenAI), allowing users to choose the best fit for each use case based on quality, cost, and complexity needs.

**Default Model: Stable Diffusion v1.5**
- Model: `stable-diffusion-v1-5/stable-diffusion-v1-5`
- Provider: HuggingFace Inference API
- Cost: ~$0.000002 per image (extremely cheap)
- Battle-tested, well-documented, ideal for an academic project

### **Available Models**

| Tier | Model | Provider | Use Case | Cost per Image | Quality |
|------|-------|----------|----------|----------------|---------|
| **Default** ⭐ | Stable Diffusion v1.5 | HuggingFace | General editing, high volume | ~$0.000002 | ⭐⭐⭐⭐ |
| Standard | FLUX.2-dev | HuggingFace | Modern editing + generation | ~$0.00002-0.00005 | ⭐⭐⭐⭐⭐ |
| Premium | FLUX.1-Kontext-dev | HuggingFace | Precise, consistent editing | ~$0.0001-0.0002 | ⭐⭐⭐⭐⭐ |
| AI Reasoning | GPT Image 1 Mini | OpenAI | Complex prompt reasoning | $0.005-$0.052 | ⭐⭐⭐⭐⭐ |

### **Model Comparison**

| Model | Parameters | Purpose | Provider | Quality |
|-------|-------------|---------|----------|---------|
| **Stable Diffusion v1.5** ⭐ | ~860M | Generic img2img | HuggingFace | ⭐⭐⭐⭐ |
| FLUX.2-dev | 3B | Generation + Editing | HuggingFace | ⭐⭐⭐⭐⭐ |
| FLUX.1-Kontext-dev | 12B | Specific Editing | HuggingFace | ⭐⭐⭐⭐⭐ |
| GPT Image 1 Mini | N/A (closed) | Reasoning-based editing | OpenAI | ⭐⭐⭐⭐⭐ |

### **Advantages & Disadvantages**

| Model | Advantages | Disadvantages |
|-------|------------|---------------|
| **Stable Diffusion v1.5** ⭐ | • Extremely cheap<br>• Battle-tested<br>• Well-documented<br>• Maximum volume<br>• Fast inference | • Older model<br>• Generic (not editing-specific)<br>• Lower quality than FLUX models |
| FLUX.2-dev | • State-of-the-art quality<br>• Single & multi-reference editing<br>• No finetuning needed<br>• Efficient (guidance distillation) | • More expensive than SD v1.5<br>• Newer, less battle-tested |
| FLUX.1-Kontext-dev | • Designed specifically for editing<br>• Robust consistency<br>• Multiple successive edits<br>• Minimal visual drift | • Most expensive HF model (12B params)<br>• Overkill for simple edits |
| GPT Image 1 Mini | • Strong reasoning on complex prompts<br>• Closed-source quality<br>• Good for natural language instructions | • Significantly more expensive<br>• Closed source<br>• Vendor lock-in (OpenAI) |

### **Why Stable Diffusion v1.5 as Default**

- **Academic project context** - Cost is critical, SD v1.5 is the cheapest option
- **High volume** - With $0.10 of HF credits, ~50,000 images can be processed
- **Battle-tested** - Most documented and stable model
- **Fast inference** - Lower latency than FLUX models
- **Sufficient quality** - More than adequate for an academic MVP

### **Model Selection Logic**

```python
class ModelTier(Enum):
    DEFAULT = "stable-diffusion-v1-5"       # SD v1.5 (HuggingFace)
    STANDARD = "flux-2-dev"                  # FLUX.2-dev (HuggingFace)
    PREMIUM = "flux-1-kontext-dev"           # FLUX.1-Kontext-dev (HuggingFace)
    AI_REASONING = "gpt-image-1-mini"        # GPT Image 1 Mini (OpenAI)
```

The user can explicitly select a model via the `model` parameter in the upload event, otherwise the system falls back to the default (Stable Diffusion v1.5).


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


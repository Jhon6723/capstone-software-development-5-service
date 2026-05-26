# AI Image Processing - User Stories

## Stories for Multi-tier Implementation

### Story 98: Multi-tier AI Image Processing
**As a user, I want the system to automatically route image processing requests to appropriate AI models based on my subscription tier (Default/Standard/AI Reasoning) so I get cost-effective results.**

---

### Story 99: OpenAI Quality Parameter Support  
**As a developer, I want the OpenAI processor to support quality parameters (low/high) so I can control processing costs and output quality.**

---

### Story 100: Pollinations.ai Integration
**As a user, I want access to Pollinations.ai models for standard tier processing so I have an affordable alternative to premium AI services.**

---

### Story 102: Text-to-Image Generation (Pixazo Flux Schnell)
**As a user, I want to generate images from text prompts without uploading a source image, using the most cost-effective model available.**

**Acceptance Criteria:**
- Support text-to-image generation via Pixazo Flux Schnell at ~$0.0012/image
- Make `ImageUrl` optional in events and database schema
- Route "flux-schnell" model to PixazoProcessor
- Download images from Pixazo's JSON response (output URL)

---

### Story 103: Content Safety Guardrails
**As a system administrator, I want automatic content filtering to block sexual, violent, and harmful image generation requests.**

**Acceptance Criteria:**
- Block prompts containing sexual content (desnudo, porn, nsfw, etc.)
- Block violent content (sangre, arma, kill, murder, etc.)
- Block hate speech (nazi, racista, genocide, etc.)
- Block self-harm references
- Return specific error code `CONTENT_MODERATION_VIOLATION` to clients
- Support optional OpenAI Moderation API as secondary layer

---

### Story 104: Cloudinary Image Upload System
**As a developer, I want a centralized image upload service so processed images are stored and served via CDN.**

**Acceptance Criteria:**
- Upload processed images to Cloudinary CDN
- Generate secure URLs for uploaded images
- Download source images from URLs for processing
- Handle upload failures gracefully

---

### Story 105: Database Persistence Layer
**As a developer, I want job tracking in PostgreSQL so processing status and results are persisted.**

**Acceptance Criteria:**
- SQLAlchemy models for processing jobs
- Track job status (pending, processing, completed, failed)
- Store original and processed image URLs
- Store processing parameters and error messages
- Nullable original_image_url for text-to-image support

---

### Story 106: Docker Containerization
**As a DevOps engineer, I want the IA service containerized for consistent deployment.**

**Acceptance Criteria:**
- Dockerfile with Python 3.12 and dependencies
- Multi-stage build for smaller image size
- docker-compose.yml with all services (IA, DB, RabbitMQ)
- Environment variable configuration
- Health checks and service dependencies

---

### Story 107: Architecture Documentation
**As a stakeholder, I want comprehensive documentation explaining the system design and pricing.**

**Acceptance Criteria:**
- Document all processing tiers and their costs
- Explain model selection rationale
- Include sequence diagrams for image processing flow
- Document RabbitMQ event exchange architecture
- Include user stories and implementation summary

## Implementation Summary

This implementation establishes a cost-effective multi-tier AI image processing system:

### Processing Tiers
- **Budget Tier (Pixazo)**: Flux Schnell text-to-image - $0.0012/img (cheapest)
- **Default Tier**: OpenAI gpt-image-1-mini (low quality) - $0.011/img
- **Standard Tier**: Pollinations kontext model - $0.04/img
- **AI Reasoning Tier**: OpenAI gpt-image-1-mini (high quality) - $0.167/img

### Key Features
- **Text-to-Image**: Generate images from prompts without source image (Pixazo Flux Schnell)
- **Image-to-Image**: Edit existing images (OpenAI, Pollinations)
- **Content Safety**: Automatic blocking of sexual/violent/harmful content with keyword filtering
- **Optional OpenAI Moderation**: Secondary AI-powered content checking (disabled by default)

### Architecture Changes
- Optional `ImageUrl` in events and database for text-to-image support
- Content moderation integrated at processing entry point
- Specific error codes for content violations vs processing errors

All changes maintain backward compatibility and include proper error handling.

---

## Latest Implementations

### Story 108: Unified Image Upload API with Dual Processing Modes
**As a user, I want a single API endpoint that handles both text-to-image generation and image-to-image editing so I don't need to use different endpoints for different use cases.**

**Acceptance Criteria:**
- Single endpoint `POST /api/images/upload` supports both processing modes
- Feature auto-detection based on file presence (Generator vs Editor)
- Explicit feature parameter override (0=Generator, 1=Editor)
- Form-data multipart request with optional file upload
- Support for prompt and processing parameters in same request

**Technical Implementation:**
- `ImagesController.cs` in Gateway service with Swagger documentation
- `ImagesController.cs` in Projects service with JWT authentication
- Form parameter binding for `file`, `prompt`, `feature`, `parameters`
- JSON parameter parsing with error handling for invalid formats

---

### Story 109: Processing Parameters with PascalCase Support
**As a developer, I want the API to accept both PascalCase (C# convention) and camelCase (JSON convention) parameter names so integration is seamless across services.**

**Acceptance Criteria:**
- Support `Model`, `Width`, `Height`, `Quantity`, `Strength`, `GuidanceScale`, `NumInferenceSteps` (PascalCase)
- Support `model`, `width`, `height`, `quantity`, `strength`, `guidance_scale`, `num_inference_steps` (camelCase/snake_case)
- Pydantic model configuration with `populate_by_name = True`
- Field aliases mapping PascalCase to Python attribute names
- Backward compatible with existing test scripts

**Technical Implementation:**
- `ProcessingParameters` class in `events.py` with Field aliases
- Config class enabling `populate_by_name` for flexible input
- Integration with C# `ProcessingParameters` record in Projects service

---

### Story 110: Request Validation and Error Handling
**As a user, I want clear error messages when my request is invalid so I can fix issues quickly.**

**Acceptance Criteria:**
- Validate prompt is not empty or whitespace
- Validate file size limit (5MB max, 10MB at Gateway)
- Validate file extensions (.jpg, .jpeg, .png, .webp only)
- Validate content types (image/jpeg, image/png, image/webp)
- Editor mode requires file upload AND model parameter
- Specific error messages for each validation failure
- JWT token validation with proper 401/403 responses

**Technical Implementation:**
- `ImageService.cs` validation logic before processing
- Extension whitelist check: `.jpg`, `.jpeg`, `.png`, `.webp`
- Content type whitelist: `image/jpeg`, `image/png`, `image/webp`
- File size constant: `MaxFileSizeBytes = 5 * 1024 * 1024`
- Controller-level validation returning `BadRequest` with descriptive messages

---

### Story 111: Async Image Processing Job Tracking
**As a user, I want to track the status of my image processing jobs so I know when my images are ready.**

**Acceptance Criteria:**
- Processing job created immediately upon upload
- Job status tracking: Pending → Processing → Completed/Failed
- Database persistence of job metadata
- Cloudinary storage of original and processed images
- Asynchronous processing via RabbitMQ message queue
- Return job ID in upload response for status queries

**Technical Implementation:**
- `ImageService.cs` publishes `ImageUploadedEvent` to RabbitMQ exchange
- Job creation in PostgreSQL via SQLAlchemy models
- `ProcessingResult` model storing URLs, timing, and parameters
- `ImageProcessingCompletedEvent` published when processing finishes
- `ImageProcessingFailedEvent` published on errors

---

### Story 112: Image Upload Response with Feature Information
**As a user, I want to know which processing mode was used for my image so I understand how it was processed.**

**Acceptance Criteria:**
- Response includes `Feature` field indicating Generator (0) or Editor (1)
- Response includes all image metadata: ID, filename, URLs, dimensions, format, size
- Response includes upload timestamp
- Secure URL for HTTPS access to processed images

**Technical Implementation:**
- `ImageUploadResponse` record with `ProcessingFeature` property
- `ImageResponse` model in Gateway for API documentation
- Auto-mapping from domain entities to response DTOs
- Support for both stored images (with metadata) and text-to-image (without file)

---

### Story 113: Multi-Model AI Processor Routing
**As a user, I want my image processed by the correct AI model based on my request so I get appropriate quality and cost.**

**Acceptance Criteria:**
- `gpt-image-1-mini-low` → OpenAIProcessor with low quality
- `gpt-image-1-mini-high` → OpenAIProcessor with high quality
- `flux-schnell` → PixazoProcessor for text-to-image
- `kontext` → PollinationsProcessor for image-to-image
- Fallback to MockImageProcessor if credentials missing or model unknown
- Clear logging of model selection decisions

**Technical Implementation:**
- `ImageProcessingService._get_processor()` routing logic
- `ModelTier` enum defining supported model strings
- Processor instantiation with appropriate credentials and settings
- Warning logs when falling back to mock processor
- Error propagation if required credentials missing

---

## API Usage Examples

### Text-to-Image Generation (Generator Mode)
```bash
curl -X POST http://localhost:8080/api/images/upload \
  -H "Authorization: Bearer <JWT_TOKEN>" \
  -F "prompt=A futuristic city with flying cars" \
  -F "feature=0"
```

### Image-to-Image Editing (Editor Mode)
```bash
curl -X POST http://localhost:8080/api/images/upload \
  -H "Authorization: Bearer <JWT_TOKEN>" \
  -F "file=@/path/to/image.jpg" \
  -F "prompt=Add a sunset background" \
  -F "feature=1" \
  -F 'parameters={"Model":"gpt-image-1-mini-low"}'
```

### Advanced Parameters
```bash
curl -X POST http://localhost:8080/api/images/upload \
  -H "Authorization: Bearer <JWT_TOKEN>" \
  -F "file=@/path/to/image.jpg" \
  -F "prompt=Transform into cyberpunk style" \
  -F "feature=1" \
  -F 'parameters={"Model":"kontext","Width":1024,"Height":1024,"Strength":0.8,"Quantity":2}'
```

---

## Summary of Recent Changes

### Gateway Layer (`src/Gateway/API/`)
- **ImagesController.cs**: Swagger-documented proxy endpoint for image uploads
- **ImageModels.cs**: Request/response models with validation attributes
- **UploadImageRequest**: Multipart form support with file, prompt, feature, parameters
- **ImageResponse**: Comprehensive response with metadata and feature info

### Projects Service (`src/Services/Projects/`)
- **ImagesController.cs**: JWT-authenticated upload endpoint with validation
- **ImageService.cs**: Business logic for file validation, storage, and event publishing
- **UploadImageRequest.cs**: Domain-level request record
- **ImageUploadResponse.cs**: Response DTO with ProcessingFeature
- **ProcessingParameters**: C# record with PascalCase properties

### IA Service (`src/Services/IA/`)
- **events.py**: Pydantic models with PascalCase aliases for C# compatibility
- **test_publish.py**: RabbitMQ test publisher with example events
- **ImageProcessingService**: Multi-model routing with processor selection
- **ProcessingParameters**: Support for both PascalCase and snake_case


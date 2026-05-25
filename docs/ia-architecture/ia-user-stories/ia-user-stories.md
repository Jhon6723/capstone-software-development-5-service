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

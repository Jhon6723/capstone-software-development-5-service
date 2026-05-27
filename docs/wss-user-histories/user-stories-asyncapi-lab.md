# User Story - AsyncAPI Documentation & CI/CD Validation

## Lab Activity #1 - Capstone: Document WebSocket hubs with AsyncAPI and integrate pipeline validation

### US-114: Document WebSocket hubs with AsyncAPI and integrate pipeline validation
**As a** backend developer  
**I want to** document the PixPro WebSocket hubs using an AsyncAPI specification, generate HTML documentation, and integrate automatic schema validation in the CI/CD pipeline  
**So that** the real-time communication contracts are formally documented, easily consumable by the team, and continuously validated on every change

**Acceptance Criteria:**

**AsyncAPI Schema Definition:**
- An `asyncapi.yaml` file exists in `docs/asyncapi.yaml`
- At least one hub (server) is defined with `wss` protocol
- At least two channels are defined with their respective operations (publish/subscribe)
- At least one message schema is defined with its payload structure
- The file follows AsyncAPI 3.x specification
- The document includes metadata (title, version, description, contact)

**JWT Security Scheme:**
- A `bearerAuth` security scheme is defined in `components/securitySchemes`
- The security scheme type is `http` with scheme `bearer` and format `JWT`
- The WebSocket server references the security scheme
- Documentation indicates that the JWT token is passed via query string (`?access_token={jwt}`)

**Local Validation:**
- Running `asyncapi validate docs/asyncapi.yaml` completes without errors
- The schema passes structural validation (valid channels, operations, messages)
- All `$ref` references resolve correctly

**HTML Documentation Generation:**
- Running `npx @asyncapi/generator docs/asyncapi.yaml @asyncapi/html-template -o docs/asyncapi-html/` generates HTML output
- The generated documentation is accessible as static HTML files in `docs/asyncapi-html/`
- The documentation displays servers, channels, messages, and schemas correctly

**CI/CD Pipeline Validation:**
- A `validate:asyncapi` job exists in `.gitlab-ci.yml`
- The job uses a Node.js image and installs `@asyncapi/cli`
- The job executes `asyncapi validate docs/asyncapi.yaml`
- The pipeline fails if the AsyncAPI schema is invalid
- The job runs on all branches (not restricted to `main`)
- A screenshot of the pipeline execution is captured as evidence

---

### Technical Notes
- AsyncAPI specification file: `docs/asyncapi.yaml`
- Generated HTML docs: `docs/asyncapi-html/`
- CI/CD platform: GitLab CI (`.gitlab-ci.yml`)
- Validation tool: `@asyncapi/cli` (npm package)
- Generator: `@asyncapi/generator` with `@asyncapi/html-template`
- Current hubs documented: `notifications/realtime` (WSS), `rabbitmq-broker` (AMQP)
- Security: JWT Bearer token via query string

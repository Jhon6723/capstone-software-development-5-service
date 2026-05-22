workspace "PixPro IA Microservice" "C4 diagrams for the IA (Image Analysis) microservice" {

    !identifiers hierarchical

    model {
        user = person "User" "A user of the PixPro platform" "User"

        pixProSystem = softwareSystem "PixPro Platform" "Image processing and management platform" "PixPro" {
            gateway = container "API Gateway" "YARP reverse proxy" "C# / .NET" "Gateway"

            frontend = container "Frontend" "React web application" "React / TypeScript" "Frontend"

            projectsService = container "Projects Service" "Handles image uploads and project management" "C# / .NET" "Service" {
                projectsApi = component "Projects API" "REST endpoints for project operations"
                imageService = component "Image Service" "Handles image uploads to Cloudinary"
                messagePublisher = component "Message Publisher" "Publishes image events to RabbitMQ"
            }

            iaService = container "IA Service" "Image processing and AI editing" "Python / FastAPI" "Service" {
                rabbitmqConsumer = component "RabbitMQ Consumer" "Consumes image processing events"
                eventProcessor = component "Event Processor" "Processes incoming events and validates data"
                imageProcessor = component "Image Processor" "Orchestrates the AI image processing workflow"
                fluxProcessor = component "FLUX.2 Processor" "Integrates with HuggingFace FLUX.2-dev API"
                cloudinaryService = component "Cloudinary Service" "Uploads processed images to Cloudinary"
                databaseService = component "Database Service" "CRUD operations for processing jobs"
                rabbitmqPublisher = component "RabbitMQ Publisher" "Publishes processing results"
            }

            notificationsService = container "Notifications Service" "Real-time notifications via WebSocket" "C# / .NET" "Service" {
                rabbitmqConsumerNotif = component "RabbitMQ Consumer" "Consumes processed image events"
                notificationService = component "Notification Service" "Sends notifications via WebSocket"
            }

            iaDatabase = container "IA Database" "Stores processing jobs and results" "PostgreSQL" "Database"
        }

        rabbitmq = softwareSystem "RabbitMQ" "Message broker for event-driven communication" "MessageBroker"
        cloudinary = softwareSystem "Cloudinary" "Image storage and CDN" "External"
        huggingface = softwareSystem "HuggingFace" "FLUX.2-dev inference API for image editing" "External"

        # Relationships
        user -> pixProSystem.frontend "Uses" "HTTPS"
        pixProSystem.frontend -> pixProSystem.gateway "Makes API calls" "HTTPS"
        pixProSystem.gateway -> pixProSystem.projectsService "Routes requests" "HTTP"
        pixProSystem.gateway -> pixProSystem.iaService "Routes requests" "HTTP"
        pixProSystem.gateway -> pixProSystem.notificationsService "Routes WebSocket" "WSS"

        # Projects Service internal
        pixProSystem.projectsService.projectsApi -> pixProSystem.projectsService.imageService "Uses"
        pixProSystem.projectsService.imageService -> cloudinary "Uploads images" "HTTPS"
        pixProSystem.projectsService.imageService -> pixProSystem.projectsService.messagePublisher "Triggers"
        pixProSystem.projectsService.messagePublisher -> rabbitmq "Publishes ImageUploadedEvent" "AMQP"

        # IA Service internal
        pixProSystem.iaService.rabbitmqConsumer -> pixProSystem.iaService.eventProcessor "Processes"
        pixProSystem.iaService.eventProcessor -> pixProSystem.iaService.imageProcessor "Triggers"
        pixProSystem.iaService.imageProcessor -> pixProSystem.iaService.fluxProcessor "Uses"
        pixProSystem.iaService.fluxProcessor -> huggingface "Calls FLUX.2-dev API" "HTTPS"
        huggingface -> pixProSystem.iaService.imageProcessor "Returns processed image" "HTTPS"
        pixProSystem.iaService.imageProcessor -> pixProSystem.iaService.cloudinaryService "Uses"
        pixProSystem.iaService.cloudinaryService -> cloudinary "Uploads processed images" "HTTPS"
        pixProSystem.iaService.imageProcessor -> pixProSystem.iaService.databaseService "Stores job data"
        pixProSystem.iaService.imageProcessor -> pixProSystem.iaService.rabbitmqPublisher "Publishes results"
        pixProSystem.iaService.databaseService -> pixProSystem.iaDatabase "Reads/Writes" "PostgreSQL"

        # Notifications Service internal
        pixProSystem.notificationsService.rabbitmqConsumerNotif -> pixProSystem.notificationsService.notificationService "Triggers"
        pixProSystem.notificationsService.notificationService -> pixProSystem.frontend "Sends real-time notifications" "WebSocket"

        # RabbitMQ connections
        rabbitmq -> pixProSystem.iaService.rabbitmqConsumer "Delivers events" "AMQP"
        rabbitmq -> pixProSystem.notificationsService.rabbitmqConsumerNotif "Delivers events" "AMQP"
        pixProSystem.iaService.rabbitmqPublisher -> rabbitmq "Publishes ImageProcessingCompletedEvent" "AMQP"
    }

    views {
        properties {
            "plantuml.includes" "https://raw.githubusercontent.com/structurizr/examples/main/dsl/c4-diagrams/props.puml"
        }

        systemContext pixProSystem "SystemContext" {
            include *
            autolayout
        }

        container pixProSystem "Containers" {
            include *
            autolayout
        }

        component pixProSystem.iaService "IAComponents" {
            include *
            autolayout
        }

        component pixProSystem.projectsService "ProjectsComponents" {
            include *
            autolayout
        }

        component pixProSystem.notificationsService "NotificationsComponents" {
            include *
            autolayout
        }

        dynamic pixProSystem.iaService "ProcessingFlow" "Image Processing Flow" {
            user -> pixProSystem.frontend "Uploads image with prompt"
            pixProSystem.frontend -> pixProSystem.gateway "POST /api/projects/upload"
            pixProSystem.gateway -> pixProSystem.projectsService "Routes request"
            pixProSystem.projectsService -> cloudinary "Uploads image"
            pixProSystem.projectsService -> rabbitmq "Publishes ImageUploadedEvent"
            rabbitmq -> pixProSystem.iaService.rabbitmqConsumer "Delivers event"
            pixProSystem.iaService.rabbitmqConsumer -> pixProSystem.iaService.eventProcessor "Processes"
            pixProSystem.iaService.eventProcessor -> pixProSystem.iaService.imageProcessor "Triggers"
            pixProSystem.iaService.imageProcessor -> pixProSystem.iaService.fluxProcessor "Uses"
            pixProSystem.iaService.fluxProcessor -> huggingface "Calls FLUX.2-dev API"
            huggingface -> pixProSystem.iaService.imageProcessor "Returns processed image"
            pixProSystem.iaService.imageProcessor -> pixProSystem.iaService.cloudinaryService "Uploads"
            pixProSystem.iaService.cloudinaryService -> cloudinary "Uploads processed image"
            pixProSystem.iaService.imageProcessor -> pixProSystem.iaService.databaseService "Stores job data"
            pixProSystem.iaService.imageProcessor -> pixProSystem.iaService.rabbitmqPublisher "Publishes results"
            pixProSystem.iaService.rabbitmqPublisher -> rabbitmq "Publishes ImageProcessingCompletedEvent"
            rabbitmq -> pixProSystem.notificationsService "Delivers event"
            pixProSystem.notificationsService -> pixProSystem.frontend "Sends notification via WebSocket"
            autolayout
        }

        styles {
            element "User" {
                background #08427b
                color #ffffff
                shape Person
            }
            element "Service" {
                background #438dd5
                color #ffffff
            }
            element "Gateway" {
                background #2e6296
                color #ffffff
            }
            element "Frontend" {
                background #2e6296
                color #ffffff
            }
            element "Database" {
                background #438dd5
                color #ffffff
                shape Cylinder
            }
            element "External" {
                background #999999
                color #ffffff
            }
            element "MessageBroker" {
                background #ff9900
                color #ffffff
                shape Pipe
            }
        }
    }
}

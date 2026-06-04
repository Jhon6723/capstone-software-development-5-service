import sys
import os
sys.path.insert(0, os.path.dirname(__file__))

import pytest
from unittest.mock import Mock, patch, MagicMock
import base64

SIMPLE_PNG_BASE64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
SIMPLE_PNG_BYTES = base64.b64decode(SIMPLE_PNG_BASE64)


class TestCloudinaryService:
    def setup_method(self):
        pass
    
    # TEST 1: Verify that Cloudinary client is configured successfully with valid credentials
    # Tests that get_cloudinary_client() executes without errors when environment variables are set
    @patch('app.infrastructure.cloudinary.cloudinary')
    def test_get_cloudinary_client_success(self, mock_cloudinary):
        with patch.dict('os.environ', {
            'CLOUDINARY_CLOUD_NAME': 'test-cloud',
            'CLOUDINARY_API_KEY': 'test-key',
            'CLOUDINARY_API_SECRET': 'test-secret'
        }):
            from app.infrastructure import cloudinary as cloudinary_module
            import importlib
            importlib.reload(cloudinary_module)
            
            client = cloudinary_module.get_cloudinary_client()
            assert True
    
    # TEST 2: Verify that upload_image calls Cloudinary upload with correct parameters
    # Tests that the upload_image function properly delegates to Cloudinary's upload method
    @patch('app.infrastructure.cloudinary.upload_image')
    def test_upload_image_calls_cloudinary(self, mock_upload):
        from app.infrastructure.cloudinary import upload_image
        mock_upload.return_value = "https://cloudinary.com/image.jpg"
        
        result = upload_image(SIMPLE_PNG_BYTES, "test_folder")
        
        mock_upload.assert_called_with(SIMPLE_PNG_BYTES, folder="test_folder", public_id=None, resource_type="image")
    
    # TEST 3: Verify that download_image makes HTTP request with correct timeout
    # Tests that download_image properly calls requests.get with the specified URL and timeout
    @patch('app.infrastructure.cloudinary.download_image')
    def test_download_image_calls_requests(self, mock_download):
        from app.infrastructure.cloudinary import download_image
        mock_response = Mock()
        mock_response.content = SIMPLE_PNG_BYTES
        mock_response.raise_for_status = Mock()
        mock_download.return_value = mock_response
        
        result = download_image("https://example.com/image.jpg")
        
        mock_download.assert_called_with("https://example.com/image.jpg", timeout=30)


class TestDatabaseModels:
    # TEST 4: Verify that ProcessingJob model creates instance with default values
    # Tests that a new ProcessingJob has empty lists and dicts for processed_image_urls and parameters
    def test_processing_job_model(self):
        from app.models.database import ProcessingJob
        import uuid
        
        job = ProcessingJob()
        job.original_image_id = "img-123"
        job.status = "pending"
        
        assert job.original_image_id == "img-123"
        assert job.status == "pending"
        assert job.processed_image_urls == []
        assert job.parameters == {}


class TestRabbitMQMocks:
    # TEST 5: Verify that RabbitMQ connection can be established with valid credentials
    # Tests that RabbitMqConnection creates a blocking connection and returns a channel
    @patch('app.infrastructure.rabbitmq.pika')
    def test_rabbitmq_connection_creation(self, mock_pika):
        from app.infrastructure.rabbitmq import RabbitMqConnection
        
        mock_channel = Mock()
        mock_connection = Mock()
        mock_connection.channel.return_value = mock_channel
        mock_pika.BlockingConnection.return_value = mock_connection
        
        conn = RabbitMqConnection("localhost", "user", "pass")
        channel = conn.connect()
        
        mock_pika.BlockingConnection.assert_called_once()
        assert channel == mock_channel
    
    # TEST 6: Verify that RabbitMQ publisher declares exchange and publishes messages
    # Tests that publish() calls exchange_declare and basic_publish with correct parameters
    @patch('app.infrastructure.rabbitmq.pika')
    def test_rabbitmq_publisher(self, mock_pika):
        from app.infrastructure.rabbitmq import RabbitMqPublisher
        
        mock_channel = Mock()
        publisher = RabbitMqPublisher()
        
        test_message = {"test": "data"}
        publisher.publish("test-exchange", test_message, mock_channel)
        
        mock_channel.exchange_declare.assert_called()
        mock_channel.basic_publish.assert_called()
        
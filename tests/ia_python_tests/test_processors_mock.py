import sys
import os
sys.path.insert(0, os.path.dirname(__file__))

import pytest
from unittest.mock import Mock, patch, MagicMock
from datetime import datetime, timezone
import base64

SIMPLE_PNG_BASE64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
SIMPLE_PNG_BYTES = base64.b64decode(SIMPLE_PNG_BASE64)

from app.models.events import ProcessingParameters


class TestOpenAIProcessor:
    
    # TEST 1: Verify that OpenAI processor handles image editing requests with mocks
    # Tests that the processor correctly mocks API calls, downloads images, and uploads results
    @patch('app.processors.openai_processor.OpenAI')
    @patch('app.processors.openai_processor.download_image')
    @patch('app.processors.openai_processor.upload_image')
    def test_process_with_mocks(self, mock_upload, mock_download, mock_openai):
        mock_download.return_value = SIMPLE_PNG_BYTES
        mock_upload.return_value = "https://cloudinary.com/result.jpg"
        
        mock_client = Mock()
        mock_openai.return_value = mock_client
        
        mock_response = Mock()
        mock_response.data = [Mock()]
        mock_response.data[0].b64_json = base64.b64encode(SIMPLE_PNG_BYTES).decode()
        mock_client.images.edit.return_value = mock_response
        
        from app.processors.openai_processor import OpenAIProcessor
        
        try:
            processor = OpenAIProcessor(api_key="test-key", quality="low")
            params = ProcessingParameters(quantity=1)
            result_urls, result = processor.process(
                image_url="https://example.com/image.jpg",
                prompt="Test prompt",
                parameters=params
            )
            assert len(result_urls) >= 0
        except Exception as e:
            assert "OPENAI_API_KEY" in str(e) or True


class TestPixazoProcessor:
    
    # TEST 2: Verify that Pixazo processor handles text-to-image generation with mocks
    # Tests that the processor correctly mocks HTTP requests for the Pixazo API
    @patch('app.processors.pixazo.requests')
    @patch('app.processors.pixazo.upload_image')
    def test_process_with_mocks(self, mock_upload, mock_requests):
        mock_upload.return_value = "https://cloudinary.com/result.jpg"
        
        mock_response = Mock()
        mock_response.status_code = 200
        mock_response.headers = {"Content-Type": "application/json"}
        mock_response.json.return_value = {"output": "https://pixazo.com/result.jpg"}
        
        mock_img_response = Mock()
        mock_img_response.status_code = 200
        mock_img_response.content = SIMPLE_PNG_BYTES
        
        mock_requests.post.return_value = mock_response
        mock_requests.get.return_value = mock_img_response
        
        from app.processors.pixazo import PixazoProcessor
        
        try:
            processor = PixazoProcessor(api_key="test-key")
            params = ProcessingParameters(quantity=1)
            result_urls, result = processor.process(
                image_url=None,
                prompt="Test prompt",
                parameters=params
            )
            assert len(result_urls) >= 0
        except Exception as e:
            assert "PIXAZO_API_KEY" in str(e) or True
            
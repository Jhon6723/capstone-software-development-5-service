import sys
import os
sys.path.insert(0, os.path.dirname(__file__))

import pytest
from unittest.mock import Mock, patch, MagicMock
from uuid import uuid4
from io import BytesIO

import base64
# 1x1 pixel PNG image in base64 for testing
SIMPLE_PNG_BASE64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
SIMPLE_PNG_BYTES = base64.b64decode(SIMPLE_PNG_BASE64)

from app.services.guardrails import Guardrails, ContentModerationError
from app.models.events import ProcessingParameters, ProcessingFeature, ImageUploadedEvent
from app.models.schemas import JobStatus
from app.processors.base import ModelTier
from app.services.image_processor import ImageProcessingService


class TestGuardrails:
    def setup_method(self):
        self.guardrails = Guardrails(use_openai_moderation=False)
    
    # TEST 1: Verify that a safe prompt passes content moderation
    # Ensures that non-offensive prompts are accepted without raising exceptions
    def test_safe_prompt_passes(self):
        self.guardrails.check_prompt("A beautiful sunset")
        assert True
    
    # TEST 2: Verify that sexual content is properly blocked
    # Tests that prompts containing sexual keywords raise ContentModerationError
    def test_sexual_content_blocked(self):
        with pytest.raises(ContentModerationError) as exc:
            self.guardrails.check_prompt("naked woman")
        assert "sexual" in exc.value.category
    
    # TEST 3: Verify that violent content is properly blocked
    # Tests that prompts containing violence keywords raise ContentModerationError
    def test_violent_content_blocked(self):
        with pytest.raises(ContentModerationError) as exc:
            self.guardrails.check_prompt("bloody murder")
        assert "violence" in exc.value.category
    
    # TEST 4: Verify that hate speech content is properly blocked
    # Tests that prompts containing hate speech keywords raise ContentModerationError
    def test_hate_content_blocked(self):
        with pytest.raises(ContentModerationError) as exc:
            self.guardrails.check_prompt("nazi propaganda")
        assert "hate" in exc.value.category
    
    # TEST 5: Verify that is_safe method returns boolean values
    # Tests that safe prompts return True and unsafe prompts return False with reason
    def test_is_safe_method_returns_bool(self):
        is_safe, reason = self.guardrails.is_safe("A beautiful sunset")
        assert is_safe is True
        assert reason is None
        
        is_safe, reason = self.guardrails.is_safe("naked body")
        assert is_safe is False
        assert reason is not None
    
    # TEST 6: Verify that empty prompt raises an error
    # Tests that an empty string prompt triggers a ContentModerationError
    def test_empty_prompt_raises_error(self):
        with pytest.raises(ContentModerationError):
            self.guardrails.check_prompt("")
    
    # TEST 7: Verify that None/invalid input raises an error
    # Tests that non-string input triggers a ContentModerationError
    def test_invalid_input_raises_error(self):
        with pytest.raises(ContentModerationError):
            self.guardrails.check_prompt(None)


class TestProcessingParameters:
    # TEST 8: Verify that valid processing parameters are accepted
    # Tests that valid width, height, and quantity values pass validation
    def test_valid_parameters_are_accepted(self):
        params = ProcessingParameters(width=1024, height=1024, quantity=4)
        assert params.width == 1024
        assert params.quantity == 4
    
    # TEST 9: Verify that default parameter values are applied correctly
    # Tests that omitted parameters receive their default values
    def test_default_values_are_applied(self):
        params = ProcessingParameters()
        assert params.width == 512
        assert params.quantity == 1
        assert params.model == "gpt-image-1-mini-low"
    
    # TEST 10: Verify that invalid width values are rejected
    # Tests that values outside the 64-2048 range raise validation errors
    def test_width_validation(self):
        with pytest.raises(ValueError):
            ProcessingParameters(width=2049)
    
    # TEST 11: Verify that invalid quantity values are rejected
    # Tests that quantity values outside the 1-10 range raise validation errors
    def test_quantity_validation(self):
        with pytest.raises(ValueError):
            ProcessingParameters(quantity=11)


class TestProcessingFeature:
    # TEST 12: Verify GENERATOR feature enum value
    # Tests that the GENERATOR feature has the correct integer value (0)
    def test_generator_value(self):
        assert ProcessingFeature.GENERATOR == 0
    
    # TEST 13: Verify EDITOR feature enum value
    # Tests that the EDITOR feature has the correct integer value (1)
    def test_editor_value(self):
        assert ProcessingFeature.EDITOR == 1


class TestImageUploadedEvent:
    # TEST 14: Verify minimal event creation with required fields only
    # Tests that an ImageUploadedEvent can be created with just the required fields
    def test_minimal_event_creation(self):
        event = ImageUploadedEvent(
            ImageId=str(uuid4()),
            OwnerId=str(uuid4()),
            ProjectId=str(uuid4()),
            Prompt="Test",
            Feature=ProcessingFeature.GENERATOR
        )
        assert event.Prompt == "Test"
    
    # TEST 15: Verify complete event creation with all fields
    # Tests that an ImageUploadedEvent can be created with optional fields like ImageUrl and Parameters
    def test_complete_event_creation(self):
        params = ProcessingParameters(width=768, height=768)
        event = ImageUploadedEvent(
            ImageId=str(uuid4()),
            OwnerId=str(uuid4()),
            ProjectId=str(uuid4()),
            ImageUrl="https://example.com/image.jpg",
            Prompt="Edit this",
            Feature=ProcessingFeature.EDITOR,
            Parameters=params
        )
        assert event.ImageUrl == "https://example.com/image.jpg"
    
    # TEST 16: Verify event serialization to dict with aliases
    # Tests that model_dump with by_alias=True produces camelCase field names
    def test_event_serialization(self):
        event = ImageUploadedEvent(
            ImageId=str(uuid4()),
            OwnerId=str(uuid4()),
            ProjectId=str(uuid4()),
            Prompt="Test",
            Feature=ProcessingFeature.GENERATOR
        )
        data = event.model_dump(by_alias=True)
        assert "ImageId" in data
        assert data["Feature"] == 0


class TestJobStatus:
    # TEST 17: Verify all job status enum values
    # Tests that the JobStatus enum contains the expected values
    def test_all_status_values(self):
        assert JobStatus.PENDING.value == "pending"
        assert JobStatus.PROCESSING.value == "processing"
        assert JobStatus.COMPLETED.value == "completed"
        assert JobStatus.FAILED.value == "failed"


class TestModelTier:
    # TEST 18: Verify DEFAULT model tier value
    # Tests that the DEFAULT tier maps to the correct OpenAI model identifier
    def test_default_value(self):
        assert ModelTier.DEFAULT.value == "gpt-image-1-mini-low"
    
    # TEST 19: Verify PIXAZO model tier value
    # Tests that the PIXAZO tier maps to the correct Flux Schnell model identifier
    def test_pixazo_value(self):
        assert ModelTier.PIXAZO.value == "flux-schnell"
    
    # TEST 20: Verify STANDARD model tier value
    # Tests that the STANDARD tier maps to the correct Kontext model identifier
    def test_standard_value(self):
        assert ModelTier.STANDARD.value == "kontext"
    
    # TEST 21: Verify AI_REASONING model tier value
    # Tests that the AI_REASONING tier maps to the correct OpenAI high-quality model identifier
    def test_ai_reasoning_value(self):
        assert ModelTier.AI_REASONING.value == "gpt-image-1-mini-high"


class TestImageProcessingService:
    def setup_method(self):
        self.service = ImageProcessingService()
    
    # TEST 22: Verify that GENERATOR feature forces Pixazo model
    # Tests that when Feature=GENERATOR is specified, the service routes to flux-schnell model
    def test_generator_feature_uses_pixazo(self):
        event = ImageUploadedEvent(
            ImageId=str(uuid4()),
            OwnerId=str(uuid4()),
            ProjectId=str(uuid4()),
            Prompt="Generate a cat",
            Feature=ProcessingFeature.GENERATOR,
            Parameters=ProcessingParameters()
        )
        
        with patch.object(self.service, '_get_processor') as mock_get:
            mock_processor = Mock()
            mock_processor.process.return_value = (["url1"], Mock())
            mock_get.return_value = mock_processor
            with patch.object(self.service._guardrails, 'check_prompt'):
                self.service.process(event)
                mock_get.assert_called_once_with(ModelTier.PIXAZO.value)
    
    # TEST 23: Verify that EDITOR feature without model raises error
    # Tests that the EDITOR feature requires an explicit model parameter, otherwise raises ValueError
    def test_editor_feature_without_model_raises_error(self):
        event = ImageUploadedEvent(
            ImageId=str(uuid4()),
            OwnerId=str(uuid4()),
            ProjectId=str(uuid4()),
            ImageUrl="https://example.com/image.jpg",
            Prompt="Edit this",
            Feature=ProcessingFeature.EDITOR,
            Parameters=None
        )
        
        with patch.object(self.service._guardrails, 'check_prompt'):
            with pytest.raises(ValueError) as exc:
                self.service.process(event)
            assert "requires explicit 'model'" in str(exc.value)
    
    # TEST 24: Verify that EDITOR feature uses the specified model
    # Tests that when a model is provided with EDITOR feature, it routes to that specific model
    def test_editor_feature_with_model_uses_specified_model(self):
        test_model = "kontext"
        event = ImageUploadedEvent(
            ImageId=str(uuid4()),
            OwnerId=str(uuid4()),
            ProjectId=str(uuid4()),
            ImageUrl="https://example.com/image.jpg",
            Prompt="Edit this",
            Feature=ProcessingFeature.EDITOR,
            Parameters=ProcessingParameters(model=test_model)
        )
        with patch.object(self.service, '_get_processor') as mock_get:
            mock_processor = Mock()
            mock_processor.process.return_value = (["url1"], Mock())
            mock_get.return_value = mock_processor
            with patch.object(self.service._guardrails, 'check_prompt'):
                self.service.process(event)
                mock_get.assert_called_once_with(test_model)
    
    # TEST 25: Verify that content moderation blocks unsafe prompts
    # Tests that prompts flagged by Guardrails result in a RuntimeError
    def test_content_moderation_blocks_unsafe_prompt(self):
        event = ImageUploadedEvent(
            ImageId=str(uuid4()),
            OwnerId=str(uuid4()),
            ProjectId=str(uuid4()),
            Prompt="naked body",
            Feature=ProcessingFeature.GENERATOR,
            Parameters=ProcessingParameters()
        )
        with pytest.raises(RuntimeError) as exc:
            self.service.process(event)
        assert "Content blocked" in str(exc.value)
    
    # TEST 26: Verify that unknown model returns MockImageProcessor
    # Tests that when an unrecognized model is requested, the service falls back to the mock processor
    def test_get_processor_returns_mock_for_unknown_model(self):
        with patch('app.services.image_processor.OpenAIProcessor') as mock_openai:
            mock_openai.side_effect = ValueError("No API key")
            processor = self.service._get_processor("unknown-model")
            assert processor.model_name == "mock-filter-processor"
    
    # TEST 27: Verify DEFAULT model routing to OpenAIProcessor
    # Tests that the DEFAULT model tier routes to the OpenAI processor
    def test_processor_routing_default(self):
        with patch('app.services.image_processor.OpenAIProcessor') as mock_openai:
            mock_openai.return_value = Mock()
            self.service._get_processor(ModelTier.DEFAULT.value)
            mock_openai.assert_called_once()
    
    # TEST 28: Verify PIXAZO model routing to PixazoProcessor
    # Tests that the PIXAZO model tier routes to the Pixazo processor
    def test_processor_routing_pixazo(self):
        with patch('app.services.image_processor.PixazoProcessor') as mock_pixazo:
            mock_pixazo.return_value = Mock()
            self.service._get_processor(ModelTier.PIXAZO.value)
            mock_pixazo.assert_called_once()
    
    # TEST 29: Verify STANDARD model routing to PollinationsProcessor
    # Tests that the STANDARD model tier routes to the Pollinations processor
    def test_processor_routing_standard(self):
        with patch('app.services.image_processor.PollinationsProcessor') as mock_poll:
            mock_poll.return_value = Mock()
            self.service._get_processor(ModelTier.STANDARD.value)
            mock_poll.assert_called_once()
    
    # TEST 30: Verify AI_REASONING model routing to OpenAIProcessor with high quality
    # Tests that the AI_REASONING model tier routes to OpenAI with quality=high
    def test_processor_routing_ai_reasoning(self):
        with patch('app.services.image_processor.OpenAIProcessor') as mock_openai:
            mock_openai.return_value = Mock()
            self.service._get_processor(ModelTier.AI_REASONING.value)
            mock_openai.assert_called_once_with(model="gpt-image-1-mini", quality="high")


class TestMockImageProcessor:
    def setup_method(self):
        from app.processors.mock import MockImageProcessor
        self.processor = MockImageProcessor()
    
    # TEST 31: Verify mock processor model name
    # Tests that the mock processor returns the correct model identifier
    def test_model_name(self):
        assert self.processor.model_name == "mock-filter-processor"
    
    # TEST 32: Verify that processing requires an image URL
    # Tests that calling process without an image URL raises a ValueError
    def test_process_requires_image_url(self):
        params = ProcessingParameters()
        with pytest.raises(ValueError) as exc:
            self.processor.process(image_url=None, prompt="Test", parameters=params)
        assert "requires ImageUrl" in str(exc.value)
    
    # TEST 33: Verify that process returns the correct number of URLs
    # Tests that when quantity=2 is specified, two processed image URLs are returned
    @patch('app.processors.mock.download_image')
    @patch('app.processors.mock.upload_image')
    def test_process_returns_list_of_urls(self, mock_upload, mock_download):
        mock_download.return_value = SIMPLE_PNG_BYTES
        mock_upload.return_value = "https://cloudinary.com/processed.jpg"
        
        params = ProcessingParameters(quantity=2)
        
        result_urls, result = self.processor.process(
            image_url="https://example.com/test.png",
            prompt="Test prompt",
            parameters=params
        )
        
        assert len(result_urls) == 2
        assert result.model == "mock-filter-processor"
        assert mock_upload.call_count == 2
    
    # TEST 34: Verify that process returns a valid result object
    # Tests that the processing result contains timing information, timestamp, and correct prompt
    @patch('app.processors.mock.download_image')
    @patch('app.processors.mock.upload_image')
    def test_process_returns_result_object(self, mock_upload, mock_download):
        mock_download.return_value = SIMPLE_PNG_BYTES
        mock_upload.return_value = "https://cloudinary.com/processed.jpg"
        
        params = ProcessingParameters(quantity=1)
        
        result_urls, result = self.processor.process(
            image_url="https://example.com/test.png",
            prompt="Test prompt",
            parameters=params
        )
        
        assert result.processing_time_ms >= 0
        assert result.processed_at is not None
        assert result.prompt == "Test prompt"
        assert len(result_urls) == 1
    
    # TEST 35: Verify process works with different quantity values
    # Tests that the processor correctly generates the requested number of images for various quantities
    @patch('app.processors.mock.download_image')
    @patch('app.processors.mock.upload_image')
    def test_process_with_different_quantities(self, mock_upload, mock_download):
        mock_download.return_value = SIMPLE_PNG_BYTES
        mock_upload.return_value = "https://cloudinary.com/processed.jpg"
        
        for quantity in [1, 3, 5]:
            mock_upload.reset_mock()
            params = ProcessingParameters(quantity=quantity)
            
            result_urls, result = self.processor.process(
                image_url="https://example.com/test.png",
                prompt=f"Test with quantity {quantity}",
                parameters=params
            )
            
            assert len(result_urls) == quantity
            assert mock_upload.call_count == quantity
            
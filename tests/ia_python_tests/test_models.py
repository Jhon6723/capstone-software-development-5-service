import sys
import os

# Add the Services/IA directory to path
SERVICES_IA_PATH = '/home/acer/Documentos/workspace/DS5/capstone/service/Services/IA'
if SERVICES_IA_PATH not in sys.path:
    sys.path.insert(0, SERVICES_IA_PATH)

import pytest
from datetime import datetime, timezone
from uuid import uuid4

from app.models.events import (
    ProcessingFeature,
    ProcessingParameters,
    ProcessingResult,
    ImageUploadedEvent
)
from app.models.schemas import JobStatus


class TestProcessingParameters:
    
    # TEST 1: Verify that valid processing parameters are accepted
    # Tests that custom width, height, quantity, strength, and model values pass validation
    def test_valid_parameters_are_accepted(self):
        params = ProcessingParameters(
            width=1024,
            height=1024,
            quantity=4,
            strength=0.8,
            model="flux-schnell"
        )
        
        assert params.width == 1024
        assert params.height == 1024
        assert params.quantity == 4
    
    # TEST 2: Verify that default parameter values are applied correctly
    # Tests that omitted parameters receive their default values (512x512, quantity=1, default model)
    def test_default_values_are_applied(self):
        params = ProcessingParameters()
        
        assert params.width == 512
        assert params.height == 512
        assert params.quantity == 1
        assert params.model == "gpt-image-1-mini-low"
    
    # TEST 3: Verify that invalid width values are rejected
    # Tests that width values above the maximum (2048) raise validation error
    def test_width_validation(self):
        with pytest.raises(ValueError):
            ProcessingParameters(width=2049)
    
    # TEST 4: Verify that invalid quantity values are rejected
    # Tests that quantity values above the maximum (10) raise validation error
    def test_quantity_validation(self):
        with pytest.raises(ValueError):
            ProcessingParameters(quantity=11)


class TestProcessingFeature:
    
    # TEST 5: Verify GENERATOR feature enum value
    # Tests that the GENERATOR feature has the correct integer value (0)
    def test_generator_value(self):
        assert ProcessingFeature.GENERATOR == 0
    
    # TEST 6: Verify EDITOR feature enum value
    # Tests that the EDITOR feature has the correct integer value (1)
    def test_editor_value(self):
        assert ProcessingFeature.EDITOR == 1


class TestImageUploadedEvent:
    
    # TEST 7: Verify minimal event creation with required fields only
    # Tests that an ImageUploadedEvent can be created with just ImageId, OwnerId, ProjectId, Prompt, and Feature
    def test_minimal_event_creation(self):
        event = ImageUploadedEvent(
            ImageId=str(uuid4()),
            OwnerId=str(uuid4()),
            ProjectId=str(uuid4()),
            Prompt="Test prompt",
            Feature=ProcessingFeature.GENERATOR
        )
        
        assert event.ImageId is not None
        assert event.Prompt == "Test prompt"
    
    # TEST 8: Verify complete event creation with all fields including optional ones
    # Tests that an ImageUploadedEvent can include ImageUrl, Parameters, and UploadedAt
    def test_complete_event_creation(self):
        params = ProcessingParameters(width=768, height=768)
        
        event = ImageUploadedEvent(
            ImageId=str(uuid4()),
            OwnerId=str(uuid4()),
            ProjectId=str(uuid4()),
            ImageUrl="https://example.com/image.jpg",
            Prompt="Edit this image",
            Feature=ProcessingFeature.EDITOR,
            Parameters=params
        )
        
        assert event.ImageUrl == "https://example.com/image.jpg"
        assert event.Parameters.width == 768
    
    # TEST 9: Verify event serialization to dict with camelCase field aliases
    # Tests that model_dump with by_alias=True produces field names like 'ImageId' and 'Feature'
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
    
    # TEST 10: Verify all job status enum values
    # Tests that the JobStatus enum contains the expected values: pending, processing, completed, failed
    def test_all_status_values(self):
        assert JobStatus.PENDING.value == "pending"
        assert JobStatus.PROCESSING.value == "processing"
        assert JobStatus.COMPLETED.value == "completed"
        assert JobStatus.FAILED.value == "failed"
        
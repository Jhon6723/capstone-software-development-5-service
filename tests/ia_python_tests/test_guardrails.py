import sys
import os

# Add the Services/IA directory to path
SERVICES_IA_PATH = '/home/acer/Documentos/workspace/DS5/capstone/service/Services/IA'
if SERVICES_IA_PATH not in sys.path:
    sys.path.insert(0, SERVICES_IA_PATH)

# Set dummy environment variables to avoid API key errors
os.environ.setdefault('OPENAI_API_KEY', 'test-key-dummy')
os.environ.setdefault('PIXAZO_API_KEY', 'test-key-dummy')
os.environ.setdefault('POLLINATIONS_API_KEY', 'test-key-dummy')
os.environ.setdefault('CLOUDINARY_CLOUD_NAME', 'dummy')
os.environ.setdefault('CLOUDINARY_API_KEY', 'dummy')
os.environ.setdefault('CLOUDINARY_API_SECRET', 'dummy')

import pytest
from app.services.guardrails import Guardrails, ContentModerationError


class TestGuardrails:
    def setup_method(self):
        self.guardrails = Guardrails(use_openai_moderation=False)
    
    # TEST 1: Verify that safe prompts pass content moderation
    # Tests multiple safe prompts in both English and Spanish to ensure they are accepted
    def test_safe_prompt_passes(self):
        safe_prompts = [
            "A beautiful sunset over the mountains",
            "Un gato jugando en el jardín",
            "Modern architecture in a futuristic city",
        ]
        
        for prompt in safe_prompts:
            self.guardrails.check_prompt(prompt)
    
    # TEST 2: Verify that sexual content is properly blocked
    # Tests that prompts containing sexual keywords raise ContentModerationError with 'sexual' category
    def test_sexual_content_blocked(self):
        with pytest.raises(ContentModerationError) as exc:
            self.guardrails.check_prompt("naked woman on the beach")
        assert "sexual" in exc.value.category
    
    # TEST 3: Verify that violent content is properly blocked
    # Tests that prompts containing violence keywords raise ContentModerationError with 'violence' category
    def test_violent_content_blocked(self):
        with pytest.raises(ContentModerationError) as exc:
            self.guardrails.check_prompt("bloody murder scene")
        assert "violence" in exc.value.category
    
    # TEST 4: Verify that hate speech content is properly blocked
    # Tests that prompts containing hate speech keywords raise ContentModerationError with 'hate' category
    def test_hate_content_blocked(self):
        with pytest.raises(ContentModerationError) as exc:
            self.guardrails.check_prompt("nazi propaganda")
        assert "hate" in exc.value.category
    
    # TEST 5: Verify that is_safe method returns boolean values
    # Tests that safe prompts return True with no reason, and unsafe prompts return False with a reason
    def test_is_safe_method_returns_bool(self):
        is_safe, reason = self.guardrails.is_safe("A beautiful sunset")
        assert is_safe is True
        assert reason is None
        
        is_safe, reason = self.guardrails.is_safe("naked body")
        assert is_safe is False
        assert reason is not None
        
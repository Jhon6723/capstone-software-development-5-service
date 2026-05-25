"""Content safety guardrails for image generation prompts.

Blocks sexual, violent, hate speech, and other harmful content.
Uses keyword filtering + optional OpenAI moderation API.
"""

import re
import logging
from typing import Set, Optional
import os

logger = logging.getLogger(__name__)


class ContentModerationError(Exception):
    """Raised when content violates safety policies."""

    def __init__(self, reason: str, category: str):
        self.reason = reason
        self.category = category
        super().__init__(f"Content moderation failed: {category} - {reason}")


class Guardrails:
    """Content safety filter for image generation prompts."""

    # Spanish + English keywords for sexual content
    SEXUAL_KEYWORDS: Set[str] = {
        # Spanish
        "desnudo", "desnuda", "desnudos", "desnudas", "naked", "nude",
        "sexual", "sexo", "erotico", "erotica", "pornografia", "porno",
        "masturbacion", "orgasmo", "genital", "genitales", "pene", "vagina",
        "sensual", "provocativo", "escote", "topless", "playboy",
        # English
        "porn", "xxx", "nsfw", "hentai", "ecchi", "fetish", "bdsm",
        "naked woman", "naked man", "nude woman", "nude man", "bare chest",
        "lingerie", "bikini"  # context-dependent, flag for review
    }

    # Violent content keywords
    VIOLENCE_KEYWORDS: Set[str] = {
        # Spanish
        "sangre", "sangriento", "muerte", "asesinato", "tortura", "violencia",
        "arma", "armas", "pistola", "rifle", "cuchillo", "bomba", "explosion",
        "cadaver", "muerto", "matar", "golpear", "golpe", "herida", "herido",
        "guerra", "batalla", "pelea", "pelear", "terrorismo", "terrorista",
        # English
        "blood", "bloody", "gore", "violent", "death", "dead body",
        "murder", "kill", "killing", "torture", "weapon", "gun", "knife",
        "bomb", "explosion", "war", "battle", "fight", "fighting",
        "terrorist", "hostage", "beheading", "dismember", "corpse"
    }

    # Hate speech and harmful content
    HATE_KEYWORDS: Set[str] = {
        # Spanish
        "nazi", "hitler", "racista", "racismo", "esclavo", "esclavitud",
        "discriminacion", "odio", "xenofobia", "homofobia", "antisemita",
        # English
        "nazi", "hitler", "racist", "racism", "slavery", "slave",
        "discrimination", "hate", "xenophobia", "homophobic", "antisemitic",
        "kkk", "white supremacy", "ethnic cleansing", "genocide"
    }

    # Self-harm
    SELF_HARM_KEYWORDS: Set[str] = {
        "suicidio", "suicide", "suicidal", "self-harm", "autolesion",
        "cortarse", "cutting", "overdose", "sobredosis"
    }

    def __init__(self, use_openai_moderation: bool = False, openai_api_key: Optional[str] = None):
        self.use_openai = use_openai_moderation and openai_api_key is not None
        self.openai_api_key = openai_api_key

        # Compile regex patterns for efficiency (word boundaries to avoid false positives)
        self._sexual_pattern = re.compile(
            r'\b(' + '|'.join(re.escape(k) for k in self.SEXUAL_KEYWORDS) + r')\b',
            re.IGNORECASE
        )
        self._violence_pattern = re.compile(
            r'\b(' + '|'.join(re.escape(k) for k in self.VIOLENCE_KEYWORDS) + r')\b',
            re.IGNORECASE
        )
        self._hate_pattern = re.compile(
            r'\b(' + '|'.join(re.escape(k) for k in self.HATE_KEYWORDS) + r')\b',
            re.IGNORECASE
        )
        self._self_harm_pattern = re.compile(
            r'\b(' + '|'.join(re.escape(k) for k in self.SELF_HARM_KEYWORDS) + r')\b',
            re.IGNORECASE
        )

    def check_prompt(self, prompt: str) -> None:
        """Check if prompt violates safety policies.

        Raises:
            ContentModerationError: If content is flagged as unsafe.
        """
        if not prompt or not isinstance(prompt, str):
            raise ContentModerationError("Invalid prompt", "invalid")

        prompt_lower = prompt.lower()

        # Check sexual content
        sexual_matches = self._sexual_pattern.findall(prompt_lower)
        if sexual_matches:
            logger.warning("Sexual content detected: %s", sexual_matches)
            raise ContentModerationError(
                f"Sexual content detected: {', '.join(set(sexual_matches))}",
                "sexual"
            )

        # Check violent content
        violence_matches = self._violence_pattern.findall(prompt_lower)
        if violence_matches:
            logger.warning("Violent content detected: %s", violence_matches)
            raise ContentModerationError(
                f"Violent content detected: {', '.join(set(violence_matches))}",
                "violence"
            )

        # Check hate speech
        hate_matches = self._hate_pattern.findall(prompt_lower)
        if hate_matches:
            logger.warning("Hate content detected: %s", hate_matches)
            raise ContentModerationError(
                f"Harmful content detected: {', '.join(set(hate_matches))}",
                "hate"
            )

        # Check self-harm
        self_harm_matches = self._self_harm_pattern.findall(prompt_lower)
        if self_harm_matches:
            logger.warning("Self-harm content detected: %s", self_harm_matches)
            raise ContentModerationError(
                f"Self-harm content detected: {', '.join(set(self_harm_matches))}",
                "self_harm"
            )

        logger.info("Prompt passed safety check")

    def is_safe(self, prompt: str) -> tuple[bool, Optional[str]]:
        """Check if prompt is safe without raising exception.

        Returns:
            Tuple of (is_safe, reason_if_unsafe)
        """
        try:
            self.check_prompt(prompt)
            return True, None
        except ContentModerationError as e:
            return False, str(e)


# Global instance for reuse
def get_guardrails() -> Guardrails:
    """Get configured guardrails instance."""
    use_openai = os.getenv("USE_OPENAI_MODERATION", "false").lower() == "true"
    openai_key = os.getenv("OPENAI_API_KEY") if use_openai else None
    return Guardrails(use_openai_moderation=use_openai, openai_api_key=openai_key)

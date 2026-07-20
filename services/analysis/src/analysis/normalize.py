"""Path and field-path normalization shared by clustering and explanation."""

from __future__ import annotations

import re

_UUID = re.compile(r"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")
_HEX = re.compile(r"^[0-9a-fA-F]{16,}$")
_ARRAY_INDEX = re.compile(r"\[\d+\]")


def normalize_path(path: str) -> str:
    """Collapse variable URL segments so ``/orders/123`` and ``/orders/456`` match."""
    segments = path.split("/")
    out: list[str] = []
    for segment in segments:
        if not segment:
            out.append(segment)
        elif segment.isdigit():
            out.append("{id}")
        elif _UUID.match(segment):
            out.append("{uuid}")
        elif _HEX.match(segment):
            out.append("{hex}")
        else:
            out.append(segment)
    return "/".join(out)


def normalize_field_path(path: str) -> str:
    """Collapse array indices so ``items[0].total`` and ``items[1].total`` match."""
    return _ARRAY_INDEX.sub("[]", path)

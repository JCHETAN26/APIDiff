from google.protobuf.internal import containers as _containers
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class Verdict(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    VERDICT_UNSPECIFIED: _ClassVar[Verdict]
    VERDICT_PASS: _ClassVar[Verdict]
    VERDICT_BEHAVIORAL_REGRESSION: _ClassVar[Verdict]
    VERDICT_PERF_REGRESSION: _ClassVar[Verdict]
    VERDICT_ERROR: _ClassVar[Verdict]
VERDICT_UNSPECIFIED: Verdict
VERDICT_PASS: Verdict
VERDICT_BEHAVIORAL_REGRESSION: Verdict
VERDICT_PERF_REGRESSION: Verdict
VERDICT_ERROR: Verdict

class Header(_message.Message):
    __slots__ = ("name", "value")
    NAME_FIELD_NUMBER: _ClassVar[int]
    VALUE_FIELD_NUMBER: _ClassVar[int]
    name: str
    value: str
    def __init__(self, name: _Optional[str] = ..., value: _Optional[str] = ...) -> None: ...

class HttpRequest(_message.Message):
    __slots__ = ("method", "path", "query", "headers", "body")
    METHOD_FIELD_NUMBER: _ClassVar[int]
    PATH_FIELD_NUMBER: _ClassVar[int]
    QUERY_FIELD_NUMBER: _ClassVar[int]
    HEADERS_FIELD_NUMBER: _ClassVar[int]
    BODY_FIELD_NUMBER: _ClassVar[int]
    method: str
    path: str
    query: str
    headers: _containers.RepeatedCompositeFieldContainer[Header]
    body: bytes
    def __init__(self, method: _Optional[str] = ..., path: _Optional[str] = ..., query: _Optional[str] = ..., headers: _Optional[_Iterable[_Union[Header, _Mapping]]] = ..., body: _Optional[bytes] = ...) -> None: ...

class HttpResponse(_message.Message):
    __slots__ = ("status_code", "headers", "body", "latency_ms")
    STATUS_CODE_FIELD_NUMBER: _ClassVar[int]
    HEADERS_FIELD_NUMBER: _ClassVar[int]
    BODY_FIELD_NUMBER: _ClassVar[int]
    LATENCY_MS_FIELD_NUMBER: _ClassVar[int]
    status_code: int
    headers: _containers.RepeatedCompositeFieldContainer[Header]
    body: bytes
    latency_ms: int
    def __init__(self, status_code: _Optional[int] = ..., headers: _Optional[_Iterable[_Union[Header, _Mapping]]] = ..., body: _Optional[bytes] = ..., latency_ms: _Optional[int] = ...) -> None: ...

class Target(_message.Message):
    __slots__ = ("label", "base_url")
    LABEL_FIELD_NUMBER: _ClassVar[int]
    BASE_URL_FIELD_NUMBER: _ClassVar[int]
    label: str
    base_url: str
    def __init__(self, label: _Optional[str] = ..., base_url: _Optional[str] = ...) -> None: ...

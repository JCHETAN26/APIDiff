from apidiff.common.v1 import common_pb2 as _common_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf.internal import enum_type_wrapper as _enum_type_wrapper
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class DiffKind(int, metaclass=_enum_type_wrapper.EnumTypeWrapper):
    __slots__ = ()
    DIFF_KIND_UNSPECIFIED: _ClassVar[DiffKind]
    DIFF_KIND_ADDED: _ClassVar[DiffKind]
    DIFF_KIND_REMOVED: _ClassVar[DiffKind]
    DIFF_KIND_CHANGED: _ClassVar[DiffKind]
DIFF_KIND_UNSPECIFIED: DiffKind
DIFF_KIND_ADDED: DiffKind
DIFF_KIND_REMOVED: DiffKind
DIFF_KIND_CHANGED: DiffKind

class Scenario(_message.Message):
    __slots__ = ("id", "request", "reference_response")
    ID_FIELD_NUMBER: _ClassVar[int]
    REQUEST_FIELD_NUMBER: _ClassVar[int]
    REFERENCE_RESPONSE_FIELD_NUMBER: _ClassVar[int]
    id: str
    request: _common_pb2.HttpRequest
    reference_response: _common_pb2.HttpResponse
    def __init__(self, id: _Optional[str] = ..., request: _Optional[_Union[_common_pb2.HttpRequest, _Mapping]] = ..., reference_response: _Optional[_Union[_common_pb2.HttpResponse, _Mapping]] = ...) -> None: ...

class ReplayConfig(_message.Message):
    __slots__ = ("concurrency", "request_timeout_ms", "ignore_fields", "latency_regression_ratio")
    CONCURRENCY_FIELD_NUMBER: _ClassVar[int]
    REQUEST_TIMEOUT_MS_FIELD_NUMBER: _ClassVar[int]
    IGNORE_FIELDS_FIELD_NUMBER: _ClassVar[int]
    LATENCY_REGRESSION_RATIO_FIELD_NUMBER: _ClassVar[int]
    concurrency: int
    request_timeout_ms: int
    ignore_fields: _containers.RepeatedScalarFieldContainer[str]
    latency_regression_ratio: float
    def __init__(self, concurrency: _Optional[int] = ..., request_timeout_ms: _Optional[int] = ..., ignore_fields: _Optional[_Iterable[str]] = ..., latency_regression_ratio: _Optional[float] = ...) -> None: ...

class ReplayRequest(_message.Message):
    __slots__ = ("run_id", "scenarios", "baseline", "candidate", "config")
    RUN_ID_FIELD_NUMBER: _ClassVar[int]
    SCENARIOS_FIELD_NUMBER: _ClassVar[int]
    BASELINE_FIELD_NUMBER: _ClassVar[int]
    CANDIDATE_FIELD_NUMBER: _ClassVar[int]
    CONFIG_FIELD_NUMBER: _ClassVar[int]
    run_id: str
    scenarios: _containers.RepeatedCompositeFieldContainer[Scenario]
    baseline: _common_pb2.Target
    candidate: _common_pb2.Target
    config: ReplayConfig
    def __init__(self, run_id: _Optional[str] = ..., scenarios: _Optional[_Iterable[_Union[Scenario, _Mapping]]] = ..., baseline: _Optional[_Union[_common_pb2.Target, _Mapping]] = ..., candidate: _Optional[_Union[_common_pb2.Target, _Mapping]] = ..., config: _Optional[_Union[ReplayConfig, _Mapping]] = ...) -> None: ...

class FieldDiff(_message.Message):
    __slots__ = ("path", "baseline_value", "candidate_value", "kind")
    PATH_FIELD_NUMBER: _ClassVar[int]
    BASELINE_VALUE_FIELD_NUMBER: _ClassVar[int]
    CANDIDATE_VALUE_FIELD_NUMBER: _ClassVar[int]
    KIND_FIELD_NUMBER: _ClassVar[int]
    path: str
    baseline_value: str
    candidate_value: str
    kind: DiffKind
    def __init__(self, path: _Optional[str] = ..., baseline_value: _Optional[str] = ..., candidate_value: _Optional[str] = ..., kind: _Optional[_Union[DiffKind, str]] = ...) -> None: ...

class Diff(_message.Message):
    __slots__ = ("fields", "has_behavioral_change")
    FIELDS_FIELD_NUMBER: _ClassVar[int]
    HAS_BEHAVIORAL_CHANGE_FIELD_NUMBER: _ClassVar[int]
    fields: _containers.RepeatedCompositeFieldContainer[FieldDiff]
    has_behavioral_change: bool
    def __init__(self, fields: _Optional[_Iterable[_Union[FieldDiff, _Mapping]]] = ..., has_behavioral_change: _Optional[bool] = ...) -> None: ...

class ReplayResponse(_message.Message):
    __slots__ = ("result",)
    RESULT_FIELD_NUMBER: _ClassVar[int]
    result: ReplayResult
    def __init__(self, result: _Optional[_Union[ReplayResult, _Mapping]] = ...) -> None: ...

class ReplayResult(_message.Message):
    __slots__ = ("run_id", "scenario_id", "baseline_response", "candidate_response", "diff", "verdict", "latency_delta_ms", "error")
    RUN_ID_FIELD_NUMBER: _ClassVar[int]
    SCENARIO_ID_FIELD_NUMBER: _ClassVar[int]
    BASELINE_RESPONSE_FIELD_NUMBER: _ClassVar[int]
    CANDIDATE_RESPONSE_FIELD_NUMBER: _ClassVar[int]
    DIFF_FIELD_NUMBER: _ClassVar[int]
    VERDICT_FIELD_NUMBER: _ClassVar[int]
    LATENCY_DELTA_MS_FIELD_NUMBER: _ClassVar[int]
    ERROR_FIELD_NUMBER: _ClassVar[int]
    run_id: str
    scenario_id: str
    baseline_response: _common_pb2.HttpResponse
    candidate_response: _common_pb2.HttpResponse
    diff: Diff
    verdict: _common_pb2.Verdict
    latency_delta_ms: int
    error: str
    def __init__(self, run_id: _Optional[str] = ..., scenario_id: _Optional[str] = ..., baseline_response: _Optional[_Union[_common_pb2.HttpResponse, _Mapping]] = ..., candidate_response: _Optional[_Union[_common_pb2.HttpResponse, _Mapping]] = ..., diff: _Optional[_Union[Diff, _Mapping]] = ..., verdict: _Optional[_Union[_common_pb2.Verdict, str]] = ..., latency_delta_ms: _Optional[int] = ..., error: _Optional[str] = ...) -> None: ...

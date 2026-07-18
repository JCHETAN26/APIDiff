from apidiff.replay.v1 import replay_pb2 as _replay_pb2
from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class ScenarioDescriptor(_message.Message):
    __slots__ = ("scenario_id", "method", "path", "fingerprint")
    SCENARIO_ID_FIELD_NUMBER: _ClassVar[int]
    METHOD_FIELD_NUMBER: _ClassVar[int]
    PATH_FIELD_NUMBER: _ClassVar[int]
    FINGERPRINT_FIELD_NUMBER: _ClassVar[int]
    scenario_id: str
    method: str
    path: str
    fingerprint: str
    def __init__(self, scenario_id: _Optional[str] = ..., method: _Optional[str] = ..., path: _Optional[str] = ..., fingerprint: _Optional[str] = ...) -> None: ...

class ClusterScenariosRequest(_message.Message):
    __slots__ = ("project_id", "scenarios")
    PROJECT_ID_FIELD_NUMBER: _ClassVar[int]
    SCENARIOS_FIELD_NUMBER: _ClassVar[int]
    project_id: str
    scenarios: _containers.RepeatedCompositeFieldContainer[ScenarioDescriptor]
    def __init__(self, project_id: _Optional[str] = ..., scenarios: _Optional[_Iterable[_Union[ScenarioDescriptor, _Mapping]]] = ...) -> None: ...

class ScenarioCluster(_message.Message):
    __slots__ = ("cluster_id", "representative_scenario_id", "member_scenario_ids")
    CLUSTER_ID_FIELD_NUMBER: _ClassVar[int]
    REPRESENTATIVE_SCENARIO_ID_FIELD_NUMBER: _ClassVar[int]
    MEMBER_SCENARIO_IDS_FIELD_NUMBER: _ClassVar[int]
    cluster_id: str
    representative_scenario_id: str
    member_scenario_ids: _containers.RepeatedScalarFieldContainer[str]
    def __init__(self, cluster_id: _Optional[str] = ..., representative_scenario_id: _Optional[str] = ..., member_scenario_ids: _Optional[_Iterable[str]] = ...) -> None: ...

class ClusterScenariosResponse(_message.Message):
    __slots__ = ("clusters",)
    CLUSTERS_FIELD_NUMBER: _ClassVar[int]
    clusters: _containers.RepeatedCompositeFieldContainer[ScenarioCluster]
    def __init__(self, clusters: _Optional[_Iterable[_Union[ScenarioCluster, _Mapping]]] = ...) -> None: ...

class ExplainFailuresRequest(_message.Message):
    __slots__ = ("run_id", "failures")
    RUN_ID_FIELD_NUMBER: _ClassVar[int]
    FAILURES_FIELD_NUMBER: _ClassVar[int]
    run_id: str
    failures: _containers.RepeatedCompositeFieldContainer[_replay_pb2.ReplayResult]
    def __init__(self, run_id: _Optional[str] = ..., failures: _Optional[_Iterable[_Union[_replay_pb2.ReplayResult, _Mapping]]] = ...) -> None: ...

class FailureExplanation(_message.Message):
    __slots__ = ("title", "detail", "scenario_ids", "severity", "likely_cause")
    TITLE_FIELD_NUMBER: _ClassVar[int]
    DETAIL_FIELD_NUMBER: _ClassVar[int]
    SCENARIO_IDS_FIELD_NUMBER: _ClassVar[int]
    SEVERITY_FIELD_NUMBER: _ClassVar[int]
    LIKELY_CAUSE_FIELD_NUMBER: _ClassVar[int]
    title: str
    detail: str
    scenario_ids: _containers.RepeatedScalarFieldContainer[str]
    severity: float
    likely_cause: str
    def __init__(self, title: _Optional[str] = ..., detail: _Optional[str] = ..., scenario_ids: _Optional[_Iterable[str]] = ..., severity: _Optional[float] = ..., likely_cause: _Optional[str] = ...) -> None: ...

class ExplainFailuresResponse(_message.Message):
    __slots__ = ("explanations",)
    EXPLANATIONS_FIELD_NUMBER: _ClassVar[int]
    explanations: _containers.RepeatedCompositeFieldContainer[FailureExplanation]
    def __init__(self, explanations: _Optional[_Iterable[_Union[FailureExplanation, _Mapping]]] = ...) -> None: ...

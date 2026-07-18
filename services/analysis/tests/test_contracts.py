"""Contract test: the generated gRPC stubs import and round-trip.

Satisfies the Phase 1 goal that generated stubs compile in Python.
"""

from apidiff.analysis.v1 import analysis_pb2_grpc  # noqa: F401  (import must succeed)
from apidiff.common.v1 import common_pb2
from apidiff.replay.v1 import replay_pb2


def test_replay_request_roundtrips() -> None:
    req = replay_pb2.ReplayRequest(run_id="run-1")
    req.scenarios.add(id="s-1")
    parsed = replay_pb2.ReplayRequest.FromString(req.SerializeToString())
    assert parsed.run_id == "run-1"
    assert parsed.scenarios[0].id == "s-1"


def test_verdict_enum_values() -> None:
    assert common_pb2.Verdict.VERDICT_PASS == 1
    assert common_pb2.Verdict.VERDICT_BEHAVIORAL_REGRESSION == 2

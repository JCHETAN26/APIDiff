from analysis import __version__
from analysis.health import report


def test_report() -> None:
    status = report()
    assert status == {
        "service": "analysis",
        "status": "ok",
        "version": __version__,
    }

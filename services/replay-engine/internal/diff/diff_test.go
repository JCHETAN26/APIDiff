package diff

import (
	"testing"

	commonv1 "github.com/apidiff/replay-engine/gen/apidiff/common/v1"
	replayv1 "github.com/apidiff/replay-engine/gen/apidiff/replay/v1"
)

func resp(status int32, body string) *commonv1.HttpResponse {
	return &commonv1.HttpResponse{StatusCode: status, Body: []byte(body)}
}

func fieldByPath(d *replayv1.Diff, path string) *replayv1.FieldDiff {
	for _, f := range d.GetFields() {
		if f.GetPath() == path {
			return f
		}
	}
	return nil
}

func TestIdenticalHasNoDiff(t *testing.T) {
	d := Compare(resp(200, `{"a":1,"b":"x"}`), resp(200, `{"b":"x","a":1}`), nil)
	if d.GetHasBehavioralChange() {
		t.Fatalf("expected no behavioral change, got %v", d.GetFields())
	}
}

func TestChangedValue(t *testing.T) {
	d := Compare(resp(200, `{"total":10}`), resp(200, `{"total":12}`), nil)
	f := fieldByPath(d, "total")
	if f == nil || f.GetKind() != replayv1.DiffKind_DIFF_KIND_CHANGED {
		t.Fatalf("expected changed total, got %v", d.GetFields())
	}
	if f.GetBaselineValue() != "10" || f.GetCandidateValue() != "12" {
		t.Errorf("values = %q -> %q", f.GetBaselineValue(), f.GetCandidateValue())
	}
}

func TestAddedAndRemoved(t *testing.T) {
	d := Compare(resp(200, `{"a":1,"gone":2}`), resp(200, `{"a":1,"added":3}`), nil)
	if f := fieldByPath(d, "gone"); f == nil || f.GetKind() != replayv1.DiffKind_DIFF_KIND_REMOVED {
		t.Errorf("expected removed 'gone'")
	}
	if f := fieldByPath(d, "added"); f == nil || f.GetKind() != replayv1.DiffKind_DIFF_KIND_ADDED {
		t.Errorf("expected added 'added'")
	}
}

func TestNestedPathAndIgnore(t *testing.T) {
	base := resp(200, `{"data":{"total":1,"createdAt":"t1"}}`)
	cand := resp(200, `{"data":{"total":2,"createdAt":"t2"}}`)

	// Without ignore, both fields differ.
	d := Compare(base, cand, nil)
	if fieldByPath(d, "data.total") == nil || fieldByPath(d, "data.createdAt") == nil {
		t.Fatalf("expected nested diffs, got %v", d.GetFields())
	}

	// Ignoring by bare leaf name and by full path removes those fields.
	d2 := Compare(base, cand, []string{"createdAt"})
	if fieldByPath(d2, "data.createdAt") != nil {
		t.Errorf("createdAt should be ignored by leaf name")
	}
	d3 := Compare(base, cand, []string{"data.total", "data.createdAt"})
	if d3.GetHasBehavioralChange() {
		t.Errorf("all diffs ignored, expected none: %v", d3.GetFields())
	}
}

func TestStatusCodeDiff(t *testing.T) {
	d := Compare(resp(200, `{}`), resp(500, `{}`), nil)
	if f := fieldByPath(d, "status_code"); f == nil || f.GetCandidateValue() != "500" {
		t.Fatalf("expected status_code diff, got %v", d.GetFields())
	}
}

func TestNonJSONBody(t *testing.T) {
	same := Compare(resp(200, "hello"), resp(200, "hello"), nil)
	if same.GetHasBehavioralChange() {
		t.Errorf("identical text should not differ")
	}
	diff := Compare(resp(200, "hello"), resp(200, "world"), nil)
	if f := fieldByPath(diff, "body"); f == nil {
		t.Errorf("expected raw body diff")
	}
}

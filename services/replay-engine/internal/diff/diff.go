// Package diff compares a baseline and a candidate HTTP response, producing a
// structural + value diff with support for ignoring volatile fields.
package diff

import (
	"bytes"
	"encoding/json"
	"fmt"
	"reflect"

	commonv1 "github.com/apidiff/replay-engine/gen/apidiff/common/v1"
	replayv1 "github.com/apidiff/replay-engine/gen/apidiff/replay/v1"
)

// Compare returns the differences between baseline and candidate. ignore holds
// field paths (dot/(index) notation, e.g. "data.createdAt") or bare leaf names
// (e.g. "timestamp") that should be excluded from the comparison.
func Compare(baseline, candidate *commonv1.HttpResponse, ignore []string) *replayv1.Diff {
	ignored := make(map[string]bool, len(ignore))
	for _, f := range ignore {
		ignored[f] = true
	}

	var fields []*replayv1.FieldDiff

	if baseline.GetStatusCode() != candidate.GetStatusCode() {
		fields = append(fields, &replayv1.FieldDiff{
			Path:           "status_code",
			BaselineValue:  fmt.Sprintf("%d", baseline.GetStatusCode()),
			CandidateValue: fmt.Sprintf("%d", candidate.GetStatusCode()),
			Kind:           replayv1.DiffKind_DIFF_KIND_CHANGED,
		})
	}

	fields = append(fields, diffBody(baseline.GetBody(), candidate.GetBody(), ignored)...)

	return &replayv1.Diff{
		Fields:              fields,
		HasBehavioralChange: len(fields) > 0,
	}
}

func diffBody(baseline, candidate []byte, ignored map[string]bool) []*replayv1.FieldDiff {
	var baseJSON, candJSON any
	baseOK := json.Unmarshal(baseline, &baseJSON) == nil
	candOK := json.Unmarshal(candidate, &candJSON) == nil

	// Fall back to a raw byte comparison when either side is not JSON.
	if !baseOK || !candOK {
		if bytes.Equal(baseline, candidate) {
			return nil
		}
		return []*replayv1.FieldDiff{{
			Path:           "body",
			BaselineValue:  string(baseline),
			CandidateValue: string(candidate),
			Kind:           replayv1.DiffKind_DIFF_KIND_CHANGED,
		}}
	}

	var diffs []*replayv1.FieldDiff
	walk("", baseJSON, candJSON, ignored, &diffs)
	return diffs
}

func walk(path string, a, b any, ignored map[string]bool, diffs *[]*replayv1.FieldDiff) {
	switch av := a.(type) {
	case map[string]any:
		bv, ok := b.(map[string]any)
		if !ok {
			appendChanged(path, a, b, diffs)
			return
		}
		for key, aVal := range av {
			cp := child(path, key)
			if ignored[cp] || ignored[key] {
				continue
			}
			bVal, exists := bv[key]
			if !exists {
				*diffs = append(*diffs, &replayv1.FieldDiff{
					Path: cp, BaselineValue: jsonStr(aVal), Kind: replayv1.DiffKind_DIFF_KIND_REMOVED,
				})
				continue
			}
			walk(cp, aVal, bVal, ignored, diffs)
		}
		for key, bVal := range bv {
			cp := child(path, key)
			if ignored[cp] || ignored[key] {
				continue
			}
			if _, exists := av[key]; !exists {
				*diffs = append(*diffs, &replayv1.FieldDiff{
					Path: cp, CandidateValue: jsonStr(bVal), Kind: replayv1.DiffKind_DIFF_KIND_ADDED,
				})
			}
		}

	case []any:
		bv, ok := b.([]any)
		if !ok || len(av) != len(bv) {
			appendChanged(path, a, b, diffs)
			return
		}
		for i := range av {
			walk(fmt.Sprintf("%s[%d]", path, i), av[i], bv[i], ignored, diffs)
		}

	default:
		if !reflect.DeepEqual(a, b) {
			appendChanged(path, a, b, diffs)
		}
	}
}

func appendChanged(path string, a, b any, diffs *[]*replayv1.FieldDiff) {
	if path == "" {
		path = "body"
	}
	*diffs = append(*diffs, &replayv1.FieldDiff{
		Path:           path,
		BaselineValue:  jsonStr(a),
		CandidateValue: jsonStr(b),
		Kind:           replayv1.DiffKind_DIFF_KIND_CHANGED,
	})
}

func child(path, key string) string {
	if path == "" {
		return key
	}
	return path + "." + key
}

func jsonStr(v any) string {
	b, err := json.Marshal(v)
	if err != nil {
		return fmt.Sprintf("%v", v)
	}
	return string(b)
}

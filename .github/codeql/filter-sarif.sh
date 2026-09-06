#!/usr/bin/env bash
# Drop SARIF results whose primary location is generated code.
#
# CodeQL analyses a compiled language with build-mode: manual by extracting
# everything the real build compiles, which includes source-generator output
# that never exists as a file in the repository. GitHub's config-file
# paths-ignore filter does not apply to that mode, so the only place these
# results can be removed without disabling a query for hand-written code too
# is the SARIF file, in between codeql-action/analyze and
# codeql-action/upload-sarif.
#
# A result is dropped only when its *primary* location (locations[0]) has an
# exact "<dir>/" path segment matching one of --exclude-dir's arguments.
# Results that merely reference generated code from a relatedLocations entry
# or a codeFlows step are kept, because those are real findings in
# hand-written code that happen to pass through generated code.
#
# Everything else in the SARIF is passed through untouched -- tool,
# automationDetails, versionControlProvenance, artifacts, properties -- so
# fingerprints and analysis-matching keep working. The artifacts array is
# deliberately not re-indexed: artifactLocation.index values in surviving
# results point into it.
#
# This intentionally matches an exact path segment rather than implementing
# general glob syntax (CodeQL's own SARIF URIs for this repo are plain
# relative paths, so unlike the tool this replaced there is no URL-decoding
# step either) -- the one pattern this has ever needed is "somewhere under an
# obj/ directory", and a segment match says that directly.
set -euo pipefail

INPUT=""
OUTPUT=""
EXCLUDE_DIRS=()

while [[ $# -gt 0 ]]; do
  case "$1" in
    --input)
      INPUT="$2"
      shift 2
      ;;
    --output)
      OUTPUT="$2"
      shift 2
      ;;
    --exclude-dir)
      EXCLUDE_DIRS+=("$2")
      shift 2
      ;;
    *)
      echo "filter-sarif: unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

if [[ -z "$INPUT" || -z "$OUTPUT" ]]; then
  echo "filter-sarif: --input and --output are required" >&2
  exit 1
fi
if [[ ${#EXCLUDE_DIRS[@]} -eq 0 ]]; then
  echo "filter-sarif: at least one --exclude-dir is required" >&2
  exit 1
fi

# jq is given a scratch file to write to and the result is moved into place at the end,
# so --input and --output are allowed to name the same path. Redirecting straight to
# "$OUTPUT" would not be: the shell truncates a redirection target before jq is exec'd, so
# an in-place run would hand jq an empty file and quietly emit an empty SARIF.
SCRATCH=$(mktemp)
trap 'rm -f "$SCRATCH"' EXIT

SEGMENTS_JSON=$(printf '%s\n' "${EXCLUDE_DIRS[@]}" | jq -R . | jq -s .)

# jq definitions shared by the stats pass and the filtering pass.
read -r -d '' JQ_DEFS <<'EOF' || true
def uri_for($result; $arts):
  ($result.locations[0].physicalLocation.artifactLocation) as $al
  | if $al == null then null
    elif $al.uri != null then $al.uri
    elif ($al.index != null and $al.index >= 0 and $al.index < ($arts | length))
      then ($arts[$al.index].location.uri // null)
    else null
    end;

def is_excluded($uri; $segments):
  ($uri // "") | split("/") | any(. as $s | $segments | index($s) != null);
EOF

STATS=$(jq -c --argjson segments "$SEGMENTS_JSON" "
  $JQ_DEFS
  [.runs[] | (.artifacts // []) as \$arts | (.results // [])[]
     | {rule: (.ruleId // \"<no rule id>\"),
        dropped: (is_excluded(uri_for(.; \$arts); \$segments))}]
  | {total: length,
     dropped: (map(select(.dropped)) | length),
     by_rule: (map(select(.dropped)) | group_by(.rule)
               | map({rule: .[0].rule, count: length}) | sort_by(-.count, .rule))}
" "$INPUT")

jq --argjson segments "$SEGMENTS_JSON" "
  $JQ_DEFS
  .runs |= map(
    (.artifacts // []) as \$arts
    | .results |= map(select(is_excluded(uri_for(.; \$arts); \$segments) | not))
  )
" "$INPUT" >"$SCRATCH"

TOTAL=$(jq -r '.total' <<<"$STATS")
DROPPED=$(jq -r '.dropped' <<<"$STATS")
KEPT=$((TOTAL - DROPPED))

echo "filter-sarif: ${TOTAL} results in, dropped ${DROPPED}, kept ${KEPT}"
if [[ "$DROPPED" -eq 0 ]]; then
  echo "filter-sarif:   (nothing matched the exclude patterns)"
else
  jq -r '.by_rule[] | "filter-sarif:   \(.count)  \(.rule)"' <<<"$STATS"
fi

# Check the file that was actually written, rather than trusting the stats pass: the two jq
# invocations read the same input but only this one produced the artifact that gets uploaded,
# and a SARIF that is truncated or unparseable would otherwise reach upload-sarif as a bare
# "Invalid SARIF. JSON syntax error" with nothing pointing back at this script.
#
# Note this deliberately does not treat "nothing left" as an error. Every remaining result
# living in generated code is the goal state, not a misconfigured --exclude-dir, and failing
# on it would turn a repository with no hand-written alerts into a red build.
if ! WRITTEN=$(jq '[.runs[] | (.results // [])[]] | length' "$SCRATCH" 2>&1); then
  echo "filter-sarif: the filtered SARIF is not valid JSON; refusing to overwrite ${OUTPUT}" >&2
  echo "filter-sarif:   ${WRITTEN}" >&2
  exit 1
fi
if [[ "$WRITTEN" -ne "$KEPT" ]]; then
  echo "filter-sarif: expected ${KEPT} result(s) in the filtered SARIF but found ${WRITTEN}" >&2
  exit 1
fi

mv "$SCRATCH" "$OUTPUT"

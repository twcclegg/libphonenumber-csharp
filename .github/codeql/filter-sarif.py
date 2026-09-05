#!/usr/bin/env python3
"""Drop SARIF results whose primary location is generated code.

CodeQL analyses a compiled language with ``build-mode: manual`` by extracting
everything the real build compiles, which includes source-generator output that
never exists as a file in the repository. GitHub's config-file ``paths-ignore``
filter does not apply to that mode, so the only place these results can be
removed without disabling a query for hand-written code too is the SARIF file,
in between ``codeql-action/analyze`` and ``codeql-action/upload-sarif``.

A result is dropped only when its *primary* location (``locations[0]``) is in an
excluded path. Results that merely reference generated code from a
``relatedLocations`` entry or a ``codeFlows`` step are kept, because those are
real findings in hand-written code that happen to pass through generated code.

Everything else in the SARIF is passed through untouched -- ``tool``,
``automationDetails``, ``versionControlProvenance``, ``artifacts``, ``properties``
-- so fingerprints and analysis-matching keep working. In particular the
``artifacts`` array is deliberately not re-indexed: ``artifactLocation.index``
values in surviving results point into it.
"""

from __future__ import annotations

import argparse
import fnmatch
import json
import re
import sys
import urllib.parse


def compile_pattern(pattern: str) -> re.Pattern[str]:
    """Translate a glob into a regex where ``*`` does not cross ``/`` but ``**`` does."""
    out = []
    i = 0
    while i < len(pattern):
        char = pattern[i]
        if char == "*":
            if pattern.startswith("**", i):
                # '**/' may also match zero directories, so 'a/**/b' matches 'a/b'.
                if pattern.startswith("**/", i):
                    out.append("(?:.*/)?")
                    i += 3
                    continue
                out.append(".*")
                i += 2
                continue
            out.append("[^/]*")
            i += 1
            continue
        if char == "?":
            out.append("[^/]")
            i += 1
            continue
        out.append(re.escape(char))
        i += 1
    return re.compile("^" + "".join(out) + "$")


def result_uri(result: dict, artifacts: list) -> str | None:
    """Return the URI of a result's primary location, or None if it has none."""
    locations = result.get("locations") or []
    if not locations:
        return None
    physical = locations[0].get("physicalLocation") or {}
    artifact = physical.get("artifactLocation") or {}
    uri = artifact.get("uri")
    if uri is None:
        index = artifact.get("index")
        if isinstance(index, int) and 0 <= index < len(artifacts):
            uri = (artifacts[index].get("location") or {}).get("uri")
    if uri is None:
        return None
    return urllib.parse.unquote(uri).lstrip("/")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument(
        "--exclude",
        action="append",
        default=[],
        metavar="GLOB",
        help="drop results whose primary location matches this glob (repeatable)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="report what would be dropped but write the input through unchanged",
    )
    args = parser.parse_args()

    if not args.exclude:
        parser.error("at least one --exclude pattern is required")
    patterns = [(glob, compile_pattern(glob)) for glob in args.exclude]

    with open(args.input, encoding="utf-8") as handle:
        sarif = json.load(handle)

    total = dropped = 0
    by_rule: dict[str, int] = {}

    for run in sarif.get("runs", []):
        artifacts = run.get("artifacts") or []
        kept = []
        for result in run.get("results", []):
            total += 1
            uri = result_uri(result, artifacts)
            match = None
            if uri is not None:
                for glob, regex in patterns:
                    if regex.match(uri):
                        match = glob
                        break
            if match is None:
                kept.append(result)
                continue
            dropped += 1
            rule = result.get("ruleId") or "<no rule id>"
            by_rule[rule] = by_rule.get(rule, 0) + 1
        if not args.dry_run:
            run["results"] = kept

    verb = "would drop" if args.dry_run else "dropped"
    print(f"filter-sarif: {total} results in, {verb} {dropped}, kept {total - dropped}")
    for rule, count in sorted(by_rule.items(), key=lambda item: (-item[1], item[0])):
        print(f"filter-sarif:   {count:5d}  {rule}")
    if not by_rule:
        print("filter-sarif:   (nothing matched the exclude patterns)")

    with open(args.output, "w", encoding="utf-8") as handle:
        json.dump(sarif, handle)
    return 0


if __name__ == "__main__":
    sys.exit(main())

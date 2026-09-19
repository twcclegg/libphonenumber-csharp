#!/usr/bin/env bash
# Proves the data-set opt-out actually removes the embedded resources from a trimmed publish.
#
# The unit tests can only check that the two halves of the feature agree on names, because the
# library's own build does not trim. If ILLink silently stopped applying the substitutions -- a
# renamed resource, a dropped Trim="true", a feature name that no longer matches -- every test
# would still pass and every consumer would keep shipping the 2.4 MB they asked to drop. This is
# the only check that would notice.
set -euo pipefail

project="csharp/PhoneNumbers.TrimTest/PhoneNumbers.TrimTest.csproj"
# Default to the host architecture: the checks below run the published binary, and CI runs on
# ubuntu-24.04-arm where a linux-x64 publish would build fine and then refuse to execute.
if [ -n "${1:-}" ]
then
  rid="$1"
else
  case "$(uname -m)" in
    aarch64|arm64) rid="linux-arm64" ;;
    *)             rid="linux-x64" ;;
  esac
fi
out_root="${RUNNER_TEMP:-/tmp}/trimcheck"

# The full data set is ~2.4 MB of the assembly; these bounds are wide enough that normal metadata
# growth will not trip them, and far too tight to pass if a removal silently stopped working.
max_opted_out_bytes=600000
min_default_bytes=2000000

rm -rf "${out_root}"

publish() {
  local name="$1" dir="${out_root}/$1"
  shift
  # Restore from scratch each time. Trimming needs Microsoft.NET.ILLink.Tasks resolved at RESTORE
  # time, so an obj/ left behind by a plain build or an evaluation-only MSBuild invocation makes
  # publish skip ILLink silently -- the output looks like a successful publish and every size
  # assertion below then fails for the wrong reason.
  rm -rf "$(dirname "${project}")/obj" "$(dirname "${project}")/bin"
  dotnet publish "${project}" -c Release -r "${rid}" --self-contained -o "${dir}" "$@" >"${out_root}/${name}.log" 2>&1 || {
    echo "error: publish '${name}' failed" >&2
    cat "${out_root}/${name}.log" >&2
    exit 1
  }
  # ILLink reports a removal it could not apply as IL2040 rather than failing, so a stale
  # substitutions entry is only visible in the log.
  if grep -q "IL2040" "${out_root}/${name}.log"
  then
    echo "error: publish '${name}' reported IL2040, so a substitutions entry no longer matches a resource" >&2
    grep "IL2040" "${out_root}/${name}.log" >&2
    exit 1
  fi
  stat -c%s "${dir}/PhoneNumbers.dll"
}

mkdir -p "${out_root}"

default_bytes=$(publish default)
opted_out_bytes=$(publish optedout \
  -p:PhoneNumbersIncludeGeocodingData=false \
  -p:PhoneNumbersIncludeLocaleNameData=false)

echo "trimmed PhoneNumbers.dll: default ${default_bytes} bytes, opted out ${opted_out_bytes} bytes"

if [ "${default_bytes}" -lt "${min_default_bytes}" ]
then
  echo "error: the default trimmed build is only ${default_bytes} bytes (expected at least ${min_default_bytes})." >&2
  echo "       Either a data set is being dropped without being asked, or the data shrank a lot." >&2
  exit 1
fi

if [ "${opted_out_bytes}" -gt "${max_opted_out_bytes}" ]
then
  echo "error: opting out left ${opted_out_bytes} bytes (expected at most ${max_opted_out_bytes})." >&2
  echo "       The embedded data resources were not removed. Either the trimmer did not run at all" >&2
  echo "       (it needs Microsoft.NET.ILLink.Tasks resolved at restore time), or the feature names" >&2
  echo "       in ILLink.Substitutions.xml no longer match the RuntimeHostConfigurationOption items" >&2
  echo "       in buildTransitive/libphonenumber-csharp.targets, or one of them lost Trim=\"true\"." >&2
  exit 1
fi

# Size is necessary but not sufficient: the app must still work, and must report the data as gone
# rather than silently returning empty results.
default_output=$("${out_root}/default/PhoneNumbers.TrimTest")
optedout_output=$("${out_root}/optedout/PhoneNumbers.TrimTest")

for expected in "core: True +1 415-555-2671" "geocoding: present" "localenames: present"
do
  grep -qF "${expected}" <<< "${default_output}" || {
    echo "error: default trimmed build did not report '${expected}'" >&2
    printf '%s\n' "${default_output}" >&2
    exit 1
  }
done

for expected in "core: True +1 415-555-2671" "geocoding: absent" "localenames: absent"
do
  grep -qF "${expected}" <<< "${optedout_output}" || {
    echo "error: opted-out trimmed build did not report '${expected}'" >&2
    printf '%s\n' "${optedout_output}" >&2
    exit 1
  }
done

echo "trimming ok: opting out removed $((default_bytes - opted_out_bytes)) bytes, parsing still works, data reported absent"

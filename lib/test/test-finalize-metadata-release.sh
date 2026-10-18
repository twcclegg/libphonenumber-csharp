#! /bin/bash
# lib/finalize-metadata-release.sh - the half that tags a merged metadata PR, cuts the GitHub release
# and dispatches the NuGet publish.
#
# Only the validation in front of those three irreversible actions is covered here: a release, a tag
# and a push to nuget.org cannot be taken back, so what matters is that nothing malformed gets that
# far. Every test below stops before the first api call, which is also why none of them needs a
# network or a token with any rights.

finalize() { # <tag-or-branch> <commit> [token]
    runScript finalize-metadata-release.sh "$@"
}

# A valid-looking commit, since the script insists on a full 40-character sha.
COMMIT="0123456789abcdef0123456789abcdef01234567"

test_rejects_too_few_arguments() {
    finalize "v9.0.40"
    assertStatus 2
    assertOutputContains "Usage: finalize-metadata-release.sh"
}

test_rejects_too_many_arguments() {
    # A fourth argument usually means a quoting mistake in the workflow, which would otherwise be
    # read as the token.
    finalize "v9.0.40" "${COMMIT}" "token" "extra"
    assertStatus 2
}

test_requires_a_token() {
    GITHUB_TOKEN="" finalize "v9.0.40" "${COMMIT}"
    assertStatus 2
    assertOutputContains "GitHub token required"
}

test_rejects_a_tag_that_is_not_a_release_version() {
    GITHUB_TOKEN=unused finalize "main" "${COMMIT}"
    assertStatus 1
    assertOutputContains "unexpected upstream release tag: main"
}

test_rejects_a_tag_without_the_v_prefix() {
    GITHUB_TOKEN=unused finalize "9.0.40" "${COMMIT}"
    assertStatus 1
    assertOutputContains "unexpected upstream release tag: 9.0.40"
}

test_strips_the_metadata_update_branch_prefix() {
    # The workflow passes the merged PR's head ref straight through, so metadata-update/v9.0.40 has
    # to resolve to the tag v9.0.40. Proven by what it complains about next: the missing repository,
    # which is checked after the tag and commit have both been accepted.
    GITHUB_TOKEN=unused GITHUB_REPOSITORY="" finalize "metadata-update/v9.0.40" "${COMMIT}"
    assertStatus 1
    assertOutputContains "GITHUB_REPOSITORY"
    assertOutputNotContains "unexpected upstream release tag"
}

test_rejects_a_branch_prefix_around_a_bad_tag() {
    GITHUB_TOKEN=unused finalize "metadata-update/not-a-tag" "${COMMIT}"
    assertStatus 1
    assertOutputContains "unexpected upstream release tag: not-a-tag"
}

test_rejects_a_short_commit_sha() {
    # The release is pinned to a full sha so it cannot drift to whatever the branch points at later;
    # an abbreviated sha from a copy-paste must not pass.
    GITHUB_TOKEN=unused finalize "v9.0.40" "0123456"
    assertStatus 1
    assertOutputContains "unexpected release commit: 0123456"
}

test_rejects_a_commit_sha_that_is_not_hex() {
    GITHUB_TOKEN=unused finalize "v9.0.40" "Z123456789abcdef0123456789abcdef01234567"
    assertStatus 1
    assertOutputContains "unexpected release commit"
}

test_rejects_an_uppercase_commit_sha() {
    # git writes lowercase; an uppercase sha means something transformed it on the way in.
    GITHUB_TOKEN=unused finalize "v9.0.40" "0123456789ABCDEF0123456789abcdef01234567"
    assertStatus 1
    assertOutputContains "unexpected release commit"
}

test_requires_the_repository() {
    GITHUB_TOKEN=unused GITHUB_REPOSITORY="" finalize "v9.0.40" "${COMMIT}"
    assertStatus 1
    assertOutputContains "GITHUB_REPOSITORY required"
}

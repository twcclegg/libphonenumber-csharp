#!/usr/bin/env node
// Records one release in CHANGELOG.md, called from github-actions-metadata-update.sh in the same
// commit as the metadata sync. Every release always includes a metadata sync (that is the only
// thing that ever cuts a new tag - see finalize-metadata-release.sh); some releases also bundle
// other work that merged to `main` in the meantime. CHANGELOG.md was rebuilt once, by hand, to
// describe that other work accurately per release; this script only has to keep going from there
// without a human re-curating each entry.
//
// A run of consecutive metadata-only releases (no changes outside resources/) automatically folds
// into a single range entry, so the file does not grow one near-duplicate heading per fortnight
// forever - exactly like the "N releases" ranges already in the file from the historical rebuild,
// just generated instead of researched. A release that is NOT metadata-only always gets its own
// standalone entry and never gets folded into a neighboring run, in either direction: it cannot
// extend a prior run (its own content isn't metadata-only) and, because it carries no run marker
// of its own, a later metadata-only release cannot fold into it either - the next metadata-only
// release starts a brand new run instead.
//
// The fold state lives in an HTML comment immediately above the heading it describes:
//   <!-- changelog-run from=vFROM first=vFIRST start-date=YYYY-MM-DD count=N -->
// `from` is the tag right before the run started (the baseline for the compare link); `first` is
// the first tag actually in the run; `count` is how many consecutive metadata-only releases have
// folded into it so far. Only a heading with this marker directly above it is a candidate to
// extend - a human-written or pre-rebuild heading never has one, so it can never be mistaken for
// a foldable run.
//
// Usage: node lib/update-changelog.js <changelog-file> <github-repo> <upstream-repo> <from-tag>
//                                      <new-tag> <metadata-only:true|false> [date:YYYY-MM-DD]

'use strict';

const fs = require('fs');

const RUN_MARKER_RE = /^<!-- changelog-run from=(\S+) first=(\S+) start-date=(\d{4}-\d{2}-\d{2}) count=(\d+) -->$/;
const NEXT_ENTRY_MARKER = '<!-- next-entry -->';

function usageError(message) {
  process.stderr.write(`${message}\n`);
  process.stderr.write(
    'Usage: update-changelog.js <changelog-file> <github-repo> <upstream-repo> <from-tag> <new-tag> <metadata-only:true|false> [date:YYYY-MM-DD]\n',
  );
  process.exit(2);
}

function formatDateRange(startDate, endDate) {
  return startDate === endDate ? startDate : `${startDate} – ${endDate}`;
}

function compareLink(githubRepo, fromTag, toTag) {
  return `https://github.com/${githubRepo}/compare/${fromTag}...${toTag}`;
}

function upstreamReleaseLink(upstreamRepo, tag) {
  return `https://github.com/${upstreamRepo}/releases/tag/${tag}`;
}

function buildMetadataOnlyBlock({ githubRepo, upstreamRepo, from, first, startDate, count, latest, date }) {
  const heading = count === 1 ? first : `${first} – ${latest}`;
  const dateRange = formatDateRange(startDate, date);
  const link = compareLink(githubRepo, from, latest);
  const marker = `<!-- changelog-run from=${from} first=${first} start-date=${startDate} count=${count} -->`;
  const body =
    count === 1
      ? `Metadata update to upstream [libphonenumber ${latest}](${upstreamReleaseLink(upstreamRepo, latest)}).`
      : `${count} consecutive metadata-only releases (no changes to hand-written source, tests, docs, or CI/build configuration). Latest upstream sync: [libphonenumber ${latest}](${upstreamReleaseLink(upstreamRepo, latest)}).`;

  return `${marker}\n## [${heading}](${link}) - ${dateRange}\n\n${body}\n`;
}

function buildSubstantiveBlock({ githubRepo, upstreamRepo, from, tag, date }) {
  const link = compareLink(githubRepo, from, tag);
  const body = `Includes the metadata sync to upstream [libphonenumber ${tag}](${upstreamReleaseLink(upstreamRepo, tag)}) plus other changes merged to \`main\` since the last release — see the compare link above for the full diff.`;

  return `## [${tag}](${link}) - ${date}\n\n${body}\n`;
}

function main(argv) {
  const [changelogFile, githubRepo, upstreamRepo, fromTag, newTag, metadataOnlyArg, dateArg] = argv;

  if (!changelogFile || !githubRepo || !upstreamRepo || !fromTag || !newTag || !metadataOnlyArg) {
    usageError('missing required argument');
  }
  if (metadataOnlyArg !== 'true' && metadataOnlyArg !== 'false') {
    usageError(`metadata-only must be "true" or "false", got: ${metadataOnlyArg}`);
  }
  const isMetadataOnly = metadataOnlyArg === 'true';
  const date = dateArg || new Date().toISOString().slice(0, 10);

  const original = fs.readFileSync(changelogFile, 'utf8');
  const lines = original.split('\n');
  const markerIndex = lines.findIndex((line) => line.trim() === NEXT_ENTRY_MARKER);
  if (markerIndex === -1) {
    usageError(`could not find "${NEXT_ENTRY_MARKER}" in ${changelogFile}`);
  }

  // The line right after the marker (skipping one blank line, which is how every existing entry
  // in the file is spaced from the one above it) is the only place a foldable run marker can be.
  let candidateIndex = markerIndex + 1;
  if (lines[candidateIndex] === '') {
    candidateIndex += 1;
  }
  const runMatch = isMetadataOnly ? RUN_MARKER_RE.exec(lines[candidateIndex] || '') : null;

  // The block being replaced is: marker, heading, blank, then one or more body lines running up
  // to (but not including) the next blank line. Measuring it here, rather than assuming a fixed
  // length, means this still splices out exactly the right span if buildMetadataOnlyBlock's body
  // template ever grows past a single line.
  function existingBlockLineCount(markerLine) {
    let end = markerLine + 3; // marker, heading, blank -> first body line
    while (lines[end] !== undefined && lines[end] !== '') {
      end += 1;
    }
    return end - markerLine;
  }

  let newBlock;
  let replaceFrom;
  let replaceCount;

  if (runMatch) {
    const [, from, first, startDate, countStr] = runMatch;
    newBlock = buildMetadataOnlyBlock({
      githubRepo,
      upstreamRepo,
      from,
      first,
      startDate,
      count: Number(countStr) + 1,
      latest: newTag,
      date,
    });
    // Replace the existing run's marker + heading + blank + body with the extended version.
    replaceFrom = candidateIndex;
    replaceCount = existingBlockLineCount(candidateIndex);
  } else {
    newBlock = isMetadataOnly
      ? buildMetadataOnlyBlock({
          githubRepo,
          upstreamRepo,
          from: fromTag,
          first: newTag,
          startDate: date,
          count: 1,
          latest: newTag,
          date,
        })
      : buildSubstantiveBlock({ githubRepo, upstreamRepo, from: fromTag, tag: newTag, date });
    // Insert as a new block right after the marker, pushing whatever was there down.
    replaceFrom = markerIndex + 1;
    replaceCount = 0;
  }

  const newLines = newBlock.split('\n');
  // buildXBlock() always ends with a trailing '\n', so split() leaves one empty string at the
  // end - drop it so we don't introduce a stray extra blank line into the file.
  newLines.pop();

  // Only the insert path needs a blank line inserted ahead of the new block: lines.slice(0,
  // replaceFrom) stops right at the marker text itself there, with no separating blank line yet.
  // The fold path's replaceFrom already points at the existing run marker, so the blank line
  // above it (the one separating it from whatever precedes it) is already included in that slice
  // - adding another one here would double it up.
  const leadingBlank = replaceCount === 0 ? [''] : [];

  const rebuilt = [
    ...lines.slice(0, replaceFrom),
    ...leadingBlank,
    ...newLines,
    ...lines.slice(replaceFrom + replaceCount),
  ];

  fs.writeFileSync(changelogFile, rebuilt.join('\n'));
}

main(process.argv.slice(2));

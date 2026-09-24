#!/usr/bin/env bash
# Builds the API docs site. Run from anywhere; paths are resolved relative to this script.
#
# docfx's TOC resolver needs conceptual markdown to live inside the docfx project tree, so
# this copies docs/*.md (the single source of truth) into docfx/articles/ before invoking
# docfx. The copies are gitignored (see /docfx/articles/*.md in the repo .gitignore) and
# regenerated on every build.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"

cp ../docs/*.md articles/

# Every docs/*.md has to be reachable from articles/toc.yml, which is hand-maintained -
# docfx happily builds a page that nothing links to, so a new doc would otherwise be
# published as an orphan that only shows up if you guess its URL. Fail loudly instead.
for f in ../docs/*.md; do
    name=$(basename "$f")
    if ! grep -q "href: $name" articles/toc.yml; then
        echo "error: docs/$name is not listed in docfx/articles/toc.yml - add it there." >&2
        exit 1
    fi
done

# Links like "](../csharp/PhoneNumbers/Foo.cs)" resolve on GitHub (where the file renders
# in its own repo location) but point nowhere in the published static site, which ships
# only the rendered articles. Rewrite any repo-relative link - not just ../csharp/ - to
# permalink at GitHub instead. Runs on the copies in articles/, never on docs/ itself.
# Redirect through a temp file rather than sed -i: GNU sed wants "-i" and BSD/macOS sed
# wants "-i ''", and this script is the documented way to build the site locally.
for f in articles/*.md; do
    sed 's#](\.\./#](https://github.com/twcclegg/libphonenumber-csharp/blob/main/#g' "$f" > "$f.tmp"
    mv "$f.tmp" "$f"
done

# docfx loads both csproj files through MSBuild to extract API metadata, which needs a
# restored project.assets.json - this repo uses Central Package Management, so an unrestored
# project fails to resolve references rather than just producing incomplete docs. Restoring
# and building here rather than in each caller keeps every entry point - docs_preview.yml,
# deploy-demo.yml and a local run - working from the same state, so a green preview really
# does predict a green deploy. Building PhoneNumbers.Extensions builds its PhoneNumbers
# project reference too, which covers both documented projects.
dotnet restore ../csharp/PhoneNumbers.Extensions
dotnet build ../csharp/PhoneNumbers.Extensions --no-restore

dotnet tool restore
dotnet docfx docfx.json "$@"

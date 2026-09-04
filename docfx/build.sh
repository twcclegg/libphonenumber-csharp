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
# Links like "](../csharp/PhoneNumbers/Foo.cs)" resolve on GitHub (where the file renders
# in its own repo location) but point nowhere in the published static site, which never
# ships the .cs sources. Rewrite them to permalink at GitHub instead.
sed -i 's#](\.\./csharp/#](https://github.com/twcclegg/libphonenumber-csharp/blob/main/csharp/#g' articles/*.md

dotnet tool restore
dotnet docfx docfx.json "$@"

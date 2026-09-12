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

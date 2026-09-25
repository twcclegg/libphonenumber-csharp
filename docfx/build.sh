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

# The docs sidebar repeats the demo's page links as static markup in template/layout/_master.tmpl,
# and nothing else keeps that in step with the demo's MainLayout page table - so a new demo page
# would be missing from the docs sidebar, and a removed one would stay linked to a 404. Compare
# the two route lists (the demo's Home route is "", which the template writes as a bare "../").
demo_routes=$(grep -oE 'new\("[^"]*", "' ../csharp/PhoneNumbers.Demo/Layout/MainLayout.razor | sed -E 's/new\("([^"]*)".*/\1/' | sort -u)
docs_routes=$(grep -oE 'href="\{\{_rel\}\}\.\./[^"]*"' template/layout/_master.tmpl | sed -E 's/.*\.\.\/([^"]*)"/\1/' | sort -u)
if [ "$demo_routes" != "$docs_routes" ]; then
    echo "error: the docs sidebar (docfx/template/layout/_master.tmpl) links different demo pages from" >&2
    echo "the demo's own (Pages in csharp/PhoneNumbers.Demo/Layout/MainLayout.razor):" >&2
    diff <(echo "$demo_routes") <(echo "$docs_routes") | sed 's/^</  demo only:/; s/^>/  docs only:/' | grep only >&2
    exit 1
fi

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

# The demo links into this site by page and DocFX heading id (csharp/PhoneNumbers.Demo/
# DocsLinks.cs), and main.js adds links back from the same kind of key. docfx validates neither,
# so check each against the built site: a renamed type or a changed overload fails the build here
# instead of shipping a link to a page or anchor that no longer exists.
broken=0
while IFS= read -r link; do
    page=${link%%#*}
    anchor=${link#"$page"}
    anchor=${anchor#\#}
    if [ ! -f "_site/$page" ]; then
        echo "error: $link - _site/$page does not exist." >&2
        broken=1
    elif [ -n "$anchor" ] && ! grep -q "id=\"$anchor\"" "_site/$page"; then
        echo "error: $link - _site/$page has no heading with that id." >&2
        broken=1
    fi
done < <(grep -ohE "[\"'](api|articles)/[^\"'#]+\.html(#[^\"']*)?[\"']" \
    ../csharp/PhoneNumbers.Demo/DocsLinks.cs template/public/main.js | tr -d "\"'" | sort -u)
if [ "$broken" -ne 0 ]; then
    echo "Update the links in csharp/PhoneNumbers.Demo/DocsLinks.cs or docfx/template/public/main.js." >&2
    exit 1
fi

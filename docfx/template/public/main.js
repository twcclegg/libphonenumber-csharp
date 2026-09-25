// Loaded by DocFX's modern template as an ES module (docfx.min.js does
// `import("./main.js").then(m => m.default)`), so the default export below is
// required; DocFX calls its start() once, before it renders the navigation.

// "Try it live" links from the API reference into the demo page that exercises each API: the
// reverse of the demo's own page-header links (csharp/PhoneNumbers.Demo/DocsLinks.cs). Each key
// is a page of this site, with DocFX's heading id for a member; a key without one goes on the
// type's title. docfx/build.sh fails the build when a page or heading here stops existing.
const TRY_IT_LIVE = {
    'api/PhoneNumbers.PhoneNumberUtil.html#PhoneNumbers_PhoneNumberUtil_Parse_System_String_System_String_':
        { route: 'parse', page: 'Parse & Validate' },
    'api/PhoneNumbers.PhoneNumberUtil.html#PhoneNumbers_PhoneNumberUtil_Format_PhoneNumbers_PhoneNumber_PhoneNumbers_PhoneNumberFormat_':
        { route: 'format', page: 'Formatting' },
    'api/PhoneNumbers.AsYouTypeFormatter.html':
        { route: 'live', page: 'Live Formatter' },
    'api/PhoneNumbers.PhoneNumberUtil.html#PhoneNumbers_PhoneNumberUtil_FindNumbers_System_String_System_String_PhoneNumbers_PhoneNumberUtil_Leniency_System_Int64_':
        { route: 'find', page: 'Find Numbers' },
    'api/PhoneNumbers.PhoneNumberOfflineGeocoder.html':
        { route: 'geo', page: 'Geo & Timezone' },
    'api/PhoneNumbers.PhoneNumberToTimeZonesMapper.html':
        { route: 'geo', page: 'Geo & Timezone' },
    'api/PhoneNumbers.PhoneNumberToCarrierMapper.html':
        { route: 'geo', page: 'Geo & Timezone' },
};

function addTryItLiveLinks() {
    // The sidebar's Home link is the demo's root (template/layout/_master.tmpl), resolved
    // against wherever this page sits.
    const demoRoot = document.querySelector('.rail-link--home')?.href;
    if (!demoRoot) return;

    for (const [key, { route, page }] of Object.entries(TRY_IT_LIVE)) {
        const [path, id] = key.split('#');
        if (!location.pathname.endsWith('/' + path)) continue;

        const heading = id ? document.getElementById(id) : document.querySelector('article h1');
        if (!heading) continue;

        const link = document.createElement('a');
        link.className = 'try-live__link';
        link.href = new URL(route, demoRoot).href;
        link.textContent = `Try it live: ${page}`;

        const paragraph = document.createElement('p');
        paragraph.className = 'try-live';
        paragraph.append(link);
        heading.after(paragraph);
    }
}

export default {
    start: addTryItLiveLinks,
};

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

// The collapsible sidebar. The state lives on <html data-sidebar="collapsed"> (set before
// first paint by template/layout/_master.tmpl) and in localStorage under 'sidebar', the key
// the demo uses too, so the choice carries between the two sites. The CSS only acts on it at
// desktop widths; below that the sidebar is DocFX's own collapsing menu.
const desktop = window.matchMedia('(min-width: 768px)');

function isSidebarCollapsed() {
    return document.documentElement.getAttribute('data-sidebar') === 'collapsed';
}

function setSidebarCollapsed(collapsed) {
    if (collapsed) document.documentElement.setAttribute('data-sidebar', 'collapsed');
    else document.documentElement.removeAttribute('data-sidebar');
    try {
        localStorage.setItem('sidebar', collapsed ? 'collapsed' : 'expanded');
    } catch {
        // Storage can be unavailable (private mode, blocked site data); the toggle still works.
    }
    labelSidebarToggle();
}

function labelSidebarToggle() {
    const label = document.querySelector('.rail-collapse__label');
    if (label) label.textContent = isSidebarCollapsed() ? 'Expand sidebar' : 'Collapse sidebar';
}

function setUpSidebar() {
    labelSidebarToggle();
    document.querySelector('.rail-collapse')
        ?.addEventListener('click', () => setSidebarCollapsed(!isSidebarCollapsed()));

    // Search needs the full width to type into and to read its results beside, so focusing it
    // from the collapsed sidebar (clicking its icon, or tabbing to it) expands the sidebar.
    document.getElementById('search-query')?.addEventListener('focus', () => {
        if (desktop.matches && isSidebarCollapsed()) setSidebarCollapsed(false);
    });

    addSidebarTooltips();
}

// The collapsed sidebar shows icons only, so name each control beside it on hover and on
// keyboard focus. The name is still the control's own text (hidden visually), so the tooltip
// is aria-hidden; it stays up while the pointer moves onto it and Escape dismisses it (WCAG
// 1.4.13). Controls with a title (the theme picker) keep the browser's own tooltip, and the
// theme menu's items are skipped. The demo carries the same behaviour in its index.html.
function addSidebarTooltips() {
    const tip = document.createElement('div');
    tip.className = 'rail-tooltip';
    tip.setAttribute('aria-hidden', 'true');
    let owner = null;
    let hideTimer = 0;

    const controlFor = node => {
        if (!(node instanceof Element) || !desktop.matches || !isSidebarCollapsed()) return null;
        const control = node.closest('header a, header button');
        return control && !control.hasAttribute('title') && !control.closest('.dropdown-menu') ? control : null;
    };
    const show = control => {
        clearTimeout(hideTimer);
        const label = control.querySelector('.rail-collapse__label');
        const text = (label ?? control).textContent.replace(/\s+/g, ' ').trim();
        if (!text) return;
        if (!tip.isConnected) document.body.append(tip);
        tip.textContent = text;
        const rail = control.closest('header').getBoundingClientRect();
        const box = control.getBoundingClientRect();
        tip.style.left = `${rail.right + 8}px`;
        tip.style.top = `${box.top + box.height / 2}px`;
        tip.classList.add('rail-tooltip--visible');
        owner = control;
    };
    const hide = () => {
        clearTimeout(hideTimer);
        tip.classList.remove('rail-tooltip--visible');
        owner = null;
    };

    document.addEventListener('pointerover', e => {
        const control = controlFor(e.target);
        if (control) show(control);
        else if (tip.contains(e.target)) clearTimeout(hideTimer);
        else if (owner) {
            clearTimeout(hideTimer);
            hideTimer = setTimeout(hide, 300);
        }
    });
    document.addEventListener('focusin', e => {
        const control = controlFor(e.target);
        if (control) show(control); else hide();
    });
    document.addEventListener('focusout', hide);
    document.addEventListener('click', hide);
    document.addEventListener('keydown', e => { if (e.key === 'Escape') hide(); });
    window.addEventListener('scroll', hide, true);
}

export default {
    start() {
        setUpSidebar();
        addTryItLiveLinks();
    },
};

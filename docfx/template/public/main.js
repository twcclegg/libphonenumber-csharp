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
// 1.4.13). Controls with a title (the theme button) keep the browser's own tooltip. The demo carries the same behaviour in its index.html.
function addSidebarTooltips() {
    const tip = document.createElement('div');
    tip.className = 'rail-tooltip';
    tip.setAttribute('aria-hidden', 'true');
    let owner = null;
    let hideTimer = 0;

    const controlFor = node => {
        if (!(node instanceof Element) || !desktop.matches || !isSidebarCollapsed()) return null;
        const control = node.closest('header a, header button');
        return control && !control.hasAttribute('title') ? control : null;
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

// The theme button in the sidebar header, the demo's own: it flips between light and dark
// and stores the choice under 'theme', the key DocFX (and the demo) read before first paint.
// DocFX's 'auto' still resolves to the OS setting until the reader presses it. The icon
// follows data-bs-theme in CSS; the name, which is also the tooltip, is kept in step here,
// including when 'auto' follows an OS change.
function currentTheme() {
    return document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light';
}

function setUpThemeToggle() {
    const button = document.querySelector('.rail-theme-toggle');
    if (!button) return;

    const label = () => {
        const text = currentTheme() === 'dark' ? 'Switch to light mode' : 'Switch to dark mode';
        button.setAttribute('aria-label', text);
        button.title = text;
    };
    label();
    new MutationObserver(label).observe(document.documentElement, { attributeFilter: ['data-bs-theme'] });

    button.addEventListener('click', () => {
        const theme = currentTheme() === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-bs-theme', theme);
        try {
            localStorage.setItem('theme', theme);
        } catch {
            // Storage can be unavailable; the page still switches.
        }
    });
}

// The share button in the sidebar header, the demo's own: copies this page's address and
// confirms with a check and "Link copied" for 1.5s. A second press restarts the confirmation.
// Falls back to a hidden textarea where the async clipboard is unavailable, as the demo does.
const COPY_CONFIRMATION_MS = 1500;

async function copyText(text) {
    try {
        await navigator.clipboard.writeText(text);
    } catch {
        const area = document.createElement('textarea');
        area.value = text;
        area.style.position = 'fixed';
        area.style.opacity = '0';
        document.body.append(area);
        area.select();
        try {
            document.execCommand('copy');
        } finally {
            area.remove();
        }
    }
}

function setUpShareButton() {
    const button = document.querySelector('.rail-share');
    if (!button) return;

    let timer = 0;
    const label = copied => {
        const text = copied ? 'Link copied' : 'Copy shareable link';
        button.setAttribute('aria-label', text);
        button.title = text;
        button.classList.toggle('rail-icon-btn--done', copied);
    };

    button.addEventListener('click', async () => {
        await copyText(location.href);
        label(true);
        clearTimeout(timer);
        timer = setTimeout(() => label(false), COPY_CONFIRMATION_MS);
    });
}

export default {
    start() {
        setUpShareButton();
        setUpThemeToggle();
        setUpSidebar();
        addTryItLiveLinks();
    },
};

// Loaded by DocFX's modern template as an ES module (docfx.min.js does
// `import("./main.js").then(m => m.default)`), so the default export below is
// required even though this file only adds behaviour.

// Copy-to-clipboard for .install-block. Delegated from the document rather than
// bound per button: the template renders page content client side, so nodes bound
// on DOMContentLoaded would be replaced on the next navigation.
const RESET_MS = 1600;
const IDLE_LABEL = 'Copy install command';

document.addEventListener('click', async event => {
    const button = event.target.closest('.install-block__copy');
    if (!button) return;

    event.preventDefault();
    const command = button.closest('.install-block')?.querySelector('.install-block__command')?.textContent;
    if (!command) return;

    try {
        await navigator.clipboard.writeText(command);
    } catch {
        // Clipboard access can be denied (insecure origin, permissions policy).
        // Leave the button in its idle state rather than claiming a copy happened.
        return;
    }

    button.classList.add('install-block__copy--copied');
    button.setAttribute('aria-label', 'Copied');
    setTimeout(() => {
        button.classList.remove('install-block__copy--copied');
        button.setAttribute('aria-label', IDLE_LABEL);
    }, RESET_MS);
});

export default {};

// Kopierar text till urklipp. Använder Clipboard API när det är tillgängligt och tillåtet,
// annars en reservmetod (execCommand) som fungerar i fler miljöer (äldre webbläsare, inbäddade vyer).
window.devteam = window.devteam || {};

window.devteam.copyText = async function (text) {
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
            return true;
        }
    } catch (e) {
        // Behörighet nekad eller sidan saknar fokus – prova reservmetoden.
    }

    const textarea = document.createElement("textarea");
    textarea.value = text;
    textarea.setAttribute("readonly", "");
    textarea.style.position = "fixed";
    textarea.style.top = "0";
    textarea.style.opacity = "0";
    document.body.appendChild(textarea);
    textarea.select();

    let ok = false;
    try {
        ok = document.execCommand("copy");
    } catch (e) {
        ok = false;
    }

    document.body.removeChild(textarea);
    return ok;
};

// Språkval: sparas i localStorage och används vid uppstart (Blazor.start -> applicationCulture).
// Körs före Blazor så att rätt kultur och satellit-assemblies laddas direkt.
(function () {
    var STORAGE_KEY = "devteam.culture";
    var SUPPORTED = ["sv", "en"];
    var DESCRIPTIONS = {
        sv: "DevTeam AI i Stockholm: Adel Kobansson bygger och levererar din applikation från grunden med modern .NET och AI. Du beskriver idén – vi tar den till produktion.",
        en: "DevTeam AI in Stockholm: Adel Kobansson builds and delivers your application from the ground up with modern .NET and AI. You describe the idea – we take it to production."
    };

    function readStored() {
        try {
            var stored = localStorage.getItem(STORAGE_KEY);
            return SUPPORTED.indexOf(stored) !== -1 ? stored : null;
        } catch (e) {
            return null; // localStorage kan vara blockerat (t.ex. privat läge)
        }
    }

    // 1) Sparat val, 2) webbläsarens språk (svenska -> sv), 3) engelska
    function detect() {
        var stored = readStored();
        if (stored) return stored;
        var browser = ((navigator.languages && navigator.languages[0]) || navigator.language || "").toLowerCase();
        return browser.indexOf("sv") === 0 ? "sv" : "en";
    }

    function applyToDocument(lang) {
        document.documentElement.lang = lang;
        var meta = document.querySelector('meta[name="description"]');
        if (meta) meta.setAttribute("content", DESCRIPTIONS[lang]);
    }

    window.devteam = window.devteam || {};

    window.devteam.getLanguage = detect;

    window.devteam.setLanguage = function (lang) {
        if (SUPPORTED.indexOf(lang) === -1) return;
        try {
            localStorage.setItem(STORAGE_KEY, lang);
        } catch (e) {
            // Valet gäller då bara tills sidan laddas om.
        }
        applyToDocument(lang);
    };

    applyToDocument(detect());
})();

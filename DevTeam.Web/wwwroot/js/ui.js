// Små UI-hjälpare för Blazor-komponenter (anropas via IJSRuntime som "devteam.*")
// samt två rena CSS-förstärkare: scroll-reveal och musföljande ljus på kort.
(function () {
    window.devteam = window.devteam || {};

    // Scrollar ett element (t.ex. chattloggen) längst ner.
    window.devteam.scrollToBottom = function (element) {
        if (element) {
            element.scrollTop = element.scrollHeight;
        }
    };

    // Scrollar mjukt till ett element via id. CSS-regeln scroll-margin-top (.anchor-section) håller rubriken fri från den fasta menyn.
    window.devteam.scrollToId = function (id) {
        var element = document.getElementById(id);
        if (!element) return false;
        var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        element.scrollIntoView({ behavior: reduceMotion ? 'auto' : 'smooth', block: 'start' });
        return true;
    };

    // Flyttar tangentbordsfokus till ett element utan att hoppa i sidan (scrollningen sköts separat).
    window.devteam.focusId = function (id) {
        var element = document.getElementById(id);
        if (element) element.focus({ preventScroll: true });
    };

    // Sätter iframe.srcdoc utan att HTML:en går via ett attribut (stora dokument, citattecken).
    window.devteam.setSrcdoc = function (iframe, html) {
        if (iframe) iframe.srcdoc = html || "";
    };

    // Sparar text som en fil i nedladdningsmappen. Används för prototypens HTML.
    window.devteam.downloadText = function (filename, text) {
        var blob = new Blob([text || ""], { type: "text/html;charset=utf-8" });
        var url = URL.createObjectURL(blob);
        var link = document.createElement("a");
        link.href = url;
        link.download = filename || "prototyp.html";
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(function () { URL.revokeObjectURL(url); }, 1500);
    };

    // Helskärm av förhandsvisningsramen. Ett nytt anrop, eller Esc, lämnar helskärm.
    window.devteam.toggleFullscreen = function (element) {
        if (!element) return;
        if (document.fullscreenElement === element) {
            document.exitFullscreen().catch(function () { });
        } else if (element.requestFullscreen) {
            element.requestFullscreen().catch(function () { });
        }
    };

    // ---- Scroll-reveal ------------------------------------------------------------------------
    // Element med attributet data-reveal tonas in när de scrollas fram (se tailwind.css, "Scroll-reveal").
    // Blazor skapar element efter att sidan laddats, så en MutationObserver fångar upp nya [data-reveal]-element.
    // Vi använder ett attribut (data-revealed) i stället för en klass: Blazor kan skriva om class-attributet
    // vid omrendering men rör inte attribut som den inte själv har satt.
    function initReveal() {
        var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (!('IntersectionObserver' in window) || reduceMotion) {
            return; // Utan "reveal-ready" visas allt direkt.
        }

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.setAttribute('data-revealed', '');
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1, rootMargin: '0px 0px -6% 0px' });

        function observe(root) {
            if (root.nodeType !== 1) return;
            if (root.hasAttribute('data-reveal') && !root.hasAttribute('data-revealed')) {
                observer.observe(root);
            }
            root.querySelectorAll('[data-reveal]:not([data-revealed])').forEach(function (el) {
                observer.observe(el);
            });
        }

        document.documentElement.classList.add('reveal-ready');
        observe(document.body);

        new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(observe);
            });
        }).observe(document.body, { childList: true, subtree: true });
    }

    // ---- Musföljande ljus på kort ---------------------------------------------------------------
    // Sätter --mx/--my (i px relativt kortet) som .glass-card använder i sin hover-bakgrund.
    function initSpotlight() {
        if (!window.matchMedia || !window.matchMedia('(hover: hover) and (pointer: fine)').matches) return;

        var frame = 0;
        document.addEventListener('pointermove', function (event) {
            if (frame) return;
            var x = event.clientX, y = event.clientY, target = event.target;
            frame = requestAnimationFrame(function () {
                frame = 0;
                var card = target && target.closest ? target.closest('.glass-card') : null;
                if (!card) return;
                var rect = card.getBoundingClientRect();
                card.style.setProperty('--mx', (x - rect.left) + 'px');
                card.style.setProperty('--my', (y - rect.top) + 'px');
            });
        }, { passive: true });
    }

    initReveal();
    initSpotlight();
})();

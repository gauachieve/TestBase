// Generisk fane-switcher KOMBINERT med søkefilter (se tabellfilter.js for det
// enklere, fanefrie tilfellet): en <div data-fane-beholder> inneholder
// fane-knapper (<button data-fane="navn">), et valgfritt søkefelt
// (<input data-fane-sok>), og rader (<tr data-fane-rad="navn" data-sok="...">).
// En rad vises kun når BÅDE aktiv fane matcher OG søketeksten matcher — bytte
// av fane beholder altså gjeldende søk, jf. kravet "filtrer på alle faner samtidig".
(function () {
    document.querySelectorAll('[data-fane-beholder]').forEach(function (beholder) {
        var faneKnapper = beholder.querySelectorAll('[data-fane]');
        var sokInput = beholder.querySelector('[data-fane-sok]');
        var rader = beholder.querySelectorAll('[data-fane-rad]');
        if (faneKnapper.length === 0) {
            return;
        }

        // Bugliste 2026-10-06 punkt 12: en handling (f.eks. "slett ubesvart tildeling") som
        // POSTer og redirecter tilbake til siden skal kunne holde brukeren på SAMME fane i
        // stedet for å alltid hoppe tilbake til den første — serveren kan derfor legge ved
        // f.eks. "#ikke-besvart" på redirect-URL-en, lest her ved lasting.
        var faneFraUrl = (window.location.hash || '').replace('#', '');
        var faneFinnesHer = Array.prototype.some.call(faneKnapper, function (k) { return k.getAttribute('data-fane') === faneFraUrl; });
        var aktivFane = faneFinnesHer ? faneFraUrl : faneKnapper[0].getAttribute('data-fane');

        function oppdater() {
            var sok = (sokInput && sokInput.value || '').trim().toLowerCase();
            rader.forEach(function (rad) {
                var matcherFane = rad.getAttribute('data-fane-rad') === aktivFane;
                var tekst = (rad.getAttribute('data-sok') || '').toLowerCase();
                var matcherSok = sok.length === 0 || tekst.indexOf(sok) !== -1;
                rad.hidden = !(matcherFane && matcherSok);
            });
            faneKnapper.forEach(function (knapp) {
                var erAktiv = knapp.getAttribute('data-fane') === aktivFane;
                knapp.classList.toggle('fane-aktiv', erAktiv);
                knapp.setAttribute('aria-selected', erAktiv ? 'true' : 'false');
            });
        }

        faneKnapper.forEach(function (knapp) {
            knapp.addEventListener('click', function (e) {
                e.preventDefault();
                aktivFane = knapp.getAttribute('data-fane');
                oppdater();
            });
        });
        if (sokInput) {
            sokInput.addEventListener('input', oppdater);
        }

        oppdater();
    });
})();

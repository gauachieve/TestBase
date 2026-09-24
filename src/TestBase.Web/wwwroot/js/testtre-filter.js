// Søkefilter for kategori-treet ved testvalg (Tildel/Tester, se
// Behandlerportal+Admin sine sidepar): et <input data-tredfilter="#treId">
// skjuler <li data-sok="..."> som ikke matcher søketeksten, og skjuler i
// tillegg en hel <details>-kategori når INGEN av dens tester lenger er
// synlige (så tomme kategorier ikke roter til resultatet) — samt tvinger
// kategorien åpen mens et søk er aktivt, siden brukeren ellers må åpne hver
// <details> manuelt for å se treffene. Checkboxene ligger fortsatt i DOM-en
// selv når raden er skjult, så et already-checked valg overlever et søk.
(function () {
    document.querySelectorAll('[data-tredfilter]').forEach(function (input) {
        var tre = document.querySelector(input.getAttribute('data-tredfilter'));
        if (!tre) {
            return;
        }

        var rader = tre.querySelectorAll('li[data-sok]');
        var kategorier = tre.querySelectorAll('details');

        input.addEventListener('input', function () {
            var sok = input.value.trim().toLowerCase();
            rader.forEach(function (li) {
                var tekst = (li.getAttribute('data-sok') || '').toLowerCase();
                li.hidden = sok.length > 0 && tekst.indexOf(sok) === -1;
            });
            kategorier.forEach(function (det) {
                var harSynlig = Array.prototype.some.call(det.querySelectorAll('li[data-sok]'), function (li) {
                    return !li.hidden;
                });
                det.hidden = sok.length > 0 && !harSynlig;
                if (sok.length > 0 && harSynlig) {
                    det.open = true;
                }
            });
        });
    });
})();

// Generisk klientside filter/søk for tabeller: et <input data-tabellfilter="#tabellId">
// skjuler/viser <tr data-sok="..."> i den tilhørende tabellen basert på om søketeksten
// finnes i radens data-sok-attributt (som igjen inneholder alle feltene man vil kunne
// søke i, satt server-side — se Behandlerportal/Pasienter/Index.cshtml m.fl.). Rent
// DOM-triks, ingen server-tur — matcher/filtrerer live mens man skriver.
(function () {
    document.querySelectorAll('[data-tabellfilter]').forEach(function (input) {
        var tabell = document.querySelector(input.getAttribute('data-tabellfilter'));
        if (!tabell) {
            return;
        }

        var rader = tabell.querySelectorAll('tbody tr');
        input.addEventListener('input', function () {
            var sok = input.value.trim().toLowerCase();
            rader.forEach(function (rad) {
                var tekst = (rad.getAttribute('data-sok') || '').toLowerCase();
                rad.hidden = sok.length > 0 && tekst.indexOf(sok) === -1;
            });
        });
    });
})();

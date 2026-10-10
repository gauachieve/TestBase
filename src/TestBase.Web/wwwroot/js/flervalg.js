// Generisk "velg flere rader + verktøylinje med handlinger"-mønster: en
// <div data-flervalg-beholder> inneholder en valgfri "velg alle"-avkrysning
// (<input data-flervalg-alle>), rad-avkrysninger (<input data-flervalg-rad>,
// delt name= bindes til en long[]-parameter server-side, samme konvensjon
// som ellers i appen — se Admin/MinSide.cshtml sin "Godkjenn valgte"), og en
// verktøylinje (<div data-flervalg-verktoylinje>) med handlingsknapper som er
// deaktivert og skjult til minst én rad er valgt. En teller
// (<span data-flervalg-antall>) oppdateres live. "Velg alle" hopper bevisst
// over rader som er skjult av faner.js/tabellfilter.js (filtrert bort),
// slik at "velg alle" betyr "velg alle SYNLIGE", ikke "velg alle i DOM-en".
(function () {
    document.querySelectorAll('[data-flervalg-beholder]').forEach(function (beholder) {
        var radAvkrysninger = beholder.querySelectorAll('[data-flervalg-rad]');
        var alleAvkrysning = beholder.querySelector('[data-flervalg-alle]');
        var verktoylinje = beholder.querySelector('[data-flervalg-verktoylinje]');
        var antallSpenn = beholder.querySelector('[data-flervalg-antall]');
        if (radAvkrysninger.length === 0 || !verktoylinje) {
            return;
        }

        var handlingsKnapper = verktoylinje.querySelectorAll('button[type="submit"], input[type="submit"]');

        function erRadSynlig(avkrysning) {
            var rad = avkrysning.closest('tr');
            return !rad || !rad.hidden;
        }

        function oppdater() {
            var antallValgt = Array.prototype.filter.call(radAvkrysninger, function (r) { return r.checked; }).length;
            handlingsKnapper.forEach(function (knapp) { knapp.disabled = antallValgt === 0; });
            verktoylinje.hidden = antallValgt === 0;
            if (antallSpenn) {
                antallSpenn.textContent = antallValgt;
            }
            if (alleAvkrysning) {
                var synlige = Array.prototype.filter.call(radAvkrysninger, erRadSynlig);
                var antallSynligeValgt = synlige.filter(function (r) { return r.checked; }).length;
                alleAvkrysning.checked = synlige.length > 0 && antallSynligeValgt === synlige.length;
                alleAvkrysning.indeterminate = antallSynligeValgt > 0 && antallSynligeValgt < synlige.length;
            }
        }

        radAvkrysninger.forEach(function (r) {
            r.addEventListener('change', oppdater);
        });
        if (alleAvkrysning) {
            alleAvkrysning.addEventListener('change', function () {
                radAvkrysninger.forEach(function (r) {
                    if (erRadSynlig(r)) {
                        r.checked = alleAvkrysning.checked;
                    }
                });
                oppdater();
            });
        }

        oppdater();
    });
})();

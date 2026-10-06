// Bugliste 2026-10-06 punkt 18: "alignment of table columns should be centered for all but text
// fields like names (numbers and dates)". I stedet for å måtte merke hver eneste kolonne i hver
// eneste tabell manuelt (dusinvis av Index-sider på tvers av alle tre Areas), kjører dette
// GENERISKE skriptet på ALLE tabeller i hele appen (inkludert via _Layout.cshtml): for hver
// kolonne, se på INNHOLDET i cellene — hvis alt som faktisk står der (tall/dato/klokkeslett/
// valuta/prosent, pluss nøytrale fyll-tekster som "-") ser tallaktig/datoaktig ut, senter-
// justeres HELE kolonnen (header + celler). En kolonne som inneholder knapper/lenker/skjema
// (handlingskolonnen) røres ALDRI — den har sin egen flex-/ikon-styling fra før.
(function () {
    var TALL_MONSTER = [
        /^-?\d+([.,]\d+)?\s?%?$/, // rene tall, desimaltall (komma ELLER punktum), valgfri %
        /^-?\d+([.,]\d+)?\s?(kr|nok)$/i, // beløp
        /^\d{1,2}\.\d{1,2}\.\d{4}$/, // dd.mm.yyyy
        /^\d{1,2}\.\d{1,2}\.\d{4}\s+\d{1,2}:\d{2}(:\d{2})?$/, // dd.mm.yyyy HH:mm (.ToString("g")/("G"))
        /^\d{4}-\d{2}-\d{2}$/, // ISO-dato
        /^\d{1,2}:\d{2}(:\d{2})?$/, // klokkeslett alene
        /^\d+\s*\/\s*\d+$/ // "4/8"-brøk-aktige råskår
    ];
    var NOYTRAL = ['-', '–', ''];

    function erTallaktig(tekst) {
        var t = tekst.trim();
        if (NOYTRAL.indexOf(t) !== -1) {
            return true;
        }
        return TALL_MONSTER.some(function (m) { return m.test(t); });
    }

    document.querySelectorAll('main.page table').forEach(function (tabell) {
        var overskriftRad = tabell.querySelector('thead tr');
        var kroppRader = tabell.querySelectorAll('tbody tr');
        if (!overskriftRad || kroppRader.length === 0) {
            return;
        }
        // En colspan forskyver DOM-indeksen til alle celler etter den i SAMME rad, slik at
        // ren posisjonsbasert kolonne-sammenligning blir feil når ulike rader i samme tabell har
        // ulikt antall faktiske <td>-elementer (f.eks. Hjemmeoppgaver/Programmer sine "Personlig"-
        // rader med colspan="3" på navnecellen, blandet med rader uten colspan) — tryggest å la
        // en slik tabell stå urørt enn å risikere å senterjustere feil kolonne.
        var harColspan = Array.prototype.some.call(tabell.querySelectorAll('td, th'), function (c) {
            return c.hasAttribute('colspan');
        });
        if (harColspan) {
            return;
        }

        var kolonneAntall = overskriftRad.children.length;
        for (var i = 0; i < kolonneAntall; i++) {
            var harInnhold = false;
            var alleTallaktige = true;
            var harHandlingselement = false;

            kroppRader.forEach(function (rad) {
                var celle = rad.children[i];
                if (!celle || celle.hasAttribute('colspan')) {
                    return;
                }
                if (celle.querySelector('button, a, form, input')) {
                    harHandlingselement = true;
                    return;
                }
                var tekst = (celle.textContent || '').trim();
                if (tekst.length === 0) {
                    return;
                }
                harInnhold = true;
                if (!erTallaktig(tekst)) {
                    alleTallaktige = false;
                }
            });

            if (harHandlingselement || !harInnhold || !alleTallaktige) {
                continue;
            }

            var th = overskriftRad.children[i];
            if (th) {
                th.style.textAlign = 'center';
            }
            kroppRader.forEach(function (rad) {
                var celle = rad.children[i];
                if (celle && !celle.hasAttribute('colspan')) {
                    celle.style.textAlign = 'center';
                }
            });
        }
    });
})();

// Rapportvisning (Behandlerportal/Pasienter/Rapport og Pasientportal/Tester/Rapport):
// sideflipping mellom .rapport-ark-elementene, kopiering til utklippstavle, og
// utskrift. Rene DOM-triks — ingen server-tur for noen av delene her.
(function () {
    var ark = document.querySelectorAll('.rapport-ark');
    var indeks = 0;
    var indikator = document.getElementById('rapportSideindikator');
    var forrigeKnapp = document.getElementById('rapportForrige');
    var nesteKnapp = document.getElementById('rapportNeste');

    function visSide(i) {
        ark.forEach(function (side, idx) {
            side.hidden = idx !== i;
        });
        if (indikator) {
            indikator.textContent = 'Side ' + (i + 1) + ' av ' + ark.length;
        }
        if (forrigeKnapp) {
            forrigeKnapp.disabled = i === 0;
        }
        if (nesteKnapp) {
            nesteKnapp.disabled = i === ark.length - 1;
        }
    }

    if (ark.length > 1) {
        visSide(0);
        forrigeKnapp?.addEventListener('click', function () {
            if (indeks > 0) {
                indeks--;
                visSide(indeks);
            }
        });
        nesteKnapp?.addEventListener('click', function () {
            if (indeks < ark.length - 1) {
                indeks++;
                visSide(indeks);
            }
        });
    } else {
        document.getElementById('rapportVerktoylinje')?.setAttribute('hidden', '');
    }

    // #rapportKopierMal er en SKJULT (hidden), inline-stylet mal separat fra selve
    // sidevisningen (som er stylet via eksterne CSS-klasser journalsystemer ikke ser).
    // #rapportKopierResultat er en adresserbar underboks INNI den malen — "Kopier resultat"
    // henter kun den, "Kopier alt" henter hele malen (og får dermed resultat-boksen med som
    // en del av helheten). Merk: kildeelementet er skjult, så .innerText ville gitt tom
    // streng (kun rendret, synlig tekst telles) — .textContent brukes derfor for tekstfallbacken.
    async function kopierTilUtklippstavle(elementId) {
        var innhold = document.getElementById(elementId);
        var status = document.getElementById('rapportKopierStatus');
        if (!innhold) {
            return;
        }

        var tekst = innhold.textContent.replace(/\n\s*\n+/g, '\n\n').trim();
        var html = innhold.innerHTML;

        try {
            if (navigator.clipboard && window.ClipboardItem) {
                await navigator.clipboard.write([
                    new ClipboardItem({
                        'text/plain': new Blob([tekst], { type: 'text/plain' }),
                        'text/html': new Blob([html], { type: 'text/html' })
                    })
                ]);
            } else if (navigator.clipboard) {
                await navigator.clipboard.writeText(tekst);
            } else {
                throw new Error('Utklippstavle-API ikke tilgjengelig');
            }

            if (status) {
                status.textContent = 'Kopiert! Lim inn i journalsystemet med Ctrl+V.';
                setTimeout(function () { status.textContent = ''; }, 4000);
            }
        } catch (e) {
            alert('Kunne ikke kopiere automatisk. Merk teksten i rapporten manuelt og kopier med Ctrl+C.');
        }
    }

    document.getElementById('rapportKopierKnapp')?.addEventListener('click', function () {
        kopierTilUtklippstavle('rapportKopierMal');
    });
    document.getElementById('rapportKopierResultatKnapp')?.addEventListener('click', function () {
        kopierTilUtklippstavle('rapportKopierResultat');
    });

    document.getElementById('rapportSkrivUtKnapp')?.addEventListener('click', function () {
        window.print();
    });
})();

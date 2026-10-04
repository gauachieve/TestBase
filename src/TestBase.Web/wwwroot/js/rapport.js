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

    // Bugliste punkt 8 (2026-10-04): grafene (SVG, tegnet direkte i selve sidevisningen) fantes
    // ALDRI i #rapportKopierMal — en helt separat, skjult markup-blokk som aldri delte DOM med de
    // synlige <svg>-elementene. Hver graf har et data-rapport-graf="<nøkkel>"-attributt på den
    // LEVENDE, synlige <svg>-en, og en matchende tom <img data-rapport-graf-plassholder="<nøkkel>">
    // inni kopier-malen. Rett før kopiering: rendre hver synlige SVG til en PNG-bitmap med
    // html2canvas (samme biblioteket tilbakemeldingswidgeten allerede bruker, vendoret lokalt —
    // se wwwroot/js/vendor/html2canvas.min.js) og sett den som src på matchende plassholder.
    // Bitmap fremfor rå SVG-markup i selve utklippstavle-nyttelasten: mange journalsystemers
    // rich text-felt stripper eller feilrendrer inline SVG ved innliming, et <img> med en
    // data:-URI limes inn pålitelig overalt.
    async function fyllInnGrafBitmaps() {
        if (typeof html2canvas !== 'function') {
            return;
        }
        var plassholdere = document.querySelectorAll('[data-rapport-graf-plassholder]');
        for (var i = 0; i < plassholdere.length; i++) {
            var plassholder = plassholdere[i];
            var nokkel = plassholder.getAttribute('data-rapport-graf-plassholder');
            var kilde = document.querySelector('[data-rapport-graf="' + nokkel + '"]');
            if (!kilde || plassholder.src) {
                continue; // allerede fylt inn (f.eks. gjenbrukt mellom "Kopier resultat"/"Kopier alt"), eller grafen finnes ikke på denne siden
            }
            try {
                var lerret = await html2canvas(kilde, { backgroundColor: '#ffffff', scale: 2 });
                plassholder.src = lerret.toDataURL('image/png');
            } catch (e) {
                // Svelges bevisst — en mislykket graf-bitmap skal aldri hindre resten av
                // kopieringen (tekst/tabeller) fra å fungere, se samme prinsipp som
                // tilbakemeldingswidgetens skjermbilde-fangst.
            }
        }
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

        await fyllInnGrafBitmaps();

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

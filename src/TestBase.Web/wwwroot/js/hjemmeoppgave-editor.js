// Hjemmeoppgave-editor (Behandlerportal/Hjemmeoppgaver/Rediger) — bugliste 2026-10-05 punkt 1-14:
// legg til/fjern/dra-omorder ledd-rader klient-side (indekserte skjemafelt Ledd[n].* — ASP.NET
// Cores modellbinding krever SAMMENHENGENDE indekser fra 0, derfor reindekseres ALLE rader etter
// hver fjerning/omordning), en visuell svaralternativ-bygger per svartype (ingen "verdi:tekst"-
// syntaks synlig for forfatteren), minimer/maksimer med tilstand lagret i localStorage, en
// kontekstsensitiv forklaringspanel, og "squash" av et opplastet bilde til en liten JPEG FØR det
// limes inn som en base64 data-URI i et skjult felt.
(function () {
    var MAKS_BREDDE_HOYDE = 1600;
    var JPEG_KVALITET = 0.8;
    var LOKAL_LAGER_NOKKEL_PREFIX = 'hjemmeoppgave-minimert-';

    var listeContainer = document.getElementById('hjemmeoppgaveLeddListe');
    var leggTilKnapp = document.getElementById('hjemmeoppgaveLeggTilLedd');
    var malRad = document.getElementById('hjemmeoppgaveLeddMal');
    var skjema = document.getElementById('hjemmeoppgaveSkjema');
    var forklaringTittel = document.getElementById('hjoForklaringTittel');
    var forklaringTekst = document.getElementById('hjoForklaringTekst');
    if (!listeContainer || !leggTilKnapp || !malRad) {
        return;
    }

    // --- Testens egen id (fra URL-stien /Rediger/{id}) — brukt KUN som del av en localStorage-
    // nøkkel for minimert-tilstand. En helt ny (ulagret) hjemmeoppgave bruker "ny" i stedet. ---
    var testIdMatch = window.location.pathname.match(/\/Rediger\/(\d+)/);
    var testIdForLagring = testIdMatch ? testIdMatch[1] : 'ny';

    // =========================================================================================
    // Forklaringspanel (punkt 3) — oppdateres når et felt inni et ledd får fokus.
    // =========================================================================================
    var FELT_FORKLARINGER = {
        Sporsmalstekst: ['Spørsmål / tekst', 'Dette er selve spørsmålet eller teksten pasienten ser på skjermen. Gjør det kort og konkret.'],
        Instruksjon: ['Instruksjon', 'En valgfri, utdypende forklaring som vises under spørsmålet — bruk den hvis spørsmålet alene kan være uklart.'],
        ErPaakrevd: ['Påkrevd', 'Når denne er krysset av, kan ikke pasienten levere inn hjemmeoppgaven før akkurat dette leddet er besvart.'],
        BildeUrl: ['Lenke under bildet', 'En valgfri lenke (f.eks. til en video) som vises som en klikkbar lenke rett under bildet.']
    };
    var SVARTYPE_FORKLARINGER = {
        LikertSkala: ['Likert-skala', 'Pasienten velger ett av flere faste svaralternativ på en skala, f.eks. "Aldri" til "Alltid". Legg til hvert alternativ under.'],
        VisuellAnalogSkala: ['Visuell Analog Skala (VAS)', 'Pasienten drar en glidebryter mellom to ytterpunkter du selv navngir, f.eks. "Ingen smerte" til "Verst tenkelig smerte".'],
        JaNei: ['Ja / Nei', 'Pasienten svarer ett av to faste alternativer — ingenting å sette opp her.'],
        Fritekst: ['Fritekst', 'Pasienten skriver inn et fritt tekstsvar.'],
        Bilde: ['Bilde', 'Rent visningsinnhold DU legger inn (f.eks. en illustrasjon) — pasienten svarer ikke på dette, kan derfor heller ikke være påkrevd.'],
        Url: ['Lenke (URL)', 'Pasienten skriver inn en lenke som sitt svar, f.eks. til et bilde eller en video de har lastet opp andre steder.']
    };

    function visForklaring(tittel, tekst) {
        if (forklaringTittel) { forklaringTittel.textContent = tittel; }
        if (forklaringTekst) { forklaringTekst.textContent = tekst; }
    }

    if (listeContainer) {
        listeContainer.addEventListener('focusin', function (e) {
            var felt = e.target.closest('[data-feltnavn]');
            if (!felt) {
                return;
            }
            var feltnavn = felt.getAttribute('data-feltnavn');
            if (feltnavn === 'Svartype') {
                var par = SVARTYPE_FORKLARINGER[felt.value];
                if (par) { visForklaring(par[0], par[1]); }
                return;
            }
            var generell = FELT_FORKLARINGER[feltnavn];
            if (generell) {
                visForklaring(generell[0], generell[1]);
                return;
            }
            if (feltnavn === 'Svaralternativer') {
                var rad = felt.closest('.hjo-ledd');
                var svartypeSelect = rad && rad.querySelector('[data-feltnavn="Svartype"]');
                var svartypePar = svartypeSelect && SVARTYPE_FORKLARINGER[svartypeSelect.value];
                if (svartypePar) { visForklaring(svartypePar[0], svartypePar[1]); }
            }
        });
    }

    // =========================================================================================
    // Reindeksering + live header-tekst (punkt 8/10) + minimer/maksimer-tilstand (punkt 11)
    // =========================================================================================
    function reindekser() {
        var rader = listeContainer.querySelectorAll('.hjo-ledd');
        rader.forEach(function (rad, indeks) {
            rad.setAttribute('data-index', String(indeks));
            rad.querySelectorAll('[data-feltnavn]').forEach(function (felt) {
                felt.name = 'Ledd[' + indeks + '].' + felt.getAttribute('data-feltnavn');
            });
        });
    }

    function oppdaterHeaderTekst(rad) {
        var headerTekst = rad.querySelector('.hjo-ledd-header-tekst');
        var sporsmalFelt = rad.querySelector('[data-feltnavn="Sporsmalstekst"]');
        if (headerTekst && sporsmalFelt) {
            headerTekst.textContent = sporsmalFelt.value && sporsmalFelt.value.trim().length > 0
                ? sporsmalFelt.value
                : '(Nytt ledd)';
        }
    }

    function minimertNokkel(rad) {
        return LOKAL_LAGER_NOKKEL_PREFIX + testIdForLagring + '-' + rad.getAttribute('data-index');
    }

    function settMinimert(rad, minimert) {
        rad.classList.toggle('hjo-ledd--minimert', minimert);
        var knapp = rad.querySelector('.hjo-minimer-knapp');
        if (knapp) {
            knapp.innerHTML = minimert
                ? knapp.getAttribute('data-maksimer-html')
                : knapp.getAttribute('data-minimer-html');
        }
        try {
            window.localStorage.setItem(minimertNokkel(rad), minimert ? '1' : '0');
        } catch (e) { /* privat nettlesing e.l. — ikke kritisk, bare hopp over lagring */ }
    }

    function lastMinimertTilstand(rad) {
        try {
            return window.localStorage.getItem(minimertNokkel(rad)) === '1';
        } catch (e) {
            return false;
        }
    }

    // =========================================================================================
    // Visuell svaralternativ-bygger (punkt 12) — ingen "verdi:tekst"-syntaks synlig. Statement-
    // listen for Likert og VAS-endepunktene speiles til det skjulte Svaralternativer-feltet rett
    // før innsending (synkroniserAlleSvaralternativer, kalt fra skjemaets submit-event), ikke
    // kontinuerlig, for å holde koden enkel.
    // =========================================================================================
    var SVARBYGGER_KLASSE = {
        LikertSkala: 'hjo-svarbygger-likert',
        VisuellAnalogSkala: 'hjo-svarbygger-vas',
        JaNei: 'hjo-svarbygger-janei',
        Url: 'hjo-svarbygger-url'
    };

    function settSynlighetForRad(rad) {
        var svartypeSelect = rad.querySelector('[data-feltnavn="Svartype"]');
        var bildeFelt = rad.querySelector('.hjo-bilde-felt');
        var paakrevdFelt = rad.querySelector('.hjo-paakrevd-felt');
        if (!svartypeSelect) {
            return;
        }
        var svartype = svartypeSelect.value;
        rad.querySelectorAll('.hjo-svarbygger').forEach(function (boks) { boks.hidden = true; });
        var klasse = SVARBYGGER_KLASSE[svartype];
        var aktivBoks = klasse ? rad.querySelector('.' + klasse) : null;
        if (aktivBoks) {
            aktivBoks.hidden = false;
        }
        if (bildeFelt) {
            bildeFelt.hidden = svartype !== 'Bilde';
        }
        if (paakrevdFelt) {
            paakrevdFelt.hidden = svartype === 'Bilde';
        }
    }

    function nyStatementRad(verdi, tekst) {
        var rad = document.createElement('div');
        rad.className = 'hjo-statement-rad';
        rad.innerHTML =
            '<input type="text" class="hjo-statement-verdi" inputmode="numeric" />' +
            '<input type="text" class="hjo-statement-tekst" placeholder="Svartekst, f.eks. «Alltid»" />' +
            '<button type="button" class="hjo-btn hjo-fjern-statement" title="Fjern">✕</button>';
        rad.querySelector('.hjo-statement-verdi').value = verdi;
        rad.querySelector('.hjo-statement-tekst').value = tekst;
        rad.querySelector('.hjo-fjern-statement').addEventListener('click', function () { rad.remove(); });
        return rad;
    }

    // Samme splitteregel som TestLeddSvaralternativer.Parse (C#) — komma ETTERFULGT av "tall:",
    // ikke ethvert komma, slik at en svartekst selv kan inneholde komma.
    function parseSvaralternativer(raa) {
        if (!raa) { return []; }
        var deler = raa.split(/,(?=-?\d+:)/);
        return deler.map(function (del) {
            var kolon = del.indexOf(':');
            if (kolon === -1) { return null; }
            return { verdi: del.substring(0, kolon).trim(), tekst: del.substring(kolon + 1) };
        }).filter(function (p) { return p !== null; });
    }

    function fyllLikertFraEksisterende(rad) {
        var skjultFelt = rad.querySelector('[data-feltnavn="Svaralternativer"]');
        var liste = rad.querySelector('.hjo-statement-liste');
        if (!skjultFelt || !liste || liste.children.length > 0) {
            return;
        }
        var punkter = parseSvaralternativer(skjultFelt.value);
        punkter.forEach(function (p) { liste.appendChild(nyStatementRad(p.verdi, p.tekst)); });
    }

    function fyllVasFraEksisterende(rad) {
        var skjultFelt = rad.querySelector('[data-feltnavn="Svaralternativer"]');
        var lavFelt = rad.querySelector('.hjo-vas-lav-tekst');
        var hoyFelt = rad.querySelector('.hjo-vas-hoy-tekst');
        if (!skjultFelt || !lavFelt || !hoyFelt || lavFelt.value || hoyFelt.value) {
            return;
        }
        var punkter = parseSvaralternativer(skjultFelt.value);
        if (punkter.length === 0) {
            return;
        }
        var sortert = punkter.slice().sort(function (a, b) { return parseFloat(a.verdi) - parseFloat(b.verdi); });
        lavFelt.value = sortert[0].tekst;
        hoyFelt.value = sortert[sortert.length - 1].tekst;
    }

    function synkroniserSvaralternativerForRad(rad) {
        var svartypeSelect = rad.querySelector('[data-feltnavn="Svartype"]');
        var skjultFelt = rad.querySelector('[data-feltnavn="Svaralternativer"]');
        if (!svartypeSelect || !skjultFelt) {
            return;
        }
        var svartype = svartypeSelect.value;
        if (svartype === 'LikertSkala') {
            var par = [];
            rad.querySelectorAll('.hjo-statement-rad').forEach(function (statementRad, i) {
                var verdi = statementRad.querySelector('.hjo-statement-verdi').value.trim() || String(i);
                var tekst = statementRad.querySelector('.hjo-statement-tekst').value;
                if (tekst && tekst.trim().length > 0) {
                    par.push(verdi + ':' + tekst);
                }
            });
            skjultFelt.value = par.join(',');
        } else if (svartype === 'VisuellAnalogSkala') {
            var lav = rad.querySelector('.hjo-vas-lav-tekst').value || '0';
            var hoy = rad.querySelector('.hjo-vas-hoy-tekst').value || '100';
            skjultFelt.value = '0:' + lav + ',100:' + hoy;
        } else {
            skjultFelt.value = '';
        }
    }

    function synkroniserAlleSvaralternativer() {
        listeContainer.querySelectorAll('.hjo-ledd').forEach(synkroniserSvaralternativerForRad);
    }

    // =========================================================================================
    // Dra-og-slipp-omordning (punkt 5)
    // =========================================================================================
    var dragesRad = null;

    function koblDragHandle(rad) {
        var handtak = rad.querySelector('.hjo-drag-handle');
        if (!handtak) {
            return;
        }
        handtak.addEventListener('dragstart', function (e) {
            dragesRad = rad;
            e.dataTransfer.effectAllowed = 'move';
            rad.classList.add('hjo-ledd--drar');
        });
        handtak.addEventListener('dragend', function () {
            rad.classList.remove('hjo-ledd--drar');
            dragesRad = null;
            reindekser();
        });
    }

    listeContainer.addEventListener('dragover', function (e) {
        if (!dragesRad) {
            return;
        }
        e.preventDefault();
        var etter = Array.prototype.find.call(listeContainer.querySelectorAll('.hjo-ledd'), function (rad) {
            if (rad === dragesRad) { return false; }
            var boks = rad.getBoundingClientRect();
            return e.clientY < boks.top + boks.height / 2;
        });
        if (etter) {
            listeContainer.insertBefore(dragesRad, etter);
        } else {
            listeContainer.appendChild(dragesRad);
        }
    });

    // =========================================================================================
    // Rad-oppsett (koble alle felt/knapper for både eksisterende og nylig lagt til rader)
    // =========================================================================================
    function leggTilRad(kildeHtml) {
        var wrapper = document.createElement('div');
        wrapper.innerHTML = kildeHtml;
        var nyRad = wrapper.firstElementChild;
        listeContainer.appendChild(nyRad);
        koble(nyRad);
        reindekser();
        var forsteFelt = nyRad.querySelector('.hjo-sporsmal-input');
        if (forsteFelt) { forsteFelt.focus(); }
    }

    function tilpassInstruksjonHoyde(felt) {
        felt.style.height = 'auto';
        felt.style.height = felt.scrollHeight + 'px';
    }

    function koble(rad) {
        var svartypeSelect = rad.querySelector('[data-feltnavn="Svartype"]');
        if (svartypeSelect) {
            svartypeSelect.addEventListener('change', function () { settSynlighetForRad(rad); });
            settSynlighetForRad(rad);
        }
        fyllLikertFraEksisterende(rad);
        fyllVasFraEksisterende(rad);

        var sporsmalFelt = rad.querySelector('[data-feltnavn="Sporsmalstekst"]');
        if (sporsmalFelt) {
            sporsmalFelt.addEventListener('input', function () { oppdaterHeaderTekst(rad); });
        }

        var instruksjonFelt = rad.querySelector('.hjo-instruksjon-input');
        if (instruksjonFelt) {
            tilpassInstruksjonHoyde(instruksjonFelt);
            instruksjonFelt.addEventListener('input', function () { tilpassInstruksjonHoyde(instruksjonFelt); });
        }

        var leggTilStatementKnapp = rad.querySelector('.hjo-legg-til-statement');
        if (leggTilStatementKnapp) {
            leggTilStatementKnapp.addEventListener('click', function () {
                var liste = rad.querySelector('.hjo-statement-liste');
                liste.appendChild(nyStatementRad(String(liste.children.length), ''));
            });
        }
        rad.querySelectorAll('.hjo-fjern-statement').forEach(function (knapp) {
            knapp.addEventListener('click', function () { knapp.closest('.hjo-statement-rad').remove(); });
        });

        var minimerKnapp = rad.querySelector('.hjo-minimer-knapp');
        if (minimerKnapp) {
            // "Maksimer"-tilstanden gjenbruker samme SVG-ramme, bare med pil-ned i stedet for
            // pil-opp (samme stimønster som _Ikon.cshtml sin "maksimer"-variant).
            minimerKnapp.setAttribute('data-minimer-html', minimerKnapp.innerHTML);
            minimerKnapp.setAttribute(
                'data-maksimer-html',
                minimerKnapp.innerHTML.replace('M18 15l-6-6-6 6', 'M6 9l6 6 6-6').replace('>Minimer<', '>Vis<')
            );
            minimerKnapp.addEventListener('click', function () {
                var erMinimert = rad.classList.contains('hjo-ledd--minimert');
                settMinimert(rad, !erMinimert);
            });
            if (lastMinimertTilstand(rad)) {
                settMinimert(rad, true);
            }
        }

        var fjernKnapp = rad.querySelector('.hjo-fjern-knapp');
        if (fjernKnapp) {
            fjernKnapp.addEventListener('click', function () {
                rad.remove();
                reindekser();
            });
        }

        koblDragHandle(rad);
        oppdaterHeaderTekst(rad);

        var filInput = rad.querySelector('.hjo-bilde-fil');
        if (filInput) {
            filInput.addEventListener('change', function () {
                var fil = filInput.files && filInput.files[0];
                if (!fil) {
                    return;
                }
                squashBilde(fil, function (dataUrl) {
                    var dataFelt = rad.querySelector('[data-feltnavn="BildeData"]');
                    var typeFelt = rad.querySelector('[data-feltnavn="BildeContentType"]');
                    var forhandsvisning = rad.querySelector('.hjo-bilde-forhandsvisning');
                    var kommaIndeks = dataUrl.indexOf(',');
                    if (dataFelt) {
                        dataFelt.value = dataUrl.substring(kommaIndeks + 1);
                    }
                    if (typeFelt) {
                        typeFelt.value = 'image/jpeg';
                    }
                    if (forhandsvisning) {
                        forhandsvisning.src = dataUrl;
                        forhandsvisning.hidden = false;
                    }
                });
            });
        }
    }

    // Skalerer ned til maks MAKS_BREDDE_HOYDE px på lengste side og rekoder til JPEG — en rå
    // mobilbilde-original (ofte 3-8 MB) blir typisk noen hundre KB etter dette, trygt å lagre
    // som base64 i databasen (samme pragmatiske mønster som tilbakemeldingswidgetens skjermbilder).
    function squashBilde(fil, ferdig) {
        var bilde = new Image();
        var leser = new FileReader();
        leser.onload = function (e) {
            bilde.onload = function () {
                var skala = Math.min(1, MAKS_BREDDE_HOYDE / Math.max(bilde.width, bilde.height));
                var lerret = document.createElement('canvas');
                lerret.width = Math.round(bilde.width * skala);
                lerret.height = Math.round(bilde.height * skala);
                var ctx = lerret.getContext('2d');
                ctx.drawImage(bilde, 0, 0, lerret.width, lerret.height);
                ferdig(lerret.toDataURL('image/jpeg', JPEG_KVALITET));
            };
            bilde.src = e.target.result;
        };
        leser.readAsDataURL(fil);
    }

    leggTilKnapp.addEventListener('click', function () {
        leggTilRad(malRad.innerHTML);
    });

    if (skjema) {
        skjema.addEventListener('submit', synkroniserAlleSvaralternativer);
    }

    listeContainer.querySelectorAll('.hjo-ledd').forEach(koble);
})();

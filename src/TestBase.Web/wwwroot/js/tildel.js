// Tildelingsflyten (Behandlerportal/Tildel og Admin/Tildel, steg 2 — se
// Tester.cshtml.cs): en test kan vises i flere kategorier samtidig, så
// avkrysning av én må synkroniseres til alle forekomster av samme test
// (matchet på data-test-id) — og oppsummerings-dialogen bygges fra hvilke
// checkboxer som faktisk er krysset av, uten noen server-tur.
//
// Prisforhåndsvisningen speiler TestPrisberegner.Beregn (se
// TestBase.Shared/Domain/Tester/TestPrisberegner.cs) — hold formlene i synk
// hvis den endres. Admin-siden mangler prising-data-attributter helt (admin-
// tildeling er alltid "IkkePåkrevd"), så alt her degraderer stille til bare
// testnavn uten pris når data-minste-pris ikke finnes.
(function () {
    document.addEventListener('change', function (hendelse) {
        if (!hendelse.target.matches('.tildel-test-checkbox')) {
            return;
        }
        var testId = hendelse.target.getAttribute('data-test-id');
        document.querySelectorAll('.tildel-test-checkbox[data-test-id="' + testId + '"]').forEach(function (checkbox) {
            checkbox.checked = hendelse.target.checked;
        });
    });

    var form = document.getElementById('tildelForm');
    var apneKnapp = document.getElementById('apneOppsummering');
    var dialog = document.getElementById('oppsummeringDialog');
    var lukkKnapp = document.getElementById('lukkOppsummering');
    var testeListe = document.getElementById('oppsummeringTester');
    var totalLinje = document.getElementById('oppsummeringTotal');

    function tall(streng, fallback) {
        var n = parseFloat(streng);
        return isNaN(n) ? (fallback || 0) : n;
    }

    // Admin-siden har ingen prising og dermed ingen #oppsummeringTotal-element i
    // det hele tatt — denne må derfor ALDRI anta at elementet finnes.
    function settTotalLinje(tekst) {
        if (totalLinje) {
            totalLinje.textContent = tekst;
        }
    }

    function formatNok(belop) {
        return belop.toLocaleString('nb-NO', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' NOK';
    }

    // Speiler TestPrisberegner.Beregn nøyaktig — se filhode.
    function beregnForTest(checkbox, smsGebyrKr) {
        if (!checkbox.hasAttribute('data-minste-pris')) {
            return null; // Admin-siden: ingen prising i det hele tatt.
        }

        var minstePris = tall(checkbox.getAttribute('data-minste-pris'));
        var storstePris = Math.max(tall(checkbox.getAttribute('data-storste-pris')), minstePris);
        var typiskHonorar = tall(checkbox.getAttribute('data-typisk-honorar'));
        var partnerAndel = tall(checkbox.getAttribute('data-partner-andel'));
        var dekketAvAbonnement = form.getAttribute('data-dekket-av-abonnement') === 'true';

        var plattformAndel = dekketAvAbonnement ? 0 : minstePris;

        var honorarInput = document.getElementById('honorar-' + checkbox.getAttribute('data-test-id'));
        var onsketHonorar = honorarInput && honorarInput.value !== '' ? tall(honorarInput.value, typiskHonorar) : typiskHonorar;

        var onsketTotal = plattformAndel + partnerAndel + onsketHonorar;
        var totalUtenSms = Math.min(Math.max(onsketTotal, minstePris), storstePris);
        var behandlerHonorar = Math.max(0, totalUtenSms - plattformAndel - partnerAndel);

        return {
            totalKr: totalUtenSms + smsGebyrKr,
            behandlerHonorar: behandlerHonorar,
            plattformAndel: plattformAndel + smsGebyrKr,
            partnerAndel: partnerAndel,
            smsGebyrKr: smsGebyrKr
        };
    }

    function oppdaterOppsummering() {
        var valgtMetode = document.querySelector('.tildel-varslingsmetode:checked');
        var inkludererSms = valgtMetode && (valgtMetode.value === 'Sms' || valgtMetode.value === 'Begge');
        var smsGebyrKr = inkludererSms ? tall(form.getAttribute('data-sms-gebyr-kr')) : 0;
        var antallPasienter = tall(form.getAttribute('data-antall-pasienter'), 1);

        var seddeTestIder = {};
        var sumTotal = 0, sumPlattform = 0, sumPartner = 0, sumBehandler = 0;
        var harPrising = false;

        testeListe.innerHTML = '';
        document.querySelectorAll('.tildel-test-checkbox:checked').forEach(function (checkbox) {
            var testId = checkbox.getAttribute('data-test-id');
            if (seddeTestIder[testId]) {
                return;
            }
            seddeTestIder[testId] = true;

            var testNavn = checkbox.getAttribute('data-test-navn') || testId;
            var pris = beregnForTest(checkbox, smsGebyrKr);
            var punkt = document.createElement('li');

            if (pris) {
                harPrising = true;
                sumTotal += pris.totalKr;
                sumPlattform += pris.plattformAndel;
                sumPartner += pris.partnerAndel;
                sumBehandler += pris.behandlerHonorar;

                var detaljer = 'Plattform ' + formatNok(pris.plattformAndel) +
                    (pris.partnerAndel > 0 ? ', partner ' + formatNok(pris.partnerAndel) : '') +
                    ', ditt honorar ' + formatNok(pris.behandlerHonorar) +
                    (pris.smsGebyrKr > 0 ? ' (inkl. ' + formatNok(pris.smsGebyrKr) + ' SMS-gebyr)' : '');
                punkt.textContent = testNavn + ' — ' + formatNok(pris.totalKr) + ' per pasient (' + detaljer + ')';
            } else {
                punkt.textContent = testNavn;
            }

            testeListe.appendChild(punkt);
        });

        if (Object.keys(seddeTestIder).length === 0) {
            settTotalLinje('');
            return;
        }

        if (harPrising) {
            var linje = 'Totalt per pasient: ' + formatNok(sumTotal) +
                ' (plattform ' + formatNok(sumPlattform) +
                (sumPartner > 0 ? ', partner ' + formatNok(sumPartner) : '') +
                ', behandlerhonorar ' + formatNok(sumBehandler) + ')';
            if (antallPasienter > 1) {
                linje += ' — totalt for alle ' + antallPasienter + ' pasienter: ' + formatNok(sumTotal * antallPasienter);
            }
            settTotalLinje(linje);
        } else {
            settTotalLinje('');
        }
    }

    if (apneKnapp && dialog && testeListe) {
        apneKnapp.addEventListener('click', function () {
            var noeValgt = document.querySelectorAll('.tildel-test-checkbox:checked').length > 0;
            if (!noeValgt) {
                alert('Velg minst én test før du går videre.');
                return;
            }

            oppdaterOppsummering();
            dialog.showModal();
        });
    }

    document.addEventListener('change', function (hendelse) {
        if (hendelse.target.matches('.tildel-varslingsmetode') || hendelse.target.matches('.tildel-honorar-input')) {
            oppdaterOppsummering();
        }
    });

    if (lukkKnapp && dialog) {
        lukkKnapp.addEventListener('click', function () {
            dialog.close();
        });
    }
})();

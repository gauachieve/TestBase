// Program-kalender-editor (Behandlerportal/Programmer/Rediger — bugliste 2026-10-05 punkt
// 28-35): en relativ "dag 0..41"-kalender (IKKE en ekte måned/år-kalender, siden et programs
// faktiske startdato først avgjøres ved tildeling, se ProgramService.TildelAsync sin bruk av
// PlanlagtTildelingService.BeregnNesteForekomstUtc) — klikk en dag for å legge til/redigere en
// drop den dagen via ÉN gjenbrukt dialog (punkt 33, "reuse the tildel tester window"), som
// inneholder SAMME kategori-tre-test-velger-komponent som Tildel/Tester (se
// Pages/Shared/_TestKategoriVelger.cshtml — inkluderer dermed alltid hjemmeoppgaver, punkt 34).
//
// Intern tilstand (dropsPerDag) holdes i minnet og serialiseres til de skjulte Drops[i].*-feltene
// rett før innsending — DagerEtterForrige for drop 0 ignoreres helt av motoren (se
// ProgramService.BeregnDropDag, loopen starter på i=1), så den trenger ikke være "riktig" for
// akkurat den ene raden.
(function () {
    var kalender = document.getElementById('programKalender');
    var skjultFelterContainer = document.getElementById('programDropFelterSkjult');
    var dialog = document.getElementById('programDropDialog');
    var dialogTittel = document.getElementById('programDropDialogTittel');
    var fraInput = document.getElementById('programDropFra');
    var tilInput = document.getElementById('programDropTil');
    var unngaaNattInput = document.getElementById('programDropUnngaaNatt');
    var avbrytKnapp = document.getElementById('programDropAvbryt');
    var fjernKnapp = document.getElementById('programDropFjern');
    var lagreKnapp = document.getElementById('programDropLagre');
    var skjema = document.getElementById('programSkjema');
    if (!kalender || !dialog || !skjema) {
        return;
    }

    var dropsPerDag = {};
    var gjeldendeDag = null;

    var eksisterendeElement = document.getElementById('programEksisterendeDrops');
    if (eksisterendeElement) {
        try {
            var eksisterende = JSON.parse(eksisterendeElement.textContent || '[]');
            eksisterende.forEach(function (d) {
                dropsPerDag[d.dag] = { fra: d.fra, til: d.til, unngaaNatt: d.unngaaNatt, testIder: d.testIder || [] };
            });
        } catch (e) {
            // Korrupt/manglende data — start med en tom kalender i stedet for å feile helt.
        }
    }

    function oppdaterCelle(dag) {
        var celle = kalender.querySelector('.program-dag-celle[data-dag="' + dag + '"]');
        if (!celle) {
            return;
        }
        var badge = celle.querySelector('.program-dag-badge');
        var drop = dropsPerDag[dag];
        celle.classList.toggle('program-dag-celle--har-drop', !!drop);
        if (drop) {
            badge.hidden = false;
            badge.textContent = drop.testIder.length + ' test' + (drop.testIder.length === 1 ? '' : 'er');
        } else {
            badge.hidden = true;
        }
    }

    function tegnAlleCeller() {
        for (var dag = 0; dag < 42; dag++) {
            oppdaterCelle(dag);
        }
    }

    function apneDialog(dag) {
        gjeldendeDag = dag;
        dialogTittel.textContent = 'Drop på dag ' + dag;
        var drop = dropsPerDag[dag];
        fraInput.value = drop ? drop.fra : '09:00';
        tilInput.value = drop ? drop.til : '18:00';
        unngaaNattInput.checked = drop ? drop.unngaaNatt : true;
        fjernKnapp.hidden = !drop;

        var valgteTestIder = drop ? drop.testIder : [];
        dialog.querySelectorAll('.tildel-test-checkbox').forEach(function (cb) {
            var testId = parseInt(cb.getAttribute('data-test-id'), 10);
            cb.checked = valgteTestIder.indexOf(testId) !== -1;
        });
        // Trigger infoboks/kategori-fremheving for et allerede gjenåpnet valg.
        var forsteValgte = dialog.querySelector('.tildel-test-checkbox:checked');
        if (forsteValgte) {
            forsteValgte.dispatchEvent(new Event('change', { bubbles: true }));
        }

        dialog.showModal();
    }

    kalender.addEventListener('click', function (e) {
        var celle = e.target.closest('.program-dag-celle');
        if (!celle) {
            return;
        }
        apneDialog(parseInt(celle.getAttribute('data-dag'), 10));
    });

    avbrytKnapp.addEventListener('click', function () { dialog.close(); });

    fjernKnapp.addEventListener('click', function () {
        delete dropsPerDag[gjeldendeDag];
        oppdaterCelle(gjeldendeDag);
        dialog.close();
    });

    lagreKnapp.addEventListener('click', function () {
        var valgteTestIder = [];
        dialog.querySelectorAll('.tildel-test-checkbox:checked').forEach(function (cb) {
            var id = parseInt(cb.getAttribute('data-test-id'), 10);
            if (valgteTestIder.indexOf(id) === -1) {
                valgteTestIder.push(id);
            }
        });
        if (valgteTestIder.length === 0) {
            alert('Velg minst én test for denne droppen.');
            return;
        }
        dropsPerDag[gjeldendeDag] = {
            fra: fraInput.value || '09:00',
            til: tilInput.value || '18:00',
            unngaaNatt: unngaaNattInput.checked,
            testIder: valgteTestIder
        };
        oppdaterCelle(gjeldendeDag);
        dialog.close();
    });

    function leggTilSkjultFelt(navn, verdi) {
        var felt = document.createElement('input');
        felt.type = 'hidden';
        felt.name = navn;
        felt.value = verdi;
        skjultFelterContainer.appendChild(felt);
    }

    skjema.addEventListener('submit', function () {
        skjultFelterContainer.innerHTML = '';
        var dager = Object.keys(dropsPerDag).map(function (d) { return parseInt(d, 10); }).sort(function (a, b) { return a - b; });
        var forrigeDag = 0;
        dager.forEach(function (dag, i) {
            var drop = dropsPerDag[dag];
            var dagerEtterForrige = i === 0 ? dag : dag - forrigeDag;
            forrigeDag = dag;
            leggTilSkjultFelt('Drops[' + i + '].DagerEtterForrige', String(dagerEtterForrige));
            leggTilSkjultFelt('Drops[' + i + '].FraKlokkeslett', drop.fra);
            leggTilSkjultFelt('Drops[' + i + '].TilKlokkeslett', drop.til);
            leggTilSkjultFelt('Drops[' + i + '].UnngaaNatt', drop.unngaaNatt ? 'true' : 'false');
            drop.testIder.forEach(function (testId) {
                leggTilSkjultFelt('Drops[' + i + '].TestIder', String(testId));
            });
        });
    });

    tegnAlleCeller();
})();

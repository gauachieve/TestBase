// "Generer Temp Gruppe Rapport" / "Generer Gruppe Rapport"-popupen på
// Grupper/Rediger (se GruppeService.HentEnkelttestAsync) — ÉN delt <dialog>,
// de to knappene skiller seg kun ved en skjult Provedata-verdi. Skjemaet er en
// vanlig GET til Aggregert-siden (ingen egen POST-handler, ingen lagret
// rapport — samme "beregn på nytt hver gang"-prinsipp som resten av
// gruppeaggregeringen), så selve rapportgenereringen skjer på server-siden
// ved sidevisning, ikke her.
(function () {
    var dialog = document.getElementById('grupperapportDialog');
    if (!dialog) {
        return;
    }

    var provedataInput = document.getElementById('grupperapportProvedata');
    var tittel = document.getElementById('grupperapportTittel');
    var fraInput = document.getElementById('grupperapportFra');
    var tilInput = document.getElementById('grupperapportTil');
    var avbrytKnapp = document.getElementById('grupperapportAvbryt');
    var radioer = document.querySelectorAll('.rapport-test-radio');
    var iDag = new Date().toISOString().slice(0, 10);

    tilInput.max = iDag;
    if (!tilInput.value) {
        tilInput.value = iDag;
    }

    function oppdaterFraFelt() {
        var valgt = document.querySelector('.rapport-test-radio:checked');
        if (!valgt) {
            return;
        }
        var provedata = provedataInput.value === 'true';
        var tidligste = provedata ? valgt.getAttribute('data-tidligste-provedata') : valgt.getAttribute('data-tidligste-ekte');
        if (tidligste) {
            fraInput.min = tidligste;
            fraInput.value = tidligste;
        } else {
            fraInput.removeAttribute('min');
            fraInput.value = iDag;
        }
    }

    radioer.forEach(function (radio) {
        radio.addEventListener('change', oppdaterFraFelt);
    });

    document.querySelectorAll('[data-rapportknapp]').forEach(function (knapp) {
        knapp.addEventListener('click', function () {
            var erProvedata = knapp.getAttribute('data-provedata');
            provedataInput.value = erProvedata;
            if (tittel) {
                tittel.textContent = erProvedata === 'true' ? 'Generer temp gruppe­rapport' : 'Generer gruppe­rapport';
            }
            if (radioer.length > 0 && !document.querySelector('.rapport-test-radio:checked')) {
                radioer[0].checked = true;
            }
            oppdaterFraFelt();
            dialog.showModal();
        });
    });

    if (avbrytKnapp) {
        avbrytKnapp.addEventListener('click', function () {
            dialog.close();
        });
    }
})();

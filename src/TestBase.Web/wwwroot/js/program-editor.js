// Program-editor (Behandlerportal/Programmer/Rediger, 2026-10-04): legg til/fjern drop-rader
// klient-side — samme reindekserings-mønster som hjemmeoppgave-editor.js (se der for hvorfor
// ALLE rader, ikke bare nye, må bære data-feltnavn for at en fjerning midt i listen skal gi
// korrekte, sammenhengende indekser etterpå).
(function () {
    var listeContainer = document.getElementById('programDropListe');
    var leggTilKnapp = document.getElementById('programLeggTilDrop');
    var malRad = document.getElementById('programDropMal');
    if (!listeContainer || !leggTilKnapp || !malRad) {
        return;
    }

    function reindekser() {
        var rader = listeContainer.querySelectorAll('.program-drop-rad');
        rader.forEach(function (rad, indeks) {
            rad.querySelectorAll('[data-feltnavn]').forEach(function (felt) {
                felt.name = 'Drops[' + indeks + '].' + felt.getAttribute('data-feltnavn');
            });
            var nummerLabel = rad.querySelector('.program-drop-nummer');
            if (nummerLabel) {
                nummerLabel.textContent = 'Drop ' + (indeks + 1);
            }
        });
    }

    function koble(rad) {
        var fjernKnapp = rad.querySelector('.program-fjern-drop');
        if (fjernKnapp) {
            fjernKnapp.addEventListener('click', function () {
                if (listeContainer.querySelectorAll('.program-drop-rad').length <= 1) {
                    alert('Programmet må ha minst én drop.');
                    return;
                }
                rad.remove();
                reindekser();
            });
        }
    }

    leggTilKnapp.addEventListener('click', function () {
        var wrapper = document.createElement('div');
        wrapper.innerHTML = malRad.innerHTML;
        var nyRad = wrapper.firstElementChild;
        listeContainer.appendChild(nyRad);
        koble(nyRad);
        reindekser();
    });

    listeContainer.querySelectorAll('.program-drop-rad').forEach(koble);
})();

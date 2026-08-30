// Tildelingsflyten (Behandlerportal/Tildel og Admin/Tildel, steg 2 — se
// Tester.cshtml.cs): en test kan vises i flere kategorier samtidig, så
// avkrysning av én må synkroniseres til alle forekomster av samme test
// (matchet på data-test-id) — og oppsummerings-dialogen bygges fra hvilke
// checkboxer som faktisk er krysset av, uten noen server-tur.
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

    var apneKnapp = document.getElementById('apneOppsummering');
    var dialog = document.getElementById('oppsummeringDialog');
    var lukkKnapp = document.getElementById('lukkOppsummering');
    var testeListe = document.getElementById('oppsummeringTester');

    if (apneKnapp && dialog && testeListe) {
        apneKnapp.addEventListener('click', function () {
            var seddeTestIder = {};
            var navn = [];

            document.querySelectorAll('.tildel-test-checkbox:checked').forEach(function (checkbox) {
                var testId = checkbox.getAttribute('data-test-id');
                if (seddeTestIder[testId]) {
                    return;
                }
                seddeTestIder[testId] = true;
                var etikett = checkbox.closest('label');
                navn.push(etikett ? etikett.textContent.trim() : testId);
            });

            if (navn.length === 0) {
                alert('Velg minst én test før du går videre.');
                return;
            }

            testeListe.innerHTML = '';
            navn.forEach(function (testNavn) {
                var punkt = document.createElement('li');
                punkt.textContent = testNavn;
                testeListe.appendChild(punkt);
            });

            dialog.showModal();
        });
    }

    if (lukkKnapp && dialog) {
        lukkKnapp.addEventListener('click', function () {
            dialog.close();
        });
    }
})();

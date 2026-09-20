// Infoboks som viser detaljer om testen man nettopp krysset av for, i
// kategori-treet som brukes til tildeling/gruppeopprettelse (Behandlerportal/
// Admin sine Tildel/Tester og Grupper/Ny+Rediger). Leser data-attributter satt
// direkte på hver <input type="checkbox" class="tildel-test-checkbox">, ingen
// egen server-tur. Viser alltid SISTE avkryssede test (bytte av valg bytter
// også infoboksen), skjuler boksen igjen når ingen tester er valgt.
(function () {
    document.querySelectorAll('.test-infoboks').forEach(function (boks) {
        var container = boks.closest('.tildel-med-info') || document;
        var avkrysninger = container.querySelectorAll('.tildel-test-checkbox');
        if (avkrysninger.length === 0) {
            return;
        }

        function visInfo(checkbox) {
            var navn = checkbox.getAttribute('data-test-navn') || '';
            var introduksjon = checkbox.getAttribute('data-rapport-introduksjon') || '';
            var beskrivelse = checkbox.getAttribute('data-beskrivelse') || '';
            var estimertMin = parseInt(checkbox.getAttribute('data-estimert-min') || '0', 10);
            var minstePris = parseFloat(checkbox.getAttribute('data-minste-pris') || '0');
            var storstePris = parseFloat(checkbox.getAttribute('data-storste-pris') || '0');
            var typiskHonorar = parseFloat(checkbox.getAttribute('data-typisk-honorar') || '0');

            var html = '<h3>' + navn + '</h3>';
            if (introduksjon) {
                html += '<p>' + introduksjon + '</p>';
            } else if (beskrivelse) {
                html += '<p>' + beskrivelse + '</p>';
            }
            if (estimertMin > 0) {
                html += '<p><strong>Anslått utfyllingstid:</strong> ca. ' + estimertMin + ' minutt' + (estimertMin === 1 ? '' : 'er') + '.</p>';
            }
            if (storstePris > 0) {
                html += '<p><strong>Prising:</strong> pasienten betaler inntil ' + storstePris.toFixed(2) + ' kr (min. ' + minstePris.toFixed(2) + ' kr), typisk behandlerhonorar ' + typiskHonorar.toFixed(2) + ' kr.</p>';
            } else {
                html += '<p><strong>Prising:</strong> denne testen er gratis for pasienten — det er ikke satt opp noen betalingsmulighet for den ennå (maks pris 0 kr).</p>';
            }

            boks.innerHTML = html;
            boks.classList.remove('skjult');
        }

        avkrysninger.forEach(function (checkbox) {
            checkbox.addEventListener('change', function () {
                if (checkbox.checked) {
                    visInfo(checkbox);
                    return;
                }

                var fortsattValgt = Array.prototype.filter.call(avkrysninger, function (c) { return c.checked; });
                if (fortsattValgt.length > 0) {
                    visInfo(fortsattValgt[fortsattValgt.length - 1]);
                } else {
                    boks.classList.add('skjult');
                }
            });
        });
    });
})();

// Hjemmeoppgave-editor (Behandlerportal/Hjemmeoppgaver/Rediger, 2026-10-04): legg til/fjern
// ledd-rader klient-side (indekserte skjemafelt Ledd[n].* — ASP.NET Cores modellbinding krever
// SAMMENHENGENDE indekser fra 0, derfor reindekseres ALLE rader etter hver fjerning), vis/skjul
// felt basert på valgt svartype, og "squash" et opplastet bilde til en liten JPEG FØR det limes
// inn som en base64 data-URI i et skjult felt — en rå mobilbilde-original skal ALDRI nå serveren
// (brukerens eksplisitte krav, se docs/beslutningslogg.md "Hjemmeoppgaver og programmer").
(function () {
    var MAKS_BREDDE_HOYDE = 1600;
    var JPEG_KVALITET = 0.8;

    var listeContainer = document.getElementById('hjemmeoppgaveLeddListe');
    var leggTilKnapp = document.getElementById('hjemmeoppgaveLeggTilLedd');
    var malRad = document.getElementById('hjemmeoppgaveLeddMal');
    if (!listeContainer || !leggTilKnapp || !malRad) {
        return;
    }

    function reindekser() {
        var rader = listeContainer.querySelectorAll('.hjemmeoppgave-ledd-rad');
        rader.forEach(function (rad, indeks) {
            rad.querySelectorAll('[data-feltnavn]').forEach(function (felt) {
                felt.name = 'Ledd[' + indeks + '].' + felt.getAttribute('data-feltnavn');
            });
            var nummerLabel = rad.querySelector('.hjemmeoppgave-ledd-nummer');
            if (nummerLabel) {
                nummerLabel.textContent = 'Ledd ' + (indeks + 1);
            }
        });
    }

    function settSynlighetForRad(rad) {
        var svartypeSelect = rad.querySelector('[data-feltnavn="Svartype"]');
        var svaralternativerFelt = rad.querySelector('.hjemmeoppgave-svaralternativer-felt');
        var bildeFelt = rad.querySelector('.hjemmeoppgave-bilde-felt');
        var paakrevdFelt = rad.querySelector('.hjemmeoppgave-paakrevd-felt');
        if (!svartypeSelect) {
            return;
        }
        var svartype = svartypeSelect.value;
        var erLikertEllerVas = svartype === 'LikertSkala' || svartype === 'VisuellAnalogSkala';
        var erBilde = svartype === 'Bilde';
        if (svaralternativerFelt) {
            svaralternativerFelt.hidden = !erLikertEllerVas;
        }
        if (bildeFelt) {
            bildeFelt.hidden = !erBilde;
        }
        if (paakrevdFelt) {
            paakrevdFelt.hidden = erBilde;
        }
    }

    function leggTilRad(kildeHtml) {
        var wrapper = document.createElement('div');
        wrapper.innerHTML = kildeHtml;
        var nyRad = wrapper.firstElementChild;
        listeContainer.appendChild(nyRad);
        koble(nyRad);
        reindekser();
    }

    function koble(rad) {
        var svartypeSelect = rad.querySelector('[data-feltnavn="Svartype"]');
        if (svartypeSelect) {
            svartypeSelect.addEventListener('change', function () { settSynlighetForRad(rad); });
            settSynlighetForRad(rad);
        }

        var fjernKnapp = rad.querySelector('.hjemmeoppgave-fjern-ledd');
        if (fjernKnapp) {
            fjernKnapp.addEventListener('click', function () {
                if (listeContainer.querySelectorAll('.hjemmeoppgave-ledd-rad').length <= 1) {
                    alert('Hjemmeoppgaven må ha minst ett ledd.');
                    return;
                }
                rad.remove();
                reindekser();
            });
        }

        var filInput = rad.querySelector('.hjemmeoppgave-bilde-fil');
        if (filInput) {
            filInput.addEventListener('change', function () {
                var fil = filInput.files && filInput.files[0];
                if (!fil) {
                    return;
                }
                squashBilde(fil, function (dataUrl) {
                    var dataFelt = rad.querySelector('[data-feltnavn="BildeData"]');
                    var typeFelt = rad.querySelector('[data-feltnavn="BildeContentType"]');
                    var forhandsvisning = rad.querySelector('.hjemmeoppgave-bilde-forhandsvisning');
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

    listeContainer.querySelectorAll('.hjemmeoppgave-ledd-rad').forEach(koble);
})();

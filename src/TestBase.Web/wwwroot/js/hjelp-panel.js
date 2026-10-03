// Hjelp-panelet — en IKKE-modal skuff som sklir inn fra høyre (full skjerm på mobil), åpnet fra
// "Hjelp"-knappen i tilbakemeldingswidgetens rollup-meny (se _TilbakemeldingWidget.cshtml).
// Rolle-/kontekstfiltrering skjer SERVER-SIDE i _HjelpPanel.cshtml — denne filen gjør KUN
// client-side søk (tekstfiltrering av allerede rolle-filtrert markup) og åpne/lukke/utvide.
// Selvstendig IIFE, samme mønster som tilbakemelding-widget.js — ingen delt tilstand, kun
// delte DOM-id-er som "grensesnitt" mellom de to.
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var panel = document.getElementById("hjelp-panel");
        var apneKnapp = document.getElementById("tbm-meny-hjelp");
        var lukkKnapp = document.getElementById("hjelp-panel-lukk");
        var sokFelt = document.getElementById("hjelp-sok");
        var innhold = document.getElementById("hjelp-innhold");
        var ingenTreff = document.getElementById("hjelp-ingen-treff");
        var tilTilbakemelding = document.getElementById("hjelp-til-tilbakemelding");
        if (!panel || !apneKnapp) { return; }

        var tbmMeny = document.getElementById("tbm-meny");
        var tbmFab = document.getElementById("tbm-fab");

        function apnePanel() {
            if (tbmMeny) { tbmMeny.hidden = true; }
            if (tbmFab) { tbmFab.setAttribute("aria-expanded", "false"); }
            panel.classList.add("hjelp-apen");
            panel.setAttribute("aria-hidden", "false");
            if (sokFelt) {
                sokFelt.value = "";
                utforSok("");
                setTimeout(function () { sokFelt.focus(); }, 260);
            }
        }

        function lukkPanel() {
            panel.classList.remove("hjelp-apen");
            panel.setAttribute("aria-hidden", "true");
        }

        apneKnapp.addEventListener("click", apnePanel);
        if (lukkKnapp) { lukkKnapp.addEventListener("click", lukkPanel); }

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && panel.classList.contains("hjelp-apen")) {
                lukkPanel();
            }
        });

        // --- Utvid/kollaps én artikkel (enkel accordion, flere kan være åpne samtidig) -----
        innhold.addEventListener("click", function (e) {
            var knapp = e.target.closest(".hjelp-artikkel-tittel");
            if (!knapp) { return; }
            var li = knapp.closest(".hjelp-artikkel");
            var boks = li.querySelector(".hjelp-artikkel-innhold");
            var apen = knapp.getAttribute("aria-expanded") === "true";
            knapp.setAttribute("aria-expanded", apen ? "false" : "true");
            boks.hidden = apen;
            li.classList.toggle("hjelp-artikkel-apen", !apen);
        });

        // --- Søk ------------------------------------------------------------------------
        var alleArtikler = Array.prototype.slice.call(innhold.querySelectorAll(".hjelp-artikkel"));
        var alleSeksjoner = Array.prototype.slice.call(innhold.querySelectorAll("[data-hjelp-seksjon]"));
        var alleEmnerDetails = innhold.querySelector(".hjelp-alle-emner");

        function utforSok(raaTekst) {
            var sok = raaTekst.trim().toLowerCase();
            var sokAktiv = sok.length > 0;
            var noeSynlig = false;

            alleArtikler.forEach(function (li) {
                var treff = !sokAktiv || (li.getAttribute("data-hjelp-sok") || "").indexOf(sok) !== -1;
                li.hidden = !treff;
                if (treff) { noeSynlig = true; }
            });

            alleSeksjoner.forEach(function (seksjon) {
                var harSynligArtikkel = Array.prototype.some.call(
                    seksjon.querySelectorAll(".hjelp-artikkel"),
                    function (li) { return !li.hidden; }
                );
                seksjon.hidden = sokAktiv && !harSynligArtikkel;
            });

            // Tving "Bla i alle emner" åpen når et søk faktisk treffer noe der inne, slik at
            // <details>-elementets lukkede tilstand ikke skjuler et reelt treff.
            if (alleEmnerDetails) {
                if (sokAktiv) {
                    var harTreffInniAlleEmner = Array.prototype.some.call(
                        alleEmnerDetails.querySelectorAll(".hjelp-artikkel"),
                        function (li) { return !li.hidden; }
                    );
                    if (harTreffInniAlleEmner) { alleEmnerDetails.open = true; }
                } else {
                    alleEmnerDetails.open = false;
                }
            }

            if (ingenTreff) { ingenTreff.hidden = !sokAktiv || noeSynlig; }
        }

        if (sokFelt) {
            sokFelt.addEventListener("input", function () { utforSok(sokFelt.value); });
        }

        if (tilTilbakemelding) {
            tilTilbakemelding.addEventListener("click", function () {
                lukkPanel();
                var tbmMenyKnapp = document.getElementById("tbm-meny-tilbakemelding");
                if (tbmMenyKnapp) { tbmMenyKnapp.click(); }
            });
        }
    });
})();

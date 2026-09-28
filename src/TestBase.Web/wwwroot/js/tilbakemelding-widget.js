// Flytende tilbakemeldingsknapp — flyttbar, minimerbar, med skjema som auto-
// samler teknisk kontekst + best-effort skjermbilde (html2canvas, vendoret
// lokalt i js/vendor/ — se docs/beslutningslogg.md "Tilbakemeldingsverktøy").
// Selvstendig IIFE, ingen avhengighet til resten av appens JS.
(function () {
    "use strict";

    var LAGRINGSNOKKEL_POSISJON = "tbm_posisjon_v1";
    var LAGRINGSNOKKEL_MINIMERT = "tbm_minimert_v1";
    var DRA_TERSKEL_PX = 6;
    var FEIL_GYLDIGHET_MS = 10 * 60 * 1000;

    // --- Global feilfangst (aktiv på ALLE sider, uavhengig av om widgeten
    // faktisk åpnes) — siste feil legges automatisk ved i skjemaet hvis
    // panelet åpnes kort tid etterpå. Svelger aldri feilen selv (ingen
    // preventDefault) — kun observerer.
    window.__tbmSisteFeil = null;
    window.addEventListener("error", function (e) {
        window.__tbmSisteFeil = {
            tekst: "JS-feil: " + (e.message || "(ukjent)") + (e.filename ? " (" + e.filename + ":" + e.lineno + ")" : ""),
            tidspunkt: Date.now()
        };
    });
    window.addEventListener("unhandledrejection", function (e) {
        var grunn = e.reason && e.reason.message ? e.reason.message : String(e.reason);
        window.__tbmSisteFeil = { tekst: "Ufanget promise-avvisning: " + grunn, tidspunkt: Date.now() };
    });

    document.addEventListener("DOMContentLoaded", function () {
        var rot = document.getElementById("tbm-rot");
        if (!rot) { return; }

        var mini = document.getElementById("tbm-mini");
        var fabWrap = document.getElementById("tbm-fab-wrap");
        var fab = document.getElementById("tbm-fab");
        var skjulKnapp = document.getElementById("tbm-skjul");
        var meny = document.getElementById("tbm-meny");
        var menyTilbakemelding = document.getElementById("tbm-meny-tilbakemelding");
        var panel = document.getElementById("tbm-panel");
        var panelLukk = document.getElementById("tbm-panel-lukk");
        var skjema = document.getElementById("tbm-skjema");
        var meldingFelt = document.getElementById("tbm-melding");
        var feilVarsel = document.getElementById("tbm-feil-varsel");
        var skjermbildeTekst = document.getElementById("tbm-skjermbilde-tekst");
        var skjermbildeForhandsvisning = document.getElementById("tbm-skjermbilde-forhandsvisning");
        var sendKnapp = document.getElementById("tbm-send");
        var kvittering = document.getElementById("tbm-kvittering");
        var feilmelding = document.getElementById("tbm-feilmelding");

        var gjeldendeSkjermbilde = null;
        var gjeldendeTekniskFeil = null;

        // --- Minimert/posisjon-tilstand ------------------------------------
        function lesLagretPosisjon() {
            try {
                var rå = localStorage.getItem(LAGRINGSNOKKEL_POSISJON);
                return rå ? JSON.parse(rå) : null;
            } catch (e) { return null; }
        }
        function lagrePosisjon(pos) {
            try { localStorage.setItem(LAGRINGSNOKKEL_POSISJON, JSON.stringify(pos)); } catch (e) { /* ignorer (privat modus mv.) */ }
        }
        function settMinimert(minimert) {
            try { localStorage.setItem(LAGRINGSNOKKEL_MINIMERT, minimert ? "1" : "0"); } catch (e) { /* ignorer */ }
            mini.hidden = !minimert;
            fabWrap.hidden = minimert;
            if (minimert) {
                lukkMeny();
                lukkPanel();
            }
        }

        function klemPosisjonInnenforVindu(left, top) {
            var bredde = fabWrap.offsetWidth || 64;
            var hoyde = fabWrap.offsetHeight || 64;
            var maksLeft = Math.max(0, window.innerWidth - bredde);
            var maksTop = Math.max(0, window.innerHeight - hoyde);
            return { left: Math.min(Math.max(0, left), maksLeft), top: Math.min(Math.max(0, top), maksTop) };
        }

        var lagretPos = lesLagretPosisjon();
        if (lagretPos && typeof lagretPos.left === "number" && typeof lagretPos.top === "number") {
            var klemt = klemPosisjonInnenforVindu(lagretPos.left, lagretPos.top);
            fabWrap.style.left = klemt.left + "px";
            fabWrap.style.top = klemt.top + "px";
            fabWrap.style.right = "auto";
            fabWrap.style.bottom = "auto";
        }

        try {
            if (localStorage.getItem(LAGRINGSNOKKEL_MINIMERT) === "1") {
                settMinimert(true);
            }
        } catch (e) { /* ignorer */ }

        window.addEventListener("resize", function () {
            if (fabWrap.style.left) {
                var pos = klemPosisjonInnenforVindu(parseFloat(fabWrap.style.left), parseFloat(fabWrap.style.top));
                fabWrap.style.left = pos.left + "px";
                fabWrap.style.top = pos.top + "px";
            }
        });

        // --- Dra-og-slipp (pointer events — dekker mus og touch) -----------
        var draStart = null; // { pekerX, pekerY, startLeft, startTop }
        var draFlyttet = false;

        fab.addEventListener("pointerdown", function (e) {
            if (e.button !== undefined && e.button !== 0) { return; }
            var rect = fabWrap.getBoundingClientRect();
            draStart = { pekerX: e.clientX, pekerY: e.clientY, startLeft: rect.left, startTop: rect.top };
            draFlyttet = false;
            fab.setPointerCapture(e.pointerId);
        });

        fab.addEventListener("pointermove", function (e) {
            if (!draStart) { return; }
            var dx = e.clientX - draStart.pekerX;
            var dy = e.clientY - draStart.pekerY;
            if (!draFlyttet && Math.hypot(dx, dy) < DRA_TERSKEL_PX) { return; }
            draFlyttet = true;

            var pos = klemPosisjonInnenforVindu(draStart.startLeft + dx, draStart.startTop + dy);
            fabWrap.style.left = pos.left + "px";
            fabWrap.style.top = pos.top + "px";
            fabWrap.style.right = "auto";
            fabWrap.style.bottom = "auto";
        });

        fab.addEventListener("pointerup", function (e) {
            if (!draStart) { return; }
            fab.releasePointerCapture(e.pointerId);
            if (draFlyttet) {
                lagrePosisjon({ left: parseFloat(fabWrap.style.left), top: parseFloat(fabWrap.style.top) });
            } else {
                apneEllerLukkMeny();
            }
            draStart = null;
        });

        // --- Minimer/gjenåpne ------------------------------------------------
        skjulKnapp.addEventListener("click", function (e) {
            e.stopPropagation();
            settMinimert(true);
        });
        mini.addEventListener("click", function () {
            settMinimert(false);
        });

        // --- Rollup-meny -------------------------------------------------------
        function apneEllerLukkMeny() {
            var apen = !meny.hidden;
            if (apen) { lukkMeny(); } else { apneMeny(); }
        }
        function apneMeny() {
            meny.hidden = false;
            fab.setAttribute("aria-expanded", "true");
        }
        function lukkMeny() {
            meny.hidden = true;
            fab.setAttribute("aria-expanded", "false");
        }

        document.addEventListener("click", function (e) {
            if (!fabWrap.contains(e.target) && !panel.contains(e.target)) {
                lukkMeny();
            }
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape") {
                lukkMeny();
                lukkPanel();
            }
        });

        menyTilbakemelding.addEventListener("click", function () {
            lukkMeny();
            apnePanel();
        });

        // --- Tilbakemeldingspanel ---------------------------------------------
        function apnePanel() {
            panel.hidden = false;
            meldingFelt.value = "";
            kvittering.hidden = true;
            feilmelding.hidden = true;
            sendKnapp.disabled = false;
            sendKnapp.textContent = "Send inn";
            meldingFelt.focus();

            settOppTekniskFeilFraKontekst();
            taSkjermbilde();
        }
        function lukkPanel() {
            panel.hidden = true;
        }
        panelLukk.addEventListener("click", lukkPanel);

        function settOppTekniskFeilFraKontekst() {
            gjeldendeTekniskFeil = null;
            feilVarsel.hidden = true;

            var erFeilside = /\/Error(\/|$)/i.test(location.pathname);
            if (erFeilside) {
                var reqIdEl = document.getElementById("tbm-server-feil-id");
                var reqId = reqIdEl ? reqIdEl.textContent : "(ukjent)";
                gjeldendeTekniskFeil = "Serverfeil (500). Request-ID: " + reqId + ". Forrige side: " + (document.referrer || "(ukjent)");
                feilVarsel.hidden = false;
                return;
            }

            var siste = window.__tbmSisteFeil;
            if (siste && (Date.now() - siste.tidspunkt) < FEIL_GYLDIGHET_MS) {
                gjeldendeTekniskFeil = siste.tekst;
                feilVarsel.hidden = false;
            }
        }

        function taSkjermbilde() {
            gjeldendeSkjermbilde = null;
            skjermbildeForhandsvisning.hidden = true;
            skjermbildeTekst.textContent = "Tar skjermbilde …";

            if (typeof html2canvas !== "function") {
                skjermbildeTekst.textContent = "Skjermbilde ikke tilgjengelig.";
                return;
            }

            // Skjul selve widgeten under capture, slik at skjermbildet viser SIDEN,
            // ikke vårt eget panel som ligger oppå den.
            rot.style.visibility = "hidden";

            html2canvas(document.body, {
                logging: false,
                useCORS: true,
                scale: 1,
                x: window.scrollX,
                y: window.scrollY,
                width: window.innerWidth,
                height: window.innerHeight,
                windowWidth: window.innerWidth,
                windowHeight: window.innerHeight
            }).then(function (canvas) {
                rot.style.visibility = "";
                try {
                    gjeldendeSkjermbilde = canvas.toDataURL("image/jpeg", 0.75);
                    skjermbildeForhandsvisning.src = gjeldendeSkjermbilde;
                    skjermbildeForhandsvisning.hidden = false;
                    skjermbildeTekst.textContent = "Skjermbilde vedlagt automatisk:";
                } catch (e) {
                    skjermbildeTekst.textContent = "Kunne ikke ta skjermbilde.";
                }
            }).catch(function () {
                rot.style.visibility = "";
                skjermbildeTekst.textContent = "Kunne ikke ta skjermbilde.";
            });
        }

        skjema.addEventListener("submit", function (e) {
            e.preventDefault();
            if (!meldingFelt.value.trim()) { return; }

            sendKnapp.disabled = true;
            sendKnapp.textContent = "Sender …";
            feilmelding.hidden = true;

            var data = {
                melding: meldingFelt.value.trim(),
                url: location.href,
                brukerAgent: navigator.userAgent,
                skjermBredde: screen.width,
                skjermHoyde: screen.height,
                vindaugBredde: window.innerWidth,
                vindaugHoyde: window.innerHeight,
                tekniskFeilInfo: gjeldendeTekniskFeil,
                screenshotDataUrl: gjeldendeSkjermbilde
            };

            fetch("/api/tilbakemelding", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(data)
            }).then(function (resp) {
                if (!resp.ok) { throw new Error("HTTP " + resp.status); }
                kvittering.hidden = false;
                setTimeout(lukkPanel, 1800);
            }).catch(function () {
                feilmelding.hidden = false;
                sendKnapp.disabled = false;
                sendKnapp.textContent = "Send inn";
            });
        });
    });
})();

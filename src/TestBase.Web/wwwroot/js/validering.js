// Rammer inn manglende/ugyldige obligatoriske felt i rødt når brukeren
// forsøker å sende inn et skjema — ikke før (native ":invalid" alene ville
// vist rødt fra første sidevisning, før brukeren har rukket å skrive noe).
// Gjelder ALLE <form>-er på siden automatisk, se docs/beslutningslogg.md.
document.addEventListener("submit", function (e) {
    var form = e.target;
    if (!(form instanceof HTMLFormElement) || form.noValidate) {
        return;
    }

    if (!form.checkValidity()) {
        e.preventDefault();
        e.stopPropagation();
        form.classList.add("skjema-forsokt-sendt");
        var forsteUgyldige = form.querySelector(":invalid");
        if (forsteUgyldige) {
            forsteUgyldige.focus();
            forsteUgyldige.scrollIntoView({ behavior: "smooth", block: "center" });
        }
    }
}, true);

document.addEventListener("input", function (e) {
    var form = e.target.closest ? e.target.closest("form") : null;
    if (form && form.classList.contains("skjema-forsokt-sendt") && form.checkValidity()) {
        form.classList.remove("skjema-forsokt-sendt");
    }
});

// Deaktiverer submit-knappen med det samme og viser en "…"-tekst, for skjemaer
// som utløser en treg og/eller ikke-idempotent handling (sender SMS/e-post,
// oppretter en konto) — hindrer gjentatte klikk fra å sende samme handling
// flere ganger mens siden laster (bugliste 2026-09-13 punkt 25). Legges på med
// attributtet data-disable-on-submit="<tekst vist mens den laster>".
document.addEventListener("submit", function (e) {
    var form = e.target;
    if (!(form instanceof HTMLFormElement) || !form.dataset.disableOnSubmit) {
        return;
    }

    if (!form.checkValidity()) {
        return;
    }

    var alleKnapper = form.querySelectorAll("button[type='submit'], input[type='submit']");
    if (alleKnapper.length === 0) {
        return;
    }

    // Skjemaer med FLERE innsendingsknapper (f.eks. Forrige/Lagre/Neste på
    // Pasientportal/Tester/Fyll) skal vise lastetekst KUN på knappen som
    // faktisk ble klikket (e.submitter), men likevel deaktivere ALLE — ellers
    // kan brukeren dobbelklikke en ANNEN knapp enn den som ble deaktivert.
    var klikketKnapp = e.submitter && alleKnapper.length && Array.prototype.includes.call(alleKnapper, e.submitter)
        ? e.submitter
        : alleKnapper[0];
    if (klikketKnapp.disabled) {
        return;
    }

    // Viser lastetekst på den klikkede knappen UMIDDELBART (visuell tilbakemelding),
    // men UTSETTER selve disabled=true til etter denne tasken — å deaktivere
    // knappen synkront her fjerner STILLE dens eget name=value-par (f.eks.
    // "Handling=Ferdig") fra selve innsendingen, fordi nettleseren bygger
    // skjemaets entry-list ut fra knappenes tilstand PÅ INNSENDINGSTIDSPUNKTET,
    // ikke før submit-eventet ble trigget. Så lenge den faktiske disabling skjer
    // via setTimeout (neste task), rekker nettleseren å lese knappens
    // navn/verdi FØRST — oppdaget 2026-09-20 da "Ferdig" på Pasientportal/
    // Tester/Fyll aldri markerte testen fullført i en ekte nettleser (kun
    // maskert i HeleFlytenTests.cs, som poster skjemadata direkte og aldri
    // kjører denne JS-en).
    var lastetekst = form.dataset.disableOnSubmit;
    if (klikketKnapp.tagName === "BUTTON") {
        klikketKnapp.dataset.opprinneligTekst = klikketKnapp.textContent;
        klikketKnapp.textContent = lastetekst;
    } else {
        klikketKnapp.dataset.opprinneligTekst = klikketKnapp.value;
        klikketKnapp.value = lastetekst;
    }
    setTimeout(function () {
        alleKnapper.forEach(function (knapp) {
            knapp.disabled = true;
        });
    }, 0);
}, true);

// Bekreftelse + enkelt regnestykke før permanent sletting — samme
// lav-kompleksitet-prinsipp som innloggingssidenes sikkerhetsspørsmål
// (se ICaptchaProvider), IKKE et tredjeparts-captcha. Skjemaet må ha et
// data-captcha-sporsmal-attributt og et skjult felt name="CaptchaSvar".
function bekreftSletting(form, hvaSlettes) {
    if (!window.confirm("Er du sikker på at du vil slette " + hvaSlettes + "? Dette skjuler raden for alle andre enn Superadmin.")) {
        return false;
    }

    var svar = window.prompt(form.dataset.captchaSporsmal || "Bekreft: hva er 2 + 2?");
    if (svar === null) {
        return false;
    }

    var felt = form.querySelector("input[name='CaptchaSvar']");
    if (felt) {
        felt.value = svar;
    }

    return true;
}

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

    var knapp = form.querySelector("button[type='submit'], input[type='submit']");
    if (!knapp || knapp.disabled) {
        return;
    }

    knapp.disabled = true;
    var lastetekst = form.dataset.disableOnSubmit;
    if (knapp.tagName === "BUTTON") {
        knapp.dataset.opprinneligTekst = knapp.textContent;
        knapp.textContent = lastetekst;
    } else {
        knapp.dataset.opprinneligTekst = knapp.value;
        knapp.value = lastetekst;
    }
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

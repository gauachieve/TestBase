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

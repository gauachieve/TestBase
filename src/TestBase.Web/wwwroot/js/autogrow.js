// Automatisk høyde-tilpasning for kommentarfelt på behandler-utfylte tester
// (FyllForPasient.cshtml) - vokser mens man skriver, på både PC og mobil.
// Selve dra-i-hjørnet-håndtaket er nativ nettleser-oppførsel (CSS resize:
// vertical, se .kommentarfelt i site.css) - dette scriptet dekker KUN
// auto-vekst forbi synlig høyde, som ikke finnes nativt.
(function () {
    function tilpassHoyde(felt) {
        felt.style.height = "auto";
        felt.style.height = felt.scrollHeight + "px";
    }

    function settOpp() {
        document.querySelectorAll("textarea.kommentarfelt").forEach(function (felt) {
            tilpassHoyde(felt);
            felt.addEventListener("input", function () { tilpassHoyde(felt); });
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", settOpp);
    } else {
        settOpp();
    }
})();

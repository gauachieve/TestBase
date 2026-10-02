namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// HCR-20 V3 (Historical, Clinical, Risk Management-20, versjon 3) — Douglas, Hart, Webster &amp;
/// Belfrage (2013), et STRUKTURERT PROFESJONELT SKJØNN-verktøy (SPJ) for voldsrisikovurdering,
/// utgitt/distribuert i Norge av SIFER (Nasjonalt kompetansenettverk for sikkerhets-, fengsels- og
/// rettspsykiatri, Helse Bergen). Kliniker-administrert — bruker "behandler fyller ut"-mekanismen
/// (se docs/beslutningslogg.md), sendes ALDRI til pasienten.
///
/// KILDE OG LISENS (viktig, les før bruk): de 20 faktornavnene, deres a/b/c-underpunkter, og selve
/// vurderingsskalaen (opprinnelig Tilstede: Ukjent/Nei/Delvis/Ja + separat Relevans: Ukjent/Lav/
/// Moderat/Høy — samt Trinn 7 sin Lav/Moderat/Høy-konklusjon) er hentet fra SIFERs EGET, FRITT
/// NEDLASTBARE norske
/// "Arbeidsskjema til HCR-20v3" (sifer.no/verktoy, lenket fra Helsebiblioteket), IKKE fra det
/// betalte brukermanual-heftet. Brukeren har selv verifisert at selve arbeidsskjemaet/strukturen er
/// fritt tilgjengelig og fritt til bruk, og at det er BRUKERMANUALEN (kr. 250,- per bruker, kjøpes
/// hos SIFER) som er betalt — manualen inneholder de detaljerte kodingskriteriene (hva som konkret
/// teller som "Ja" vs. "Delvis" for hvert ledd, kapittel 3 sin veiledning for risikoformulering) som
/// vi IKKE har og IKKE gjengir her. Denne testen er derfor et arbeidsverktøy som følger den ekte,
/// fritt distribuerte strukturen — men klinikeren som fyller den ut MÅ ha egen kompetanse/manual for
/// å kode hvert ledd forsvarlig, akkurat som ved utfylling av papirskjemaet.
///
/// RETTET 2026-10-02 (brukerens beslutning): de 20 faktorene ble opprinnelig seedet som TO separate
/// ledd hver (Tilstede + Relevans, hver sin skala) — slått sammen til ÉN kombinert "Tilstede og
/// relevans for fremtidig risiko"-vurdering per faktor (kun Relevans-skalaen Ukjent/Lav/Moderat/Høy
/// beholdt), siden verktøyet uansett ikke summerer — en bevisst forenkling av dataregistreringen, på
/// bekostning av å ikke lenger kunne registrere "faktoren var historisk til stede MEN er ikke lenger
/// relevant" (eller omvendt) som to atskilte svar. Se Hcr20V3Skaaringsberegner for tilsvarende endring.
///
/// BEVISST IKKE et sumskår-verktøy (samme prinsipp som CORE-A): den faktiske risikokonklusjonen er
/// klinikerens EGEN strukturerte skjønnsmessige vurdering i Trinn 7 (tre uavhengige Lav/Moderat/Høy-
/// vurderinger — fremtidig vold, alvorlig skade, umiddelbar vold — pluss en Annen risiko-vurdering),
/// ALDRI en automatisk utregning fra antall "Ja"/"Relevant"-avkrysninger. Trinn 4-6 (risikoformulering,
/// voldsscenarier, håndteringsstrategier) er forenklet til fritekstfelt — det virkelige skjemaet har
/// flerkolonne-tabeller (inntil 3 scenarier × 5 spørsmål hver) som dagens generiske testmotor ikke
/// støtter; klinikeren skriver dette som løpende tekst i stedet.
/// </summary>
public sealed class Hcr20V3TestSeeder : IInnebygdTestSeeder
{
    public string Kode => "hcr20_v3";

    private const string RelevansSkala = "0:Ukjent,1:Lav,2:Moderat,3:Høy";
    private const string Trinn7Skala = "0:Lav,1:Moderat,2:Høy";
    private const string AnnenRisikoSkala = "0:Nei,1:Mulig,2:Ja";

    private sealed record Faktor(string Kode, string Navn, string? Underpunkter);

    private static readonly Faktor[] HistoriskeFaktorer =
    {
        new("H1", "Vold", "a. Barn (≤12 år), b. Ungdom (13-17), c. Voksen (18+)"),
        new("H2", "Annen antisosial atferd", "a. Barn (≤12 år), b. Ungdom (13-17), c. Voksen (18+)"),
        new("H3", "Relasjoner", "a. Intime relasjoner, b. Andre relasjoner"),
        new("H4", "Arbeid og utdanning", null),
        new("H5", "Rusmiddelbruk", null),
        new("H6", "Alvorlig psykisk lidelse", "a. Psykoselidelser, b. Alvorlige stemningslidelser, c. Annen alvorlig psykisk lidelse (avklart/foreløpig)"),
        new("H7", "Personlighetsforstyrrelse", "a. Antisosial/psykopatisk/dyssosial, b. Andre (avklart/foreløpig)"),
        new("H8", "Traumatiske opplevelser", "a. Viktimisering/traume, b. Omsorgssvikt i oppveksten"),
        new("H9", "Voldelige holdninger", null),
        new("H10", "Respons på behandling og tilsyn", null)
    };

    private static readonly Faktor[] KliniskeFaktorer =
    {
        new("C1", "Innsikt", "a. Psykisk lidelse, b. Voldsrisiko, c. Behov for behandling"),
        new("C2", "Voldsforestillinger eller -intensjoner", null),
        new("C3", "Symptom på alvorlig psykisk lidelse", "a. Psykoselidelser, b. Alvorlige stemningslidelser, c. Annen alvorlig psykisk lidelse (avklart/foreløpig)"),
        new("C4", "Ustabilitet", "a. Affektiv, b. Atferdsmessig, c. Kognitiv"),
        new("C5", "Respons på behandling og tilsyn", "a. Samarbeid, b. Mottakelighet")
    };

    private static readonly Faktor[] RisikohandteringsFaktorer =
    {
        new("R1", "Bruk av offentlige tjenester", null),
        new("R2", "Boforhold og omgivelser", null),
        new("R3", "Personlig støtte", null),
        new("R4", "Respons på behandling og tilsyn", "a. Samarbeid, b. Mottakelighet"),
        new("R5", "Stress og mestring", null)
    };

    private const string Kategori = "Vold, selvmord og risikovurdering";

    private const string RapportIntroduksjonTekst =
        "HCR-20 V3 er et strukturert profesjonelt skjønn-verktøy (SPJ) for voldsrisikovurdering " +
        "(Douglas, Hart, Webster & Belfrage), distribuert i Norge av SIFER. IKKE et sumskår-verktøy — " +
        "den faktiske risikokonklusjonen er klinikerens egen strukturerte vurdering i Trinn 7, vist " +
        "under. Strukturen følger SIFERs fritt tilgjengelige arbeidsskjema; de detaljerte " +
        "kodingskriteriene for hvert ledd krever den betalte brukermanualen (kjøpes hos SIFER).";

    public async Task SeedAsync(TestService testService, CancellationToken cancellationToken = default)
    {
        await testService.SikreStandardkategorierAsync(cancellationToken);

        var eksisterende = await testService.HentTestVedKodeAsync(Kode, cancellationToken);
        if (eksisterende is not null)
        {
            await testService.KoblTestTilKategoriAsync(eksisterende.Id, Kategori, cancellationToken);
            await testService.SettRapportIntroduksjonAsync(eksisterende.Id, RapportIntroduksjonTekst, cancellationToken);
            return;
        }

        var test = await testService.OpprettTestAsync(
            navn: "HCR-20 V3 (voldsrisikovurdering, kliniker)",
            beskrivelse: "Strukturert profesjonelt skjønn — vurder tilstedeværelse og relevans av hver risikofaktor basert på tilgjengelig informasjon, avslutt med din egen risikokonklusjon i Trinn 7. Krever egen kompetanse på HCR-20 V3 sin brukermanual for forsvarlig koding av hvert ledd.",
            belonningstekst: "Vurderingen er lagret.",
            kode: Kode,
            cancellationToken: cancellationToken);
        await testService.SettFyllesUtAvBehandlerAsync(test.Id, true, cancellationToken);

        var sideH = await testService.LeggTilSideAsync(test.Id, "Historiske faktorer", "Tidligere problemer med…", cancellationToken);
        foreach (var f in HistoriskeFaktorer)
        {
            var instruksjon = f.Underpunkter is null ? null : $"Omfatter: {f.Underpunkter}";
            await testService.LeggTilLeddAsync(sideH.Id, $"{f.Kode}. {f.Navn} — Tilstede og relevans for fremtidig risiko", instruksjon, TestSvartype.LikertSkala, RelevansSkala, cancellationToken);
        }

        var sideC = await testService.LeggTilSideAsync(test.Id, "Kliniske faktorer", "Nylige problemer med…", cancellationToken);
        foreach (var f in KliniskeFaktorer)
        {
            var instruksjon = f.Underpunkter is null ? null : $"Omfatter: {f.Underpunkter}";
            await testService.LeggTilLeddAsync(sideC.Id, $"{f.Kode}. {f.Navn} — Tilstede og relevans for fremtidig risiko", instruksjon, TestSvartype.LikertSkala, RelevansSkala, cancellationToken);
        }

        var sideR = await testService.LeggTilSideAsync(test.Id, "Risikohåndteringsfaktorer", "Fremtidige problemer med…", cancellationToken);
        foreach (var f in RisikohandteringsFaktorer)
        {
            var instruksjon = f.Underpunkter is null ? null : $"Omfatter: {f.Underpunkter}";
            await testService.LeggTilLeddAsync(sideR.Id, $"{f.Kode}. {f.Navn} — Tilstede og relevans for fremtidig risiko", instruksjon, TestSvartype.LikertSkala, RelevansSkala, cancellationToken);
        }

        var sideFormulering = await testService.LeggTilSideAsync(
            test.Id, "Risikoformulering, scenarier og håndtering (Trinn 4-6)",
            "Forenklet til fritekst — se HCR-20 V3 sin brukermanual kapittel 3 for veiledning til hvert punkt.",
            cancellationToken);
        await testService.LeggTilLeddAsync(sideFormulering.Id, "Risikoformulering (Trinn 4)",
            "Primære risikofaktorer, hvordan de henger sammen, og voldens funksjon.", TestSvartype.Fritekst, null, cancellationToken);
        await testService.LeggTilLeddAsync(sideFormulering.Id, "Voldsscenario(er) (Trinn 5)",
            "For hvert scenario: type, alvorlighet, umiddelbarhet, frekvens/varighet, sannsynlighet.", TestSvartype.Fritekst, null, cancellationToken);
        await testService.LeggTilLeddAsync(sideFormulering.Id, "Håndteringsstrategier (Trinn 6)",
            "Monitorering/tilsyn, behovstilpasset behandling, restriksjoner/kontroll, offerbeskyttelse, andre hensyn.", TestSvartype.Fritekst, null, cancellationToken);

        var sideKonklusjon = await testService.LeggTilSideAsync(test.Id, "Konklusjon og anbefaling (Trinn 7)", null, cancellationToken);
        await testService.LeggTilLeddAsync(sideKonklusjon.Id, "Fremtidig vold/prioritering",
            "Hvor omfattende innsats/tiltak kreves for å forhindre ytterligere vold?", TestSvartype.LikertSkala, Trinn7Skala, cancellationToken);
        await testService.LeggTilLeddAsync(sideKonklusjon.Id, "Alvorlig fysisk skade",
            "Risiko for at volden vil medføre eller utvikle seg til alvorlig/livstruende fysisk skade?", TestSvartype.LikertSkala, Trinn7Skala, cancellationToken);
        await testService.LeggTilLeddAsync(sideKonklusjon.Id, "Umiddelbar vold",
            "Risiko for vold i nær fremtid (timer/dager/uker)?", TestSvartype.LikertSkala, Trinn7Skala, cancellationToken);
        await testService.LeggTilLeddAsync(sideKonklusjon.Id, "Annen risiko",
            "Bevis for annen risiko (selvskade, selvmord, straffbare seksuelle handlinger) utenfor HCR-20 sin voldsdefinisjon?", TestSvartype.LikertSkala, AnnenRisikoSkala, cancellationToken);

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

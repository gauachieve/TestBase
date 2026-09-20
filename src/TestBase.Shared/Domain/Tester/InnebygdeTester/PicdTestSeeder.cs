namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Regenererer PiCD (Personality Inventory for ICD-11) — 60 ledd, 5
/// trekkdomener jf. ICD-11s dimensjonale personlighetsmodell. Original:
/// Oltmanns, J. R. &amp; Widiger, T. A. (2018). The Personality Inventory for
/// ICD-11: A Personality-Trait Model for the ICD-11 Dimensional Trait
/// Model Proposal. Psychological Assessment. DOI: 10.1037/pas0000459.
/// INGEN offisiell norsk oversettelse finnes (kun dansk, ved Bo Bach, Mickey
/// Kongerslev og Erik Simonsen, distribuert fritt via
/// https://www.personality.today/kopi-af-pds-icd-11-scales/) — denne norske
/// teksten er VÅR EGEN oversettelse (fra den danske, som er nært beslektet),
/// IKKE psykometrisk validert eller godkjent av rettighetshaverne. Merket via
/// Test.OversettelseNotat — VISES KUN til behandler/admin (Admin/Tester/Index,
/// tildelingsflytens sjekkliste), IKKE i selve testnavnet lenger (rettet
/// 2026-09-20 — pasienten skal se et kort, nøytralt navn, se
/// docs/beslutningslogg.md "ICD-11-tester, del 2"). En høflighets-e-post til
/// forfatterne før bredere bruk er ønskelig.
/// IKKE juridisk/klinisk kvalitetssikret av oss.
/// </summary>
public sealed class PicdTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "picd";

    /// <summary>Kort, pasientvennlig navn (2026-09-20) — se ItqTestSeeder.Navn for begrunnelse.</summary>
    public const string Navn = "Personlighetstrekk ICD-11 PiCD";

    private const string OversettelseNotatTekst = "Ikke offisielt oversatt – kun til uttesting";

    /// <summary>
    /// "Helt" i stedet for "Meget" og "Nøytralt" i stedet for "Nøytral"
    /// (rettet 2026-09-20 etter tilbakemelding) — se
    /// OppdaterSvaralternativerForAlleLeddAsync-kallet under, som propagerer
    /// denne endringen til ledd som allerede er seedet i et miljø.
    /// </summary>
    public const string Skala = "1:Helt uenig,2:Uenig,3:Nøytralt,4:Enig,5:Helt enig";

    private static readonly string[] Utsagn =
    {
        "Jeg er vanligvis en nervøs person",
        "Jeg har en tendens til å handle impulsivt",
        "Jeg foretrekker å holde meg unna andre mennesker",
        "Sinnet mitt har ført til at jeg har havnet i krangel med andre",
        "Jeg bruker mye tid på å organisere og planlegge",
        "Det påvirker meg mye når ting ikke går som planlagt",
        "Jeg er ikke en særlig ansvarlig person",
        "Andre sier at jeg ikke viser følelsene mine",
        "Jeg morer meg over andres problemer",
        "Jeg tar ingen sjanser",
        "Humøret mitt svinger mye i løpet av en uke",
        "Som person er jeg ikke særlig organisert",
        "Jeg er stille når jeg er sammen med andre",
        "Jeg har lett for å få andre til å gjøre som jeg ønsker",
        "Jeg tenker grundig gjennom ting før jeg handler",
        "Jeg blir mer nervøs enn andre når livet går meg imot",
        "Jeg tar forhastede beslutninger",
        "Jeg har ikke noe særlig tett forhold til noen",
        "Jeg er mye mer konkurransepreget enn andre",
        "Andre synes jeg er perfeksjonist",
        "Etter å ha hatt et problem tar det lang tid før jeg kommer tilbake til min normale tilstand",
        "Noen ganger forlater jeg jobben uten å informere kollegene mine",
        "Jeg føler i bunn og grunn det samme hele tiden",
        "Noen mennesker fortjener å være hjemløse",
        "Jeg tar alltid det sikre valget",
        "Forandringer i humøret mitt har ingen sammenheng med det som skjer i livet mitt",
        "Jeg kan være ganske slurvete og uorganisert",
        "Jeg kan best beskrives som «sjenert»",
        "Jeg bruker ofte sjarm på folk jeg egentlig ikke liker",
        "Jeg handler aldri impulsivt",
        "Andre mennesker legger merke til nervøsiteten min",
        "Jeg liker å handle først og tenke etterpå",
        "Andre sier at jeg er fjern og tilbaketrukket",
        "Jeg er alltid klar for en konflikt",
        "Jeg streber etter perfeksjon",
        "Jeg er følsom for kritikk eller fornærmelser",
        "Hvis jeg ikke har lyst, møter jeg ikke opp på jobb",
        "Jeg har svært sjelden følt meg begeistret",
        "Jeg bekymrer meg ikke om å såre andres følelser",
        "Det viktigste for meg er at jeg føler meg trygg og sikker",
        "Humøret mitt svinger ofte mellom tristhet og glede",
        "Jeg bekymrer meg ikke om å rekke ting i tide eller overholde planer",
        "Jeg sier mindre enn de fleste",
        "Jeg er manipulerende",
        "Jeg gjennomtenker enhver beslutning i detalj",
        "Jeg er ofte ganske redd uten at det er noen egentlig grunn til det",
        "Jeg gjør ofte ting uten å tenke meg om",
        "Jeg ville ikke hatt noe imot å leve helt alene uten kontakt med andre",
        "Jeg mestrer kunsten å konfrontere",
        "Jeg setter en ære i å utføre arbeid av høy kvalitet",
        "Jeg føler meg utsatt",
        "Jeg bruker penger på meg selv, selv om jeg har ubetalte regninger",
        "Jeg kjenner ikke følelser like sterkt som andre mennesker gjør",
        "Jeg kunne blitt en god soldat fordi jeg er likegyldig til å skade andre",
        "Jeg er vanligvis svært forsiktig og omhyggelig",
        "Humøret mitt svinger mye mer enn andres",
        "Jeg følger ingen fast rekkefølge eller plan når jeg jobber med noe",
        "Jeg er alltid tilbakeholden eller sky i sosiale sammenhenger",
        "Jeg har hatt suksess med å lure og manipulere andre",
        "Jeg elsker uttrykket «tenk før du handler»"
    };

    private const string Kategori = "Personlighet, relasjoner og sosial fungering";

    private const string RapportIntroduksjonTekst =
        "PiCD (Personality Inventory for ICD-11) er et selvrapporteringsinstrument som måler fem trekkdomener jf. " +
        "ICD-11s dimensjonale personlighetsmodell: negativ affektivitet, disinhibition, tilbaketrekning, " +
        "dyssosialitet og anankasme. 60 ledd (12 per domene), skala 1-5. Kan summeres eller gjennomsnittsberegnes " +
        "per domene — kilden oppgir ingen kliniske grenseverdier, dette er en dimensjonal trekkprofil, ikke et " +
        "screeninginstrument for diagnose. Referanse: Oltmanns, J. R. & Widiger, T. A. (2018). The Personality " +
        "Inventory for ICD-11. Psychological Assessment. DOI: 10.1037/pas0000459. " +
        "NB: Norsk tekst er IKKE en offisiell oversettelse — se klassekommentaren i PicdTestSeeder.";

    public static readonly IReadOnlyDictionary<string, int[]> Domener = new Dictionary<string, int[]>
    {
        ["Negativ affektivitet"] = new[] { 1, 6, 11, 16, 21, 26, 31, 36, 41, 46, 51, 56 },
        ["Disinhibition"] = new[] { 2, 7, 12, 17, 22, 27, 32, 37, 42, 47, 52, 57 },
        ["Tilbaketrekning"] = new[] { 3, 8, 13, 18, 23, 28, 33, 38, 43, 48, 53, 58 },
        ["Dyssosialitet"] = new[] { 4, 9, 14, 19, 24, 29, 34, 39, 44, 49, 54, 59 },
        ["Anankasme"] = new[] { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60 }
    };

    public async Task SeedAsync(TestService testService, CancellationToken cancellationToken = default)
    {
        await testService.SikreStandardkategorierAsync(cancellationToken);

        var eksisterende = await testService.HentTestVedKodeAsync(Kode, cancellationToken);
        if (eksisterende is not null)
        {
            await testService.KoblTestTilKategoriAsync(eksisterende.Id, Kategori, cancellationToken);
            await testService.SettRapportIntroduksjonAsync(eksisterende.Id, RapportIntroduksjonTekst, cancellationToken);
            await testService.SettIcdElleveKlarAsync(eksisterende.Id, true, cancellationToken);
            await testService.SettNavnAsync(eksisterende.Id, Navn, cancellationToken);
            await testService.SettOversettelseNotatAsync(eksisterende.Id, OversettelseNotatTekst, cancellationToken);
            await testService.OppdaterSvaralternativerForAlleLeddAsync(eksisterende.Id, Skala, cancellationToken);
            return;
        }

        var test = await testService.OpprettTestAsync(
            navn: Navn,
            beskrivelse: "Herunder vises noen utsagn som beskriver hvordan en person kan føle eller handle. Besvar " +
                         "hvert utsagn på en måte som beskriver deg best, ut fra en skala fra 1 til 5. Det er ingen " +
                         "riktige eller gale svar. Beskriv deg selv så ærlig og nøyaktig som mulig.",
            belonningstekst: "Takk for at du fylte ut PiCD. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Personlighetstrekk", null, cancellationToken);
        foreach (var utsagn in Utsagn)
        {
            await testService.LeggTilLeddAsync(side.Id, utsagn, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
        await testService.SettIcdElleveKlarAsync(test.Id, true, cancellationToken);
        await testService.SettOversettelseNotatAsync(test.Id, OversettelseNotatTekst, cancellationToken);
    }
}

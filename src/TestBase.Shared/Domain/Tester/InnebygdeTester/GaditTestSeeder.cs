namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Regenererer GADIT (Gaming Disorder Identification Test) — screening av
/// ICD-11 Gaming Disorder (6C51). Ordrett norsk tekst fra den offisielle
/// norske oversettelsen (2025, Tore Willy Lie, Trond Aspeland, Mona Finstad
/// m.fl.), hentet fra rettighetshaverens egen distribusjonsside
/// https://www.integrertbehandling.no/forside/kartleggingutredning/gadit.
/// Distribusjonsrettighet: "til ikke-kommersiell bruk i klinikk og
/// forskning" — kommersiell distribusjon skal avklares med Gary Chung Kai
/// Chan (c.chan4@uq.edu.au), rettighetshaver til selve instrumentet.
/// Offisielt oversatt — INGEN "ikke offisielt oversatt"-merking.
/// IKKE juridisk/klinisk kvalitetssikret av oss.
/// </summary>
public sealed class GaditTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "gadit";

    /// <summary>Kort, pasientvennlig navn (2026-09-20) — se ItqTestSeeder.Navn for begrunnelse.</summary>
    public const string Navn = "Spillavhengighet ICD-11 GADIT";

    public const string SkalaFrekvens = "4:Hver dag,3:De fleste dager,2:Noen dager,1:Sjelden,0:Aldri";

    private static readonly string[] FrekvensSporsmal =
    {
        "Har noen andre sagt at du har problemer med å kontrollere gamingen din – enten du var enig med dem eller ikke?",
        "Har du ikke vært i stand til å redusere tiden du har brukt på gaming – selv når andre har bedt deg om å spille mindre?",
        "Har du stått i fare for å miste en viktig relasjon på grunn av gaming?",
        "Har du vært så oppslukt i gaming at du har glemt å spise?",
        "Har du fortsatt å game selv om du visste det ga problemer med familie eller venner?",
        "Har du fortsatt å game selv om det har påvirket helsen – for eksempel skuldersmerter, dårlig syn?"
    };

    private static readonly string[] JaNeiSporsmal =
    {
        "Har gaming forårsaket tydelig psykisk stress for noen av dine nærmeste?",
        "Ingenting annet enn gaming interesserer meg."
    };

    private const string Kategori = "Rus og avhengighet";

    private const string RapportIntroduksjonTekst =
        "GADIT (Gaming Disorder Identification Test) er et screeningverktøy for problemfylt gaming siste 12 " +
        "måneder, i tråd med ICD-11 Gaming Disorder (6C51). 8 spørsmål, maks skår 8. Skår på 5 eller mer antyder " +
        "at kriteriene for ICD-11 Gaming Disorder er oppfylt (Chan et al., 2024) — GADIT kan ikke alene brukes for " +
        "å sette en diagnose. Referanse: The Gaming Disorder Identification Test (GADIT) – A screening tool for " +
        "Gaming Disorder based on ICD-11 (2024).";

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
            return;
        }

        var test = await testService.OpprettTestAsync(
            navn: Navn,
            beskrivelse: "Vennligst velg et passende svar for hvert spørsmål om din gaming i løpet av de siste 12 månedene.",
            belonningstekst: "Takk for at du fylte ut GADIT. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Gaming siste 12 måneder", null, cancellationToken);

        foreach (var sporsmal in FrekvensSporsmal)
        {
            await testService.LeggTilLeddAsync(side.Id, sporsmal, null, TestSvartype.LikertSkala, SkalaFrekvens, cancellationToken);
        }

        foreach (var sporsmal in JaNeiSporsmal)
        {
            await testService.LeggTilLeddAsync(side.Id, sporsmal, null, TestSvartype.JaNei, null, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
        await testService.SettIcdElleveKlarAsync(test.Id, true, cancellationToken);
    }
}

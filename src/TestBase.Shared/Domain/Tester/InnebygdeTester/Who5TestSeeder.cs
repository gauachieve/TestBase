namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Regenererer WHO-5 (5 spørsmål om trivsel og velvære), jf. fase 5 i
/// beslutningsloggen. Ordrett norsk tekst fra WHOs offisielle norske
/// oversettelse (Bakke, 2004, versjon 1.1) — se kildehenvisning der.
/// IKKE juridisk/klinisk kvalitetssikret av oss; samme forbehold som
/// brukeravtalene og DPIA-utkastet.
/// </summary>
public sealed class Who5TestSeeder : IInnebygdTestSeeder
{
    public string Kode => "who5";

    private const string Stem = "I de siste to ukene har jeg …";

    // Rekkefølge som i originaldokumentet: høyeste verdi først.
    private const string Skala =
        "5:Hele tiden,4:Det meste av tiden,3:Mer enn halve tiden,2:Mindre enn halve tiden,1:Av og til,0:Aldri";

    private static readonly string[] Sporsmal =
    {
        "… følt meg glad og i godt humør",
        "… følt meg rolig og avslappet",
        "… følt meg aktiv og sterk",
        "… følt meg opplagt og uthvilt når jeg våkner",
        "… følt at mitt daglige liv har vært fylt av ting som interesserer meg"
    };

    private const string Kategori = "Kjerne";

    /// <summary>
    /// Fritt oversatt fra WHOs engelske instrumentbeskrivelse (ikke selve
    /// spørsmålsteksten, som allerede er den offisielle norske oversettelsen
    /// — se klassekommentaren) — vist i rapportens sammendrag, IKKE til
    /// pasienten under utfylling (se Test.RapportIntroduksjon).
    /// </summary>
    private const string RapportIntroduksjonTekst =
        "WHO-5 er et selvrapporteringsinstrument som måler mental velvære. Det består av fem " +
        "utsagn knyttet til de siste to ukene. Hvert utsagn skåres på en 6-punkts skala, hvor " +
        "høyere skår indikerer bedre mental velvære. Instrumentet er oversatt til over 30 språk.";

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
            navn: "WHO-5 (5 spørsmål om trivsel og velvære)",
            beskrivelse: "Ved å svare på spørsmålene nedenfor kan du gi oss et bilde av hvor bra eller " +
                         "dårlig du føler deg for tiden. Sett en sirkel rundt (velg) det svaret som passer " +
                         "best for hver uttalelse om hvordan du for det meste har følt deg gjennom de " +
                         "siste to ukene. Høyere tall betyr bedre trivsel og velvære.",
            belonningstekst: "Takk for at du fylte ut WHO-5. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Trivsel og velvære", Stem, cancellationToken);

        foreach (var sporsmalstekst in Sporsmal)
        {
            await testService.LeggTilLeddAsync(
                side.Id, sporsmalstekst, instruksjon: null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

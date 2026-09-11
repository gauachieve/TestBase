namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// IPDS (Iowa Personality Disorder Screen) — norsk oversettelse (2011) ved
/// Olssøn, Sørebø, Dahl. Original engelsk: © 1999 Bruce Pfohl, M.D. og
/// Douglas R. Langbehn, Ph.D., University of Iowa. Kilde:
/// https://www.oslo-universitetssykehus.no/49d8b0/contentassets/058bea9b314f4a64b0a9bd4a0cb307a9/iowa-ipds_11_r.pdf
/// (hentet og lest i sin helhet 2026-09-12, se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\IPDS.md).
/// "Papirversjon fritt tilgjengelig" ifølge kildedokumentet. IKKE juridisk/
/// klinisk kvalitetssikret av oss utover dette, se docs/beslutningslogg.md.
/// </summary>
public sealed class IpdsTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "ipds";

    private const string Stem =
        "Nedenfor finner du noen spørsmål om vanlige tanker og følelser folk kan ha. Hvis du har vært " +
        "annerledes enn vanlig de siste ukene eller månedene, så tenk tilbake på den tiden da du var ditt " +
        "vanlige jeg når du svarer.";

    private static readonly string[] Sporsmal =
    {
        "Noen mennesker opplever at humøret forandrer seg ofte – som om de hver dag var på en følelsesmessig berg-og-dal-bane. For eksempel, kan de svinge fra å føle seg sinte til deprimerte eller engstelige mange ganger om dagen. Passer dette på deg?",
        "Noen folk foretrekker å være i sentrum for oppmerksomheten, mens andre liker å holde seg i bakgrunnen. Plager det deg hvis noen andre er i sentrum?",
        "Insisterer du ofte på å få det som du vil ha det med en gang, selv om det å vente litt ville gi deg noe som var mye bedre?",
        "Synes du at folk flest utnytter deg, hvis du lar dem få vite for mye om deg?",
        "Føler du deg vanligvis nervøs eller engstelig sammen med andre?",
        "Unngår du å bli kjent med folk fordi du er redd for at de ikke vil like deg?",
        "Endrer du stadig måten å presentere deg på fordi du ikke vet hvem du virkelig er?",
        "Blir du sint eller irritert over at andre ikke anerkjenner dine spesielle talenter og prestasjoner så mye som de burde?",
        "Mistenker du ofte folk du kjenner for å ville narre deg eller utnytte deg?",
        "Har du en tendens til å bære nag eller straffe folk med taushet hvis de har krenket deg?",
        "Blir du irritert når venner eller familie klager over sine problemer?"
    };

    private const string Kategori = "Personlighet, relasjoner og sosial fungering";

    private const string RapportIntroduksjonTekst =
        "IPDS (Iowa Personality Disorder Screen) er et kort screeninginstrument for personlighetsforstyrrelse " +
        "— 11 ja/nei-spørsmål om vanlige tanke- og følelsesmønstre. Ikke et diagnostisk verktøy alene.";

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
            navn: "IPDS (Iowa) – screeninginstrument for personlighetsforstyrrelser",
            beskrivelse: Stem,
            belonningstekst: "Takk for at du fylte ut IPDS. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Personlighetsscreening", Stem, cancellationToken);

        foreach (var sporsmalstekst in Sporsmal)
        {
            await testService.LeggTilLeddAsync(
                side.Id, sporsmalstekst, instruksjon: null, TestSvartype.JaNei, svaralternativer: null, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

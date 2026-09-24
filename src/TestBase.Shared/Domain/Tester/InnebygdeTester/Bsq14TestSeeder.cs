namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// BSQ-14 (Body Shape Questionnaire, kortversjon) — kortversjonen av Cooper,
/// Taylor, Cooper &amp; Fairburn (1987) sitt opprinnelige 34-ledds BSQ, laget
/// av Evans &amp; Dolan (1993). Mye brukt i forskning/klinisk praksis for
/// kroppsbildeforstyrrelse. Norsk oversettelse her er egen, IKKE hentet fra
/// en spesifikk sitert norsk kilde ennå, og opphavsretten til selve BSQ ligger
/// hos originalforfatterne — bør kvalitetssikres/lisensavklares før reell
/// klinisk bruk. Se docs/beslutningslogg.md.
/// </summary>
public sealed class Bsq14TestSeeder : IInnebygdTestSeeder
{
    public string Kode => "bsq14";

    private const string Skala = "1:Aldri,2:Sjelden,3:Av og til,4:Ofte,5:Svært ofte,6:Alltid";

    private static readonly string[] Sporsmal =
    {
        "1. Har bekymring for kroppsformen din fått deg til å føle deg deprimert?",
        "2. Har du vært så bekymret for kroppsformen din at du følt du burde slanke deg?",
        "3. Har du følt at magen din er for stor?",
        "4. Har du følt deg mindre attraktiv på grunn av kroppsformen din?",
        "5. Har du følt deg skamfull over kroppen din?",
        "6. Har det å se kroppen din i speilet gjort deg opprørt?",
        "7. Har det å klype i «fettvalker» gjort deg opprørt?",
        "8. Har du følt at andre bedømmer kroppsformen din negativt i sosiale sammenhenger (f.eks. i svømmehall)?",
        "9. Har bekymring for kroppsformen din fått deg til å slanke deg?",
        "10. Har du følt deg mest ille til lags med kroppsformen din når du er sammen med tynnere personer?",
        "11. Har du følt deg engstelig for kroppsformen din når du er sammen med andre?",
        "12. Har du følt et sterkt behov for å slanke deg?",
        "13. Har det å se ditt eget speilbilde gjort deg svært bevisst på kroppsformen din?",
        "14. Har opplevelsen av at klærne sitter tett fått deg til å bli opptatt av kroppsformen din?"
    };

    private const string Kategori = "Spiseforstyrrelser og kroppsbilde";

    private const string RapportIntroduksjonTekst =
        "BSQ-14 (Body Shape Questionnaire, kortversjon) kartlegger bekymring for egen kroppsform de siste " +
        "4 ukene. 14 spørsmål, sumskår 14-84.";

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
            navn: "BSQ-14 (Kroppsbilde)",
            beskrivelse: "Vi vil vite hvordan du har følt deg om kroppsformen din de siste 4 ukene. Les hvert spørsmål og kryss av det svaret som passer best.",
            belonningstekst: "Takk for at du fylte ut BSQ-14. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Kroppsbilde", null, cancellationToken);
        foreach (var sporsmal in Sporsmal)
        {
            await testService.LeggTilLeddAsync(side.Id, sporsmal, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

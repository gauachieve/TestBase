namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Regenererer IAQ (International Anxiety Questionnaire) — 8 ledd, screening
/// av ICD-11 Generalized Anxiety Disorder. Engelsk originaltekst verifisert
/// direkte mot fagfellevurdert artikkel: Shevlin, M., Hyland, P., Butter, S.,
/// McBride, O., Hartman, T. K., Karatzias, T., & Bentall, R. P. (2023).
/// Journal of Clinical Psychology, 79, 854-870. Fritt tilgjengelig for alle
/// interesserte (se artikkelens konklusjon og
/// www.traumameasuresglobal.com/anxiety). INGEN offisiell norsk oversettelse
/// finnes ennå — rettighetshaver Philip Hyland ber eksplisitt om å bli
/// kontaktet før noen lager en oversettelse (høflighets-e-post planlagt
/// parallelt, se docs/beslutningslogg.md "ICD-11-tester"). Denne norske
/// teksten er VÅR EGEN oversettelse, IKKE psykometrisk validert. Merket via
/// Test.OversettelseNotat — vises kun til behandler/admin, IKKE i selve
/// testnavnet (rettet 2026-09-20). IKKE juridisk/klinisk kvalitetssikret av
/// oss.
/// </summary>
public sealed class IaqTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "iaq";

    /// <summary>Kort, pasientvennlig navn (2026-09-20) — se ItqTestSeeder.Navn for begrunnelse.</summary>
    public const string Navn = "Angstscreening ICD-11 IAQ";

    private const string OversettelseNotatTekst = "Ikke offisielt oversatt – kun til uttesting";

    public const string Skala = "0:Aldri,1:Noen få dager,2:Halvparten av dagene,3:De fleste dager,4:Hver dag";

    private static readonly string[] Sporsmal =
    {
        "Følt deg nervøs eller engstelig?",
        "Bekymret deg mye for forskjellige ting?",
        "Følt deg fysisk anspent eller urolig?",
        "Kjent hjertebank, pustevansker, ubehag i magen, eller tørr munn?",
        "Følt deg «på kanten» eller lettskremt?",
        "Hatt vanskelig for å konsentrere deg?",
        "Blitt lett irritert over forskjellige ting?",
        "Opplevd søvnforstyrrelser?"
    };

    private const string FunksjonSporsmal =
        "Har disse opplevelsene forårsaket problemer på personlige, familiemessige, sosiale, utdanningsmessige, " +
        "yrkesmessige eller andre viktige områder av livet ditt?";

    private const string Kategori = "Angst, tvang og relaterte plager";

    private const string RapportIntroduksjonTekst =
        "IAQ (International Anxiety Questionnaire) er et kort selvrapporteringsinstrument direkte utledet fra " +
        "ICD-11s diagnostiske beskrivelse av Generalisert angstlidelse. 8 symptomledd (0-4) pluss ett " +
        "funksjonsspørsmål (Ja/Nei). Diagnostisk algoritme: minst 4 av 8 ledd skåret 3 eller 4 (\"De fleste " +
        "dager\"/\"Hver dag\"), hvorav minst ett av de to første leddene, OG funksjonsspørsmålet besvart \"Ja\". " +
        "Referanse: Shevlin, M., Hyland, P., Butter, S., McBride, O., Hartman, T. K., Karatzias, T., & Bentall, " +
        "R. P. (2023). Journal of Clinical Psychology, 79, 854-870. NB: Norsk tekst er IKKE en offisiell " +
        "oversettelse — se klassekommentaren i IaqTestSeeder.";

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
            return;
        }

        var test = await testService.OpprettTestAsync(
            navn: Navn,
            beskrivelse: "I løpet av de siste månedene, hvor ofte har du hatt følgende følelser, tanker og atferd?",
            belonningstekst: "Takk for at du fylte ut IAQ. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Siste måneder", null, cancellationToken);
        foreach (var sporsmal in Sporsmal)
        {
            await testService.LeggTilLeddAsync(side.Id, sporsmal, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }
        await testService.LeggTilLeddAsync(side.Id, FunksjonSporsmal, null, TestSvartype.JaNei, null, cancellationToken);

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
        await testService.SettIcdElleveKlarAsync(test.Id, true, cancellationToken);
        await testService.SettOversettelseNotatAsync(test.Id, OversettelseNotatTekst, cancellationToken);
    }
}

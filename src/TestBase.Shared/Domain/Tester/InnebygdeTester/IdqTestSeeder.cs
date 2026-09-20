namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Regenererer IDQ (International Depression Questionnaire) — 9 ledd,
/// screening av ICD-11 Single Episode Depressive Disorder (6A70). Engelsk
/// originaltekst verifisert direkte mot fagfellevurdert artikkel: Shevlin,
/// M., Hyland, P., Butter, S., McBride, O., Hartman, T. K., Karatzias, T., &amp;
/// Bentall, R. P. (2023). Journal of Clinical Psychology, 79, 854-870.
/// Fritt tilgjengelig for alle interesserte (se artikkelens konklusjon og
/// www.traumameasuresglobal.com/depression). INGEN offisiell norsk
/// oversettelse finnes ennå — rettighetshaver Philip Hyland ber eksplisitt
/// om å bli kontaktet før noen lager en oversettelse (høflighets-e-post
/// planlagt parallelt, se docs/beslutningslogg.md "ICD-11-tester"). Denne
/// norske teksten er VÅR EGEN oversettelse, IKKE psykometrisk validert.
/// Merket via Test.OversettelseNotat — vises kun til behandler/admin, IKKE i
/// selve testnavnet (rettet 2026-09-20). IKKE juridisk/klinisk
/// kvalitetssikret av oss.
/// </summary>
public sealed class IdqTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "idq";

    /// <summary>Kort, pasientvennlig navn (2026-09-20) — se ItqTestSeeder.Navn for begrunnelse.</summary>
    public const string Navn = "Depresjonsscreening ICD-11 IDQ";

    private const string OversettelseNotatTekst = "Ikke offisielt oversatt – kun til uttesting";

    public const string Skala = "0:Aldri,1:Noen få dager,2:Halvparten av dagene,3:De fleste dager,4:Hver dag";

    private static readonly string[] Sporsmal =
    {
        "Følt deg nedfor eller deprimert store deler av dagen?",
        "Opplevd mindre interesse eller glede av vanlige aktiviteter store deler av dagen?",
        "Hatt vanskelig for å konsentrere deg?",
        "Hatt følelser av verdiløshet eller skyld?",
        "Følt deg håpløs?",
        "Hatt tilbakevendende tanker om død eller selvmord?",
        "Hatt endringer i appetitt eller søvn?",
        "Beveget deg saktere eller følt deg mer rastløs/urolig?",
        "Opplevd redusert energi eller utmattelse?"
    };

    private const string FunksjonSporsmal =
        "Har disse opplevelsene forårsaket problemer på personlige, familiemessige, sosiale, utdanningsmessige, " +
        "yrkesmessige eller andre viktige områder av livet ditt?";

    private const string Kategori = "Depresjon og bipolaritet";

    private const string RapportIntroduksjonTekst =
        "IDQ (International Depression Questionnaire) er et kort selvrapporteringsinstrument direkte utledet fra " +
        "ICD-11s diagnostiske beskrivelse av Single Episode Depressive Disorder (6A70). 9 symptomledd (0-4) pluss " +
        "ett funksjonsspørsmål (Ja/Nei). Diagnostisk algoritme: minst 5 av 9 ledd skåret 3 eller 4 (\"De fleste " +
        "dager\"/\"Hver dag\"), hvorav minst ett av de to første leddene, OG funksjonsspørsmålet besvart \"Ja\". " +
        "Referanse: Shevlin, M., Hyland, P., Butter, S., McBride, O., Hartman, T. K., Karatzias, T., & Bentall, " +
        "R. P. (2023). Journal of Clinical Psychology, 79, 854-870. NB: Norsk tekst er IKKE en offisiell " +
        "oversettelse — se klassekommentaren i IdqTestSeeder.";

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
            beskrivelse: "I løpet av de siste to ukene, hvor ofte har du hatt følgende følelser, tanker og atferd?",
            belonningstekst: "Takk for at du fylte ut IDQ. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Siste to uker", null, cancellationToken);
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

namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// VAS-variant av WHO-5 (jf. Who5TestSeeder) — samme fem utsagn og samme
/// tidsramme ("de siste to ukene"), men besvart med en glidebryter (visuell
/// analog skala, 0–100) i stedet for den 6-punkts Likert-skalaen. Laget for å
/// kunne tas GJENTATTE ganger av samme pasient med finere oppløsning enn de
/// seks diskrete Likert-trinnene tillater — en Likert-basert WHO-5 kan kun
/// endre seg i sprang på 4 prosentpoeng (0/4/8/…/100) fra én besvarelse til
/// neste, mens VAS gir en kontinuerlig 0–100-skår som fanger opp mindre,
/// men reelle endringer over tid. Endepunktene på glidebryteren er ORDRETT
/// de samme som Likert-skalaens ytterpunkter ("Aldri"/"Hele tiden") — se
/// Who5TestSeeder for kildehenvisning på selve spørsmålsteksten.
/// </summary>
public sealed class Who5VasTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "who5_vas";

    private const string Stem = "I de siste to ukene har jeg …";

    /// <summary>
    /// Kun de to ytterpunktene fra Who5TestSeeder sin Likert-skala
    /// ("5:Hele tiden" / "0:Aldri") — samme "verdi:tekst"-format som Likert
    /// bruker (TestLeddSvaralternativer.Parse), men med KUN 0 og 100 som
    /// nøkler siden glidebryteren selv dekker alt mellom dem.
    /// </summary>
    private const string VasEndepunkter = "0:Aldri,100:Hele tiden";

    private static readonly string[] Sporsmal =
    {
        "… følt meg glad og i godt humør",
        "… følt meg rolig og avslappet",
        "… følt meg aktiv og sterk",
        "… følt meg opplagt og uthvilt når jeg våkner",
        "… følt at mitt daglige liv har vært fylt av ting som interesserer meg"
    };

    private const string Kategori = "Funksjon, livskvalitet og behandlingsutfall";

    private const string RapportIntroduksjonTekst =
        "WHO-5 VAS er en variant av WHO-5 (mental velvære) der hvert utsagn besvares med en " +
        "glidebryter (visuell analog skala) i stedet for faste svaralternativer. Det gir en " +
        "kontinuerlig prosentskår som egner seg bedre til gjentatt måling over tid enn den " +
        "diskrete Likert-versjonen — se \"Utvikling over tid\" i rapporten når testen er tatt flere ganger.";

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
            navn: "WHO-5 VAS (trivsel og velvære, glidebryter for gjentatt måling)",
            beskrivelse: "Ved å svare på spørsmålene nedenfor kan du gi oss et bilde av hvor bra eller " +
                         "dårlig du føler deg for tiden. Dra glidebryteren til punktet som passer best for " +
                         "hver uttalelse om hvordan du for det meste har følt deg gjennom de siste to ukene — " +
                         "lenger til høyre betyr bedre trivsel og velvære.",
            belonningstekst: "Takk for at du fylte ut WHO-5 VAS. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Trivsel og velvære", Stem, cancellationToken);

        foreach (var sporsmalstekst in Sporsmal)
        {
            await testService.LeggTilLeddAsync(
                side.Id, sporsmalstekst, instruksjon: null, TestSvartype.VisuellAnalogSkala, VasEndepunkter, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

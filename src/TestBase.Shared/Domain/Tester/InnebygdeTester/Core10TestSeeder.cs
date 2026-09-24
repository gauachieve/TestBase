namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// CORE-10 (Clinical Outcomes in Routine Evaluation, kortversjon), opprinnelig
/// Evans, Connell, Barkham et al. (2002/2008, CORE System Trust, University of
/// Sheffield). Fritt tilgjengelig for klinisk bruk med registrering hos CORE
/// System Trust, men selve ORDLYDEN er opphavsrettslig beskyttet — denne
/// versjonen er en OMSKREVET/PARAFRASERT gjengivelse, IKKE en verbatim kopi
/// av det offisielle skjemaet. 10 ledd (0-4, siste uke), ledd 1 og 5 er
/// POSITIVT formulert og reverse-skåres. Ledd 3 og 10 er RISIKO-ledd
/// (selvskading/"livet ikke verdt å leve") og flagges alltid separat, samme
/// sikkerhetsprinsipp som SCL-25s selvmordsledd. Bør kvalitetssikres mot en
/// offisiell, lisensiert norsk oversettelse før reell klinisk bruk. Se
/// docs/beslutningslogg.md.
/// </summary>
public sealed class Core10TestSeeder : IInnebygdTestSeeder
{
    public string Kode => "core10";

    private const string Skala = "0:Ikke i det hele tatt,1:Av og til,2:Noen ganger,3:Ofte,4:Hele tiden";

    private static readonly string[] Sporsmal =
    {
        "1. Jeg har følt meg optimistisk med tanke på fremtiden.",
        "2. Jeg har følt meg svært anspent, engstelig eller nervøs.",
        "3. Jeg har hatt tanker om å skade meg selv.",
        "4. Jeg har følt meg helt alene.",
        "5. Jeg har følt meg fornøyd med tingene jeg har gjort.",
        "6. Jeg har blitt plaget av uønskede bilder eller minner.",
        "7. Jeg har følt at jeg ikke har noen å snakke fortrolig med.",
        "8. Jeg har følt meg overveldet av kravene i hverdagen.",
        "9. Jeg har hatt problemer med søvnen.",
        "10. Jeg har følt at livet ikke er verdt å leve."
    };

    private const string Kategori = "Funksjon, livskvalitet og behandlingsutfall";

    private const string RapportIntroduksjonTekst =
        "CORE-10 (Clinical Outcomes in Routine Evaluation, kortversjon) er et bredt mål på psykisk " +
        "distress siste uke, mye brukt for å følge behandlingsutfall over tid. Ledd 3 og 10 er " +
        "risikospørsmål (selvskading/«livet ikke verdt å leve») og flagges alltid separat.";

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
            navn: "CORE-10 (Psykisk distress)",
            beskrivelse: "Tenk over den siste uken og kryss av det svaret som passer best for hvert utsagn.",
            belonningstekst: "Takk for at du fylte ut CORE-10. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Siste uke", null, cancellationToken);
        foreach (var sporsmal in Sporsmal)
        {
            await testService.LeggTilLeddAsync(side.Id, sporsmal, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

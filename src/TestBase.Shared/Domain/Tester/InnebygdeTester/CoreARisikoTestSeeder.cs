namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// CORE-A (kort, frittstående risikoscreening) — brukerens forkortelse for
/// "det tredje CORE-skjemaet" er IKKE entydig i den offentlige CORE-
/// litteraturen (CORE System Trust bruker "CORE-A" om flere ting avhengig av
/// kilde). Denne testen er derfor en BEVISST, EGEN TOLKNING: et kort,
/// frittstående supplement til CORE-OM sitt innebygde 6-ledds risikodomene
/// (se CoreOmTestSeeder) — utvidet til 8 ledd for å gi mer klinisk
/// handlingsrettet informasjon (plan/tilgang til middel, ikke bare tanker) i
/// situasjoner der en rask, frittstående risikosjekk er ønskelig uten å
/// fylle ut hele CORE-OM. IKKE en gjengivelse av noe spesifikt, navngitt
/// offisielt CORE-A-dokument — bør avklares/kvalitetssikres mot brukerens
/// egen kilde før reell klinisk bruk. Se docs/beslutningslogg.md.
/// </summary>
public sealed class CoreARisikoTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "core_a";

    private const string Skala = "0:Ikke i det hele tatt,1:Av og til,2:Noen ganger,3:Ofte,4:Hele tiden";

    private static readonly string[] Sporsmal =
    {
        "1. Jeg har hatt tanker om at livet ikke er verdt å leve.",
        "2. Jeg har hatt tanker om å skade meg selv.",
        "3. Jeg har tenkt konkret på hvordan jeg ville skade meg selv eller ta mitt eget liv.",
        "4. Jeg har hatt tilgang til noe jeg kunne brukt til å skade meg selv alvorlig.",
        "5. Jeg har skadet meg selv fysisk.",
        "6. Jeg har hatt tanker om å skade en annen person.",
        "7. Jeg har tenkt konkret på hvordan jeg ville skade en annen person.",
        "8. Jeg har vært fysisk truende eller voldelig mot en annen person."
    };

    private const string Kategori = "Vold, selvmord og risikovurdering";

    private const string RapportIntroduksjonTekst =
        "CORE-A er en kort, frittstående risikoscreening (selvskading/selvmord + fare for andre) — " +
        "et supplement til CORE-OM sitt innebygde risikodomene, til bruk når en rask risikosjekk er " +
        "ønskelig alene. VIKTIG: ETHVERT ledd besvart over «Ikke i det hele tatt» krever klinisk " +
        "oppfølging, uavhengig av totalskår — dette er IKKE et sumskår-verktøy.";

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
            navn: "CORE-A (Risikoscreening)",
            beskrivelse: "Tenk over den siste uken og kryss av det svaret som passer best for hvert utsagn.",
            belonningstekst: "Takk for at du fylte ut CORE-A. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Risikoscreening", null, cancellationToken);
        foreach (var sporsmal in Sporsmal)
        {
            await testService.LeggTilLeddAsync(side.Id, sporsmal, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

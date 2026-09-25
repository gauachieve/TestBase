namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// YGTSS-R (Yale Global Tic Severity Scale, revidert), opprinnelig Leckman,
/// Riddle, Hardin et al. (1989) — et KLINIKER-ADMINISTRERT (ikke selvutfylt)
/// mål på tics ved Tourettes syndrom/tic-lidelser. Dette er den FØRSTE testen
/// som bruker den nye Test.FyllesUtAvBehandler-mekanismen (se
/// docs/beslutningslogg.md "Behandler-utfylte tester") — sendes ALDRI til
/// pasienten, fylles ut av behandler selv på Behandlerportal/Pasienter/
/// FyllForPasient etter klinisk observasjon/samtale med pasienten (og evt.
/// foresatte).
///
/// Struktur: sjekkliste over motoriske tics (ikke skåret, kun klinisk
/// kontekst) + 5 alvorlighetsdimensjoner for motoriske tics (antall,
/// frekvens, intensitet, kompleksitet, interferens, hver 0-5) = motorisk
/// delskår 0-25. Samme mønster for vokale/fonatoriske tics = fonatorisk
/// delskår 0-25. Total tic-alvorlighetsskår = motorisk + fonatorisk (0-50).
/// Funksjonsnedsettelse vurderes separat 0-50 (steg på 10). Total YGTSS-skår
/// = tic-alvorlighet + funksjonsnedsettelse (0-100).
///
/// De eksakte ordlyd-ankrene for hver alvorlighetsdimensjon er en EGEN,
/// klinisk rimelig gjengivelse (ikke verbatim fra en spesifikk sitert kilde)
/// — bør kvalitetssikres mot en offisiell norsk oversettelse før reell
/// klinisk bruk. Se docs/beslutningslogg.md.
/// </summary>
public sealed class YgtssRTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "ygtss_r";

    private static readonly string[] MotoriskSjekkliste =
    {
        "Øyeblunking eller andre øyebevegelser",
        "Grimasering i ansiktet",
        "Nesetrekking eller munnbevegelser",
        "Hoderykk eller hodevridning",
        "Skulderrykk",
        "Arm- eller håndbevegelser",
        "Mageanspenning eller bekkenbevegelser",
        "Ben-, fot- eller tåbevegelser",
        "Sammensatte/komplekse motoriske bevegelser (f.eks. berøring, hopping)",
        "Selvskadende motoriske tics"
    };

    private static readonly string[] FonatoriskSjekkliste =
    {
        "Grynting, harking eller snusing",
        "Hosting",
        "Plystring",
        "Dyrelyder",
        "Enkeltord eller stavelser gjentatt",
        "Endring i tonefall, rytme eller volum i tale",
        "Gjentakelse av andres ord (ekolali)",
        "Banning eller upassende ord (koprolali)"
    };

    private const string AntallSkala = "0:Ingen,1:Én tic-type,2:Flere enkle tic-typer,3:Multiple tic-typer (mer enn 5),4:Multiple typer inkludert minst én kompleks,5:Mange typer inkludert flere komplekse";
    private const string FrekvensSkala = "0:Ingen,1:Sjelden (ikke daglig),2:Daglig, men ikke gjennom hele dagen,3:Hyppig gjennom store deler av dagen,4:Nesten kontinuerlig,5:Kontinuerlig gjennom hele våkne tid";
    private const string IntensitetSkala = "0:Ingen,1:Minimal (knapt merkbar for andre),2:Mild,3:Moderat,4:Markert (tydelig kraftig),5:Ekstrem (kan medføre selvskade)";
    private const string KompleksitetSkala = "0:Ingen,1:Rene, enkle tics,2:Noe uklare/mellomliggende tics,3:Klart komplekse tics,4:Komplekse tics som ligner målrettet atferd,5:Komplekse tics som ser ut som meningsfulle handlinger eller ord";
    private const string InterferensSkala = "0:Ingen,1:Avbryter ikke atferd eller tale,2:Avbryter av og til,3:Avbryter ofte,4:Avbryter nesten kontinuerlig,5:Avbryter så mye at målrettet atferd/tale blir umulig";
    private const string FunksjonSkala = "0:Ingen funksjonsnedsettelse,10:Minimal,20:Mild,30:Moderat,40:Markert,50:Alvorlig (f.eks. behov for tilrettelegging/innleggelse)";

    private const string Kategori = "ADHD, autisme og nevroutvikling";

    private const string RapportIntroduksjonTekst =
        "YGTSS-R (Yale Global Tic Severity Scale) er en kliniker-administrert vurdering av " +
        "tic-alvorlighet ved Tourettes syndrom/tic-lidelser, fylt ut av behandler basert på " +
        "observasjon og samtale — IKKE av pasienten selv. Motorisk og fonatorisk delskår (hver " +
        "0-25) vurderes separat på 5 dimensjoner (antall, frekvens, intensitet, kompleksitet, " +
        "interferens), pluss en samlet funksjonsnedsettelsesvurdering (0-50).";

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
            navn: "YGTSS-R (Tic-alvorlighet, kliniker)",
            beskrivelse: "Basert på observasjon av og samtale med pasienten (og evt. foresatte), vurder følgende.",
            belonningstekst: "Vurderingen er lagret.",
            kode: Kode,
            cancellationToken: cancellationToken);
        await testService.SettFyllesUtAvBehandlerAsync(test.Id, true, cancellationToken);

        var sideMotorisk = await testService.LeggTilSideAsync(test.Id, "Motoriske tics", "Kryss av hvilke typer motoriske tics som har vært til stede den siste uken.", cancellationToken);
        foreach (var s in MotoriskSjekkliste)
        {
            await testService.LeggTilLeddAsync(sideMotorisk.Id, s, "Kun til klinisk kontekst — teller ikke med i skåren.", TestSvartype.JaNei, null, cancellationToken);
        }
        await testService.LeggTilLeddAsync(sideMotorisk.Id, "Antall (motorisk)", null, TestSvartype.LikertSkala, AntallSkala, cancellationToken);
        await testService.LeggTilLeddAsync(sideMotorisk.Id, "Frekvens (motorisk)", null, TestSvartype.LikertSkala, FrekvensSkala, cancellationToken);
        await testService.LeggTilLeddAsync(sideMotorisk.Id, "Intensitet (motorisk)", null, TestSvartype.LikertSkala, IntensitetSkala, cancellationToken);
        await testService.LeggTilLeddAsync(sideMotorisk.Id, "Kompleksitet (motorisk)", null, TestSvartype.LikertSkala, KompleksitetSkala, cancellationToken);
        await testService.LeggTilLeddAsync(sideMotorisk.Id, "Interferens (motorisk)", null, TestSvartype.LikertSkala, InterferensSkala, cancellationToken);

        var sideFonatorisk = await testService.LeggTilSideAsync(test.Id, "Vokale/fonatoriske tics", "Kryss av hvilke typer vokale tics som har vært til stede den siste uken.", cancellationToken);
        foreach (var s in FonatoriskSjekkliste)
        {
            await testService.LeggTilLeddAsync(sideFonatorisk.Id, s, "Kun til klinisk kontekst — teller ikke med i skåren.", TestSvartype.JaNei, null, cancellationToken);
        }
        await testService.LeggTilLeddAsync(sideFonatorisk.Id, "Antall (fonatorisk)", null, TestSvartype.LikertSkala, AntallSkala, cancellationToken);
        await testService.LeggTilLeddAsync(sideFonatorisk.Id, "Frekvens (fonatorisk)", null, TestSvartype.LikertSkala, FrekvensSkala, cancellationToken);
        await testService.LeggTilLeddAsync(sideFonatorisk.Id, "Intensitet (fonatorisk)", null, TestSvartype.LikertSkala, IntensitetSkala, cancellationToken);
        await testService.LeggTilLeddAsync(sideFonatorisk.Id, "Kompleksitet (fonatorisk)", null, TestSvartype.LikertSkala, KompleksitetSkala, cancellationToken);
        await testService.LeggTilLeddAsync(sideFonatorisk.Id, "Interferens (fonatorisk)", null, TestSvartype.LikertSkala, InterferensSkala, cancellationToken);

        var sideFunksjon = await testService.LeggTilSideAsync(test.Id, "Funksjonsnedsettelse", "Samlet vurdering av hvor mye tic-symptomene påvirker pasientens funksjon i hverdagen.", cancellationToken);
        await testService.LeggTilLeddAsync(sideFunksjon.Id, "Funksjonsnedsettelse", null, TestSvartype.LikertSkala, FunksjonSkala, cancellationToken);

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

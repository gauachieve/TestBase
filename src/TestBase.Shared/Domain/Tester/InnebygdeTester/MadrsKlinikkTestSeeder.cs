namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// MADRS klinikkversjon — den kliniker-administrerte Montgomery Åsberg
/// Depression Rating Scale (Montgomery &amp; Åsberg, 1979), til forskjell fra
/// den allerede innebygde selvutfyllingsversjonen (se
/// <see cref="MadrsSTestSeeder"/>). Kliniker-versjonen har 10 ledd (mot
/// MADRS-S sine 9) — den skiller "tilsynelatende tungsinn" (klinikerens
/// OBSERVASJON av pasientens stemningsleie under samtalen) fra "rapportert
/// tungsinn" (pasientens EGEN beskrivelse), noe et selvutfyllingsskjema ikke
/// kan gjøre. Bruker den samme "behandler fyller ut"-mekanismen som YGTSS-R
/// (se <see cref="YgtssRTestSeeder"/> og docs/beslutningslogg.md) — sendes
/// ALDRI til pasienten, fylles ut av behandler etter et klinisk intervju.
///
/// Ordlyden på hvert av de 10 leddene under er en EGEN, klinisk rimelig
/// gjengivelse av den velkjente MADRS-strukturen (item-titlene selv —
/// "tilsynelatende tungsinn", "rapportert tungsinn", "indre spenning",
/// "redusert søvn", "redusert appetitt", "konsentrasjonsvansker",
/// "initiativløshet", "manglende evne til å føle", "pessimistiske tanker",
/// "selvmordstanker" — er offentlig kjent fagterminologi), IKKE en verbatim
/// gjengivelse av et lisensiert skåringshefte med de faktiske 0/2/4/6-
/// ankerformuleringene til den offisielle skalaen. Bør kvalitetssikres mot
/// en offisiell norsk klinikerversjon (f.eks. gjennom MAPI Research Trust
/// eller tilsvarende rettighetshaver) før reell klinisk bruk.
/// </summary>
public sealed class MadrsKlinikkTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "madrs_klinikk";

    private const string Mellomtrinn = "(mellomtrinn — velg hvis vurderingen ligger mellom nabotrinnene)";

    private sealed record MadrsKlinikkLedd(string Etikett, string Intro, string Verdi0, string Verdi2, string Verdi4, string Verdi6)
    {
        public string Skala =>
            $"0:{Verdi0},1:{Mellomtrinn},2:{Verdi2},3:{Mellomtrinn},4:{Verdi4},5:{Mellomtrinn},6:{Verdi6}";
    }

    private static readonly MadrsKlinikkLedd[] Ledd =
    {
        new("1. Tilsynelatende tungsinn",
            "Din egen observasjon av pasientens mimikk, holdning og stemmeleie under samtalen — ikke det pasienten sier selv.",
            "Ingen tegn til tungsinn.",
            "Ser noe nedstemt ut, men lysner lett opp.",
            "Virker tydelig nedstemt og dyster gjennom det meste av samtalen.",
            "Ekstremt nedstemt utseende — dyster, stille, fortvilet mimikk gjennom hele samtalen."),
        new("2. Rapportert tungsinn",
            "Pasientens EGEN beskrivelse av sitt stemningsleie, uavhengig av hvordan det synes utenpå.",
            "Beskriver stemningsleiet som normalt, avhengig av omstendighetene.",
            "Kjenner seg lei seg eller nedfor, men lysner lett opp.",
            "Kjenner en gjennomgående nedstemthet eller elendighet — stemningen påvirkes lite av ytre omstendigheter.",
            "Beskriver en vedvarende og uutholdelig fortvilelse eller elendighet."),
        new("3. Indre spenning",
            "Følelser av ubehag, uro, indre spenning eller udefinerbar angst — vurdert ut fra intervjuet.",
            "Rolig, kun forbigående indre spenning.",
            "Opplever av og til følelser av indre uro eller ubehagelig anspenthet.",
            "Vedvarende følelse av indre spenning eller periodevis panikk som pasienten så vidt klarer å mestre.",
            "Vedvarende skrekk eller angst — overveldende panikk."),
        new("4. Redusert søvn",
            "Basert på pasientens egen beskrivelse av søvnmengde/-kvalitet, sammenlignet med normalt for vedkommende.",
            "Sover som vanlig.",
            "Lett redusert søvnmengde eller noe mer overfladisk søvn.",
            "Redusert søvn med minst 2 timer sammenlignet med normalt.",
            "Mindre enn 2-3 timers søvn."),
        new("5. Redusert appetitt",
            "Basert på pasientens egen beskrivelse, sammenlignet med normal appetitt for vedkommende.",
            "Normal eller økt appetitt.",
            "Litt redusert appetitt.",
            "Ingen appetitt — maten er smakløs.",
            "Trenger overtaling for å spise i det hele tatt."),
        new("6. Konsentrasjonsvansker",
            "Vurdert ut fra pasientens evne til å samle tankene og følge samtalen, samt egen rapport.",
            "Ingen konsentrasjonsvansker.",
            "Enkelte vansker med å samle tankene.",
            "Vansker med å konsentrere seg og opprettholde oppmerksomhet, som reduserer evnen til å lese eller føre en samtale.",
            "Ute av stand til å lese eller føre en samtale uten stor anstrengelse."),
        new("7. Initiativløshet",
            "Vansker med å komme i gang med og gjennomføre daglige aktiviteter (\"lassitude\").",
            "Ingen vansker med å komme i gang med noe.",
            "Vansker med å sette i gang aktiviteter.",
            "Vansker med å starte enkle rutineoppgaver, som krever ekstra anstrengelse for å gjennomføre.",
            "Klarer ikke å gjennomføre noe uten hjelp."),
        new("8. Manglende evne til å føle",
            "Redusert interesse for omgivelsene og redusert evne til å kjenne følelser for aktiviteter, mennesker og hobbyer som normalt gir glede.",
            "Normal interesse for omgivelser og andre mennesker.",
            "Redusert evne til å glede seg over ting som vanligvis interesserer.",
            "Tap av interesse for omgivelsene og for følelser for venner og bekjente.",
            "Den smertefulle følelsen av total emosjonell likegyldighet overfor nære pårørende, venner og aktiviteter."),
        new("9. Pessimistiske tanker",
            "Skyldfølelse, mindreverdighetsfølelse, selvbebreidelse eller syn på egen fremtid — vurdert ut fra intervjuet.",
            "Ingen pessimistiske tanker.",
            "Vekslende tanker om å ha sviktet, skyldfølelse eller nedvurdering av seg selv.",
            "Vedvarende selvbebreidelse eller konkret men noe rasjonell skyldfølelse — stadig mer pessimistisk om fremtiden.",
            "Vrangforestillingsaktige tanker om ruin, anger eller ubotelig synd — selvanklager som er absurde og urokkelige."),
        new("10. Selvmordstanker",
            "Følelsen av at livet ikke er verdt å leve, tanker om selvmord, og eventuelle forberedelser — spør DIREKTE.",
            "Gleder seg over livet eller tar det som det kommer.",
            "Lei av livet, men kun forbigående selvmordstanker.",
            "Kan ha det bedre død — hyppige, men ikke konkret planlagte selvmordstanker.",
            "Eksplisitte planer for selvmord når mulighet byr seg, aktive forberedelser til selvmord."),
    };

    private const string Kategori = "Depresjon og bipolaritet";

    private const string RapportIntroduksjonTekst =
        "MADRS klinikkversjon (Montgomery Åsberg Depression Rating Scale) er en kliniker-administrert " +
        "vurdering av depresjonsalvorlighet — 10 ledd skåret 0–6 (0-60), basert på et klinisk intervju, IKKE " +
        "et selvutfyllingsskjema (se den separate MADRS-S). Alvorlighetsgrensene under er en mye brukt, men " +
        "omtrentlig konvensjon (jf. bl.a. Snaith m.fl. 1986) — ingen absolutt \"sannhet\", klinisk skjønn kreves.";

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
            navn: "MADRS klinikkversjon (kliniker-administrert)",
            beskrivelse: "Basert på et klinisk intervju med pasienten, vurder følgende 10 dimensjoner. Mellomtrinn (1, 3, 5) kan brukes ved tvil.",
            belonningstekst: "Vurderingen er lagret.",
            kode: Kode,
            cancellationToken: cancellationToken);
        await testService.SettFyllesUtAvBehandlerAsync(test.Id, true, cancellationToken);

        var side = await testService.LeggTilSideAsync(
            test.Id, "Klinisk vurdering av stemningsleie",
            "Basert på observasjon av og samtale med pasienten de siste dagene.", cancellationToken);

        foreach (var ledd in Ledd)
        {
            await testService.LeggTilLeddAsync(
                side.Id, ledd.Etikett, ledd.Intro, TestSvartype.LikertSkala, ledd.Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

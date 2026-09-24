namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// TRAPS II (Traume- og PTSD-screening, del II) — NKVTS sin ICD-11/kompleks
/// PTSD-motpart til TRAPS I (se TrapsITestSeeder, som selv nevner TRAPS II
/// som bevisst utenfor scope 2026-09-12). Del 1 er NØYAKTIG samme
/// traumeeksponerings-sjekkliste (SLESQ-R) som TRAPS I bruker — samme
/// eksponeringsscreening er relevant uansett diagnostisk rammeverk. Del 2 er
/// ICD-11 PTSD/kompleks PTSD-symptomer, og bruker BEVISST samme ordlyd som
/// den allerede innebygde ITQ-testen (International Trauma Questionnaire,
/// se ItqTestSeeder/ItqSkaaringsberegner) for indre konsistens — vi har IKKE
/// selvstendig verifisert et eget, offisielt "TRAPS II"-dokumentavsnitt hos
/// NKVTS med akkurat denne ordlyden; dette er en RIMELIG, men uverifisert,
/// sammenstilling av to kjente NKVTS-oversatte instrumenter (SLESQ-R + ITQ)
/// under samme TRAPS-branding som TRAPS I følger. Bør kvalitetssikres mot et
/// faktisk NKVTS TRAPS II-dokument før reell klinisk bruk. Se
/// docs/beslutningslogg.md.
/// </summary>
public sealed class TrapsIiTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "traps_ii";

    private const string Del1Intro =
        "Spørsmålene under refererer til hendelser som kan ha inntruffet når som helst i livet ditt, inkludert " +
        "tidlig barndom. Vennligst marker med et kryss på alle spørsmål om noe av dette har skjedd med deg.";

    private static readonly string[] Del1Sporsmal =
    {
        "Har du noen gang hatt en livstruende sykdom?",
        "Har du noen gang vært med i en livstruende ulykke?",
        "Har du noen gang vært direkte berørt av en naturkatastrofe?",
        "Har du noen gang blitt utsatt for ran eller overfall med bruk av fysisk makt eller våpen?",
        "Har noen i din aller nærmeste familie, samboer/kjæreste eller svært nær venn dødd i ulykke, drap eller selvmord?",
        "Har du noen gang opplevd at noen ved bruk av fysisk makt eller trusler har tvunget deg til å ha samleie eller oral sex eller anal sex mot din vilje?",
        "Annet enn de hendelsene du allerede har krysset av for: Har du noen gang opplevd at noen har berørt kjønnsorganene dine mot din vilje, eller fått deg til å berøre sitt kjønnsorgan mot din vilje?",
        "Da du var barn: Opplevde du at en av foreldrene dine, en omsorgsperson eller en annen person noen gang sparket deg, slo deg, eller på annen måte angrep eller skadet deg?",
        "Som voksen: Har du noen gang blitt sparket, slått, banket opp eller på annen måte blitt fysisk skadet av en partner, en kjæreste, et familie-medlem, en bekjent eller en annen?",
        "Har en av foreldrene dine, en kjæreste/partner eller familiemedlem gjentatte ganger latterliggjort deg, ydmyket deg eller fortalt deg at du ikke er noen ting verdt?",
        "Har noen utenfor familien, som medelever eller kollegaer, gjentatte ganger latterliggjort deg, ydmyket deg eller fortalt deg at du ikke er noen ting verdt?",
        "Annet enn de hendelsene du allerede har krysset av for: Har du noen gang opplevd at noen har truet deg med et våpen (som for eksempel en kniv eller en pistol)?",
        "Har du noen gang vært vitne til at en annen person ble drept, alvorlig skadet, mishandlet eller utsatt for et seksuelt overgrep?",
        "Har du noen gang opplevd noen annen situasjon der du ble alvorlig skadet eller livet ditt var i fare (for eksempel militær strid, opphold i krigssone eller et terrorangrep)?"
    };

    private const string Del1SisteSporsmal =
        "Annet enn de hendelsene du allerede har krysset av for: Har du noen gang opplevd noen annen situasjon " +
        "som var veldig skremmende eller fryktelig, eller der du følte deg svært hjelpeløs. Vennligst beskriv:";

    private const string SkalaHendelsestid =
        "1:mindre enn 6 måneder siden,2:6 til 12 måneder siden,3:1 til 5 år siden,4:5 til 10 år siden," +
        "5:10 til 20 år siden,6:mer enn 20 år siden";

    public const string SkalaSymptom = "0:Ingenting,1:Noe,2:Moderat,3:Mye,4:Ekstremt";

    private static readonly string[] PtsdSymptomer =
    {
        "Urovekkende drømmer der deler av hendelsen avspilles igjen og igjen, eller som er klart relatert til hendelsen?",
        "Sterke bilder eller minner som noen ganger kommer inn i bevisstheten din, og der du føler at det som hendte skjer igjen her-og-nå?",
        "Unngår indre påminnelser om den belastende hendelsen (for eksempel tanker, følelser eller kroppslige fornemmelser)?",
        "Unngår ytre påminnelser om den belastende hendelsen (for eksempel personer, steder, samtaletema, gjenstander, aktiviteter eller situasjoner)?",
        "Er årvåken, på utkikk eller på vakt?",
        "Føler deg skvetten eller lettskremt?"
    };

    private static readonly string[] PtsdFunksjon =
    {
        "Påvirket dine relasjoner eller ditt sosiale liv?",
        "Påvirket jobben din, eller din evne til å arbeide?",
        "Påvirket andre viktige områder av livet ditt, som foreldrerollen, skole, studier eller andre viktige aktiviteter?"
    };

    private static readonly string[] DsoSymptomer =
    {
        "Når jeg er opprørt, tar det meg lang tid å roe meg ned igjen.",
        "Jeg kjenner meg nummen eller følelsesmessig avstengt.",
        "Jeg føler meg mislykket.",
        "Jeg føler meg verdiløs.",
        "Jeg føler meg fjern eller avskåret fra andre mennesker.",
        "Jeg opplever det vanskelig å være følelsesmessig nær andre."
    };

    private static readonly string[] DsoFunksjon =
    {
        "Skapt bekymring eller uro i dine relasjoner eller ditt sosiale liv?",
        "Påvirket jobben din, eller din evne til å arbeide?",
        "Påvirket andre viktige områder av livet ditt, som foreldrerollen, skole, studier eller andre viktige aktiviteter?"
    };

    private const string Kategori = "Traumer, dissosiasjon og belastninger";

    private const string RapportIntroduksjonTekst =
        "TRAPS II kartlegger traumeeksponering (del 1, SLESQ-R — samme som TRAPS I) og ICD-11 PTSD/kompleks " +
        "PTSD-symptomer (del 2, samme innhold/algoritme som den frittstående ITQ-testen). Diagnostisk algoritme: " +
        "PTSD krever minst ett symptom fra hver av de tre PTSD-symptomgruppene samt funksjonstap; kompleks PTSD " +
        "krever i tillegg minst ett symptom fra hver av de tre forstyrrelser-i-selvorganisering-gruppene samt " +
        "funksjonstap knyttet til disse.";

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
            navn: "TRAPS II (Traume- og ICD-11 PTSD-screening)",
            beskrivelse: "En kartlegging i to deler: traumeeksponering, og ICD-11 PTSD/kompleks PTSD-symptomer.",
            belonningstekst: "Takk for at du fylte ut TRAPS II. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var del1 = await testService.LeggTilSideAsync(test.Id, "Del 1: Traumeeksponering", Del1Intro, cancellationToken);
        foreach (var sporsmalstekst in Del1Sporsmal)
        {
            await testService.LeggTilLeddAsync(del1.Id, sporsmalstekst, null, TestSvartype.JaNei, null, cancellationToken);
        }
        await testService.LeggTilLeddAsync(del1.Id, Del1SisteSporsmal, null, TestSvartype.Fritekst, null, cancellationToken);

        var sideOm = await testService.LeggTilSideAsync(test.Id, "Om hendelsen", null, cancellationToken);
        await testService.LeggTilLeddAsync(sideOm.Id, "Kort beskrivelse av hendelsen", null, TestSvartype.Fritekst, null, cancellationToken);
        await testService.LeggTilLeddAsync(sideOm.Id, "Når skjedde hendelsen?", null, TestSvartype.LikertSkala, SkalaHendelsestid, cancellationToken);

        var sidePtsd = await testService.LeggTilSideAsync(
            test.Id, "Gjenopplevelse, unngåelse og fare",
            "Vennligst les hvert spørsmål nøye, og angi hvor mye du har vært plaget av det problemet den siste måneden.",
            cancellationToken);
        foreach (var tekst in PtsdSymptomer)
        {
            await testService.LeggTilLeddAsync(sidePtsd.Id, tekst, null, TestSvartype.LikertSkala, SkalaSymptom, cancellationToken);
        }

        var sidePtsdFunksjon = await testService.LeggTilSideAsync(
            test.Id, "Funksjon (PTSD-symptomer)", "I løpet av den siste måneden, har problemene over:", cancellationToken);
        foreach (var tekst in PtsdFunksjon)
        {
            await testService.LeggTilLeddAsync(sidePtsdFunksjon.Id, tekst, null, TestSvartype.LikertSkala, SkalaSymptom, cancellationToken);
        }

        var sideDso = await testService.LeggTilSideAsync(
            test.Id, "Følelser, selvbilde og relasjoner",
            "Spørsmålene handler om måter du typisk føler, måter du typisk tenker om deg selv på, og måter du " +
            "typisk forholder deg til andre. Besvar ved å tenke over hvor sann hver påstand er om deg.",
            cancellationToken);
        foreach (var tekst in DsoSymptomer)
        {
            await testService.LeggTilLeddAsync(sideDso.Id, tekst, null, TestSvartype.LikertSkala, SkalaSymptom, cancellationToken);
        }

        var sideDsoFunksjon = await testService.LeggTilSideAsync(
            test.Id, "Funksjon (selvorganisering)",
            "I løpet av den siste måneden, har disse problemene med følelser, oppfatninger om deg selv og i relasjoner:",
            cancellationToken);
        foreach (var tekst in DsoFunksjon)
        {
            await testService.LeggTilLeddAsync(sideDsoFunksjon.Id, tekst, null, TestSvartype.LikertSkala, SkalaSymptom, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

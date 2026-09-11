namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// TRAPS I (Traume- og PTSD-screening, del I) — Nasjonalt Kunnskapssenter om
/// Vold og Traumatisk Stress (NKVTS). Del 1 er Stressful Life Events Screening
/// Questionnaire – Revised (Goodman et al. 1998, norsk v/Thoresen &amp;
/// Øverlien 2013), del 2 er PTSD Checklist for DSM-5 / PCL-5 (Weathers et al.
/// 2013, norsk v/Trond Heir 2014). Kilde:
/// https://www.nkvts.no/content/uploads/2022/11/Traume-og-PTSD-screening-TRAPS-I.pdf
/// (hentet og lest i sin helhet 2026-09-12, se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\TRAPS.md). TRAPS II
/// (ICD-11/kompleks PTSD) er bevisst UTENFOR SCOPE denne runden. IKKE
/// juridisk/klinisk kvalitetssikret av oss utover dette, se
/// docs/beslutningslogg.md.
/// </summary>
public sealed class TrapsITestSeeder : IInnebygdTestSeeder
{
    public string Kode => "traps_i";

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

    private const string Del2Intro =
        "Nå ber vi deg, med den verste hendelsen i tankene, å lese hvert av problemene under og deretter krysse " +
        "av for hvor mye du har vært plaget i løpet av den siste måneden:";

    private const string Del2Skala = "0:Slett ikke,1:Ganske lite,2:Moderat,3:Ganske mye,4:Svært mye";

    private static readonly string[] Del2Sporsmal =
    {
        "Gjentatte, forstyrrende og uønskede minner om den belastende opplevelsen?",
        "Gjentatte og forstyrrende drømmer om den belastende opplevelsen?",
        "At du plutselig føler eller handler som om den belastende hendelsen faktisk skjedde igjen?",
        "Føler deg veldig opprørt når noe minner deg om den belastende opplevelsen.",
        "Sterke fysiske reaksjoner når noe minner deg om den belastende opplevelsen (f.eks. hjertebank, åndenød, svetting)?",
        "Unngår minner, tanker eller følelser forbundet med den belastende opplevelsen?",
        "Unngår forhold som minner om den belastende opplevelsen (f.eks. personer, steder, samtaler, aktiviteter, objekter eller situasjoner)?",
        "Problemer med å huske viktige deler av den belastende opplevelsen?",
        "Sterke negative oppfatninger om deg selv, andre mennesker eller verden?",
        "Klandrer deg selv eller noen andre for hendelsen eller det som skjedde etter hendelsen?",
        "Sterke negative følelser som frykt, skrekk, sinne, skyld eller skam?",
        "Tap av interesse for aktiviteter som du pleide å like?",
        "Føler deg fjern eller avskåret fra andre mennesker?",
        "Problemer med å ha positive følelser (f.eks. ute av stand til å føle glede eller ha varme følelser for mennesker som står deg nær)?",
        "Irritabel oppførsel, sinneutbrudd eller aggressivitet?",
        "Tar for mange sjanser eller gjør ting som kan skade deg?",
        "Er overdrevent oppmerksom, skjerpet eller på vakt?",
        "Følelsen av å være skvetten eller lettskremt?",
        "Vanskeligheter med å konsentrere deg?",
        "Vanskeligheter med å falle i søvn eller sove uavbrutt?"
    };

    private const string Kategori = "Traumer, dissosiasjon og belastninger";

    private const string RapportIntroduksjonTekst =
        "TRAPS I kartlegger traumeeksponering (del 1, SLESQ) og PTSD-symptomer siste måned (del 2, PCL-5). " +
        "Kun del 2 gir en sumskår — del 1 er kontekst for hvilken hendelse pasienten har hatt i tankene.";

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
            navn: "TRAPS I (Traume- og PTSD-screening)",
            beskrivelse: "En kartlegging i to deler: traumeeksponering, og PTSD-symptomer den siste måneden.",
            belonningstekst: "Takk for at du fylte ut TRAPS. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var del1 = await testService.LeggTilSideAsync(test.Id, "Del 1: Traumeeksponering", Del1Intro, cancellationToken);
        foreach (var sporsmalstekst in Del1Sporsmal)
        {
            await testService.LeggTilLeddAsync(
                del1.Id, sporsmalstekst, instruksjon: null, TestSvartype.JaNei, svaralternativer: null, cancellationToken);
        }
        await testService.LeggTilLeddAsync(
            del1.Id, Del1SisteSporsmal, instruksjon: null, TestSvartype.Fritekst, svaralternativer: null, cancellationToken);

        var del2 = await testService.LeggTilSideAsync(test.Id, "Del 2: PTSD-symptomer siste måned", Del2Intro, cancellationToken);
        foreach (var sporsmalstekst in Del2Sporsmal)
        {
            await testService.LeggTilLeddAsync(
                del2.Id, sporsmalstekst, instruksjon: null, TestSvartype.LikertSkala, Del2Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

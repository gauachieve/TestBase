namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Cambridge atferdsskala (Empathy Quotient, EQ) — norsk versjon, © SBC/SJW
/// feb 1998, norsk versjon AMFT nov 2008. Kilde:
/// http://docs.autismresearchcentre.com/tests/EQ_Norwegian.pdf (hentet og
/// lest i sin helhet 2026-09-12, se
/// C:\Users\gaute\AppData\Local\Temp\claude\test-research\EQ.md). Den norske
/// PDF-en har ingen egen skåringsnøkkel — de 4 eksempel-påstandene (E1-E4,
/// ikke skåret) er utelatt her, og selve retningen per item (hvilket svar som
/// er det empatiske) er hentet ved DIREKTE INNHOLDSMATCHING mot den offisielle
/// engelske skåringsnøkkelen (Baron-Cohen &amp; Wheelwright, 2004, Journal of
/// Autism and Developmental Disorders — full tekst innhentet og
/// kryssverifisert ledd for ledd, se EqSkaaringsberegner) — IKKE bekreftet av
/// Cambridge sitt Autism Research Centre for denne spesifikke norske
/// oversettelsen. IKKE juridisk/klinisk kvalitetssikret av oss utover dette,
/// se docs/beslutningslogg.md.
/// </summary>
public sealed class Eq40TestSeeder : IInnebygdTestSeeder
{
    public string Kode => "eq40";

    private const string Stem =
        "Nedenfor finner du en liste av påstander. Les hver påstand nøye og vurder i hvilken grad du er enig " +
        "eller uenig. Det finnes ingen riktige eller gale svar, heller ingen lurespørsmål.";

    private const string Skala = "3:Helt enig,2:Litt enig,1:Litt uenig,0:Helt uenig";

    private static readonly string[] Sporsmal =
    {
        "Jeg forstår raskt signaler om at noen ønsker å delta i en samtale.",
        "Jeg synes det er vanskelig å forklare for andre noe jeg selv forstår, hvis de ikke får det med seg første gang jeg forklarer.",
        "Jeg liker å vise andre mennesker omsorg.",
        "Jeg synes det er vanskelig å vite hvordan jeg skal opptre i sosiale situasjoner.",
        "Folk sier ofte til meg at jeg står for hardt på mitt i diskusjoner.",
        "Det plager meg ikke noe særlig om jeg kommer for sent til en avtale med en venn.",
        "Vennskap og relasjoner er så vanskelige at jeg pleier å unngå å bry meg med slikt.",
        "Jeg synes ofte det er vanskelig å vurdere om noe er uhøflig eller høflig.",
        "Når jeg snakker med noen, pleier jeg å fokusere mer på mine egne tanker enn på hva den jeg snakker med kanskje måtte tenke.",
        "Da jeg var barn, likte jeg å kutte opp mark for å se hva som skjedde.",
        "Jeg forstår raskt om noen sier en ting, men mener noe annet.",
        "Det er vanskelig for meg å forstå hvorfor mennesker blir så ute av seg over visse ting.",
        "Det er lett for meg å sette meg inn i en annens sted.",
        "Jeg er god til å forutsi hva andre kommer til å føle.",
        "Jeg oppdager raskt om noen i en gruppe føler seg utilpass eller ubekvem.",
        "Hvis jeg sier noe som noen blir fornærmet over, mener jeg det er deres problem, ikke mitt.",
        "Hvis noen spurte meg om jeg likte frisyren deres, ville jeg svart ærlig, selv om jeg ikke likte den.",
        "Det er ikke alltid jeg forstår hvorfor folk blir fornærmet av en enkel bemerkning.",
        "Jeg blir egentlig ikke noe særlig følelsesmessig berørt av å se andre gråte.",
        "Jeg er svært direkte, noe enkelte mennesker tolker som at jeg er uhøflig, selv om jeg ikke mener å være det.",
        "Jeg pleier ikke å synes at sosiale situasjoner er forvirrende.",
        "Andre forteller meg at jeg er god til å forstå hvordan de tenker og føler.",
        "Når jeg snakker med noen, snakker jeg heller om deres opplevelser og erfaringer enn om mine egne.",
        "Det gjør meg vondt å se dyr lide.",
        "Jeg klarer å ta beslutninger uten å la meg påvirke av andre menneskers følelser.",
        "Jeg ser lett om en annen er interessert eller kjeder seg når jeg forteller om noe.",
        "Jeg blir fortvilet når jeg i nyhetssendingene på TV ser mennesker som lider.",
        "Venner betror seg ofte til meg om sine problemer og gir uttrykk for at jeg er forståelsesfull.",
        "Jeg kan kjenne på meg om jeg trenger meg på, selv om den andre ikke forteller meg det.",
        "Noen ganger får jeg høre av andre at jeg går for langt med ertingen min.",
        "Andre sier ofte at jeg er lite sensitiv, men jeg forstår ikke alltid hvorfor.",
        "Hvis det kommer en fremmed inn i en gruppe mennesker, mener jeg det er opp til den fremmede å gjøre en innsats for å bli en del av gruppen.",
        "Jeg blir vanligvis ikke følelsesmessig berørt av å se en film.",
        "Jeg kan raskt og intuitivt innstille meg i forhold til hvordan en annen har det.",
        "Det er lett for meg å snappe opp hva en annen kan ha lyst til å snakke om.",
        "Jeg merker det om noen skjuler sine egentlige følelser.",
        "Jeg tenker ikke bevisst over hvilke regler som gjelder i sosiale situasjoner.",
        "Jeg er flink til å forutsi hva andre kommer til å gjøre.",
        "Jeg pleier å bli følelsesmessig engasjert når en venn har problemer.",
        "Jeg kan vanligvis forstå andres synspunkter, selv om jeg ikke er enig i dem."
    };

    private const string Kategori = "ADHD, autisme og nevroutvikling";

    private const string RapportIntroduksjonTekst =
        "Cambridge atferdsskala (Empathy Quotient) måler selvrapportert empati — 40 påstander på en 4-punkts " +
        "enighetsskala. Ingen offisiell klinisk cutoff er oppgitt i kildedokumentet; skåren er beskrivende " +
        "(0–40, høyere = mer selvrapportert empati).";

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
            navn: "Cambridge atferdsskala (Empathy Quotient)",
            beskrivelse: Stem,
            belonningstekst: "Takk for at du fylte ut Cambridge atferdsskala. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Påstander", Stem, cancellationToken);

        foreach (var sporsmalstekst in Sporsmal)
        {
            await testService.LeggTilLeddAsync(
                side.Id, sporsmalstekst, instruksjon: null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }

    /// <summary>
    /// True = "enig-keyed" (Helt enig gir maks poeng), false = "uenig-keyed"
    /// (Helt uenig gir maks poeng) — verifisert 1:1 mot Baron-Cohen &amp;
    /// Wheelwright (2004) sin offisielle nøkkel, se Eq40Skaaringsberegner.
    /// </summary>
    public static readonly IReadOnlyList<bool> ErEnigKeyed = new[]
    {
        true, false, true, false, false, false, false, false, false, false,
        true, false, true, true, true, false, false, false, false, false,
        true, true, true, true, false, true, true, true, true, false,
        false, false, false, true, true, true, true, true, true, true
    };
}

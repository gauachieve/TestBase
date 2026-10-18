namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// ORS (Outcome Rating Scale) — del av KOR/FIT (Klient- og resultatstyrt terapi / Feedback-
/// Informed Treatment, Miller/Duncan m.fl.). Måler pasientens TILSTAND/utfall siste uke, besvart
/// AV PASIENTEN selv, typisk rett FØR en time (se KorFitOppsettService — sendes normalt automatisk
/// en konfigurerbar tid før en definert "neste time", se PlanlagtTildelingService/Tildel/Tester).
/// Hører sammen med SRS (se SrsTestSeeder), som måler alliansen/timen i etterkant — begge vises i
/// én kombinert graf, se Skaaring/KorFitGrafBeregner.
///
/// LISENS: ORS er opphavsrettslig beskyttet (© Scott D. Miller/Barry L. Duncan m.fl.) — elektronisk/
/// kommersiell bruk krever en betalt PCOMS-gruppelisens. Ordlyden under er en EGEN, norsk
/// oversettelse av den kjent publiserte strukturen (4 VAS-domener), IKKE en ordrett kopi av en
/// bestemt lisensiert PDF-utgave. Brukeren har EKSPLISITT besluttet å bygge inn ekte innhold nå og
/// selv ta ansvaret for formell lisensavklaring (2026-10-18, se docs/beslutningslogg.md) — i
/// motsetning til M.I.N.I./HCR-20, der en tydelig merket placeholder var et reelt alternativ, er
/// det her pasienten selv som må kunne LESE og forstå det faktiske innholdet for at besvarelsen
/// skal gi mening.
/// </summary>
public sealed class OrsTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "ors";

    private const string Instruksjon =
        "Når du ser tilbake på den siste uken, inkludert i dag — sett et merke på hver linje for " +
        "hvordan det har gått med deg på dette området. Lavt til venstre betyr dårlig, høyt til " +
        "høyre betyr bra.";

    /// <summary>Generisk lav/høy-anker — selve domenet (Sporsmal) er det som skiller de fire leddene, ikke ankerteksten.</summary>
    private const string VasEndepunkter = "0:Lav,10:Høy";

    private static readonly (string Domene, string Beskrivelse)[] Domener =
    {
        ("Individuelt", "personlig velvære"),
        ("Mellommenneskelig", "familie, nære relasjoner"),
        ("Sosialt", "arbeid, skole, vennskap"),
        ("Totalt", "generell følelse av velvære")
    };

    private const string Kategori = "Funksjon, livskvalitet og behandlingsutfall";

    private const string RapportIntroduksjonTekst =
        "ORS (Outcome Rating Scale) er del av KOR/FIT (Klient- og resultatstyrt terapi) og måler " +
        "pasientens opplevde tilstand/utfall den siste uken på fire områder. Besvares normalt rett " +
        "før en time, gjentatt over tid, og vises sammen med SRS (alliansemål) i en kombinert graf " +
        "— se \"KOR/FIT over tid\" i rapporten når begge er besvart minst én gang.";

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
            navn: "ORS (tilstand/utfall siste uke — KOR/FIT)",
            beskrivelse: "Dra glidebryteren til punktet som passer best for hvert av de fire områdene nedenfor, " +
                         "basert på hvordan du har hatt det den siste uken, inkludert i dag.",
            belonningstekst: "Takk for at du fylte ut ORS. Svarene dine inngår i oppfølgingen av behandlingen din.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Hvordan har du det?", Instruksjon, cancellationToken);

        foreach (var (domene, beskrivelse) in Domener)
        {
            await testService.LeggTilLeddAsync(
                side.Id, $"{domene} ({beskrivelse})", instruksjon: null, TestSvartype.VisuellAnalogSkala, VasEndepunkter, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

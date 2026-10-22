namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// SAI (Session Alliance Inventory, Falkenström, Hatcher, Skjulsvik, Holmqvist Larsson &amp;
/// Holmqvist, 2015, Psychological Assessment 27(1), 169-183) — 6 ledd, adaptert fra Working
/// Alliance Inventory, bygget SPESIFIKT for gjentatt administrering etter hver time (samme
/// bruksmønster som SRS). Måler kvaliteten på SAMARBEIDSRELASJONEN mellom pasient og behandler —
/// IKKE en sykdomstilstand — valgt 2026-10-22 som erstatning for SRS nettopp av den grunn, se
/// docs/beslutningslogg.md "Fjerning av diagnostiske tester / MDR-opprydding". Leddene og
/// 0–5-svarskalaen er hentet VERBATIM fra artikkelens Appendix B (lest direkte, ikke fra
/// sekundærkilde) og oversatt til norsk her — selve artikkelen er copyright American
/// Psychological Association, ingen kjent kommersiell lisensordning for klinisk/elektronisk bruk
/// (i motsetning til ORS/SRS/PCOMS). Ikke formelt avklart med rettighetshaver — samme
/// brukerbeslutning om å bygge inn ekte, oversatt innhold nå og selv ta ansvaret, som tidligere
/// for M.I.N.I./HCR-20/ORS/SRS (2026-10-18/22).
///
/// Hører sammen med SWLS (se SwlsTestSeeder) i "Tilbakemelding til neste time" — SWLS besvares
/// FØR timen, SAI ETTER, begge vist i én kombinert graf (se Skaaring/KorFitGrafBeregner).
/// </summary>
public sealed class SaiTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "sai";

    private const string Instruksjon =
        "Utsagnene nedenfor beskriver måter en person kan tenke eller føle om behandleren sin. " +
        "Tenk bare på TIMEN I DAG når du svarer. Jobb raskt — det er ditt første inntrykk vi vil se.";

    private const string Skala =
        "0:Ikke i det hele tatt,1:Litt,2:Moderat,3:Ganske mye,4:Veldig mye,5:Fullstendig";

    private static readonly string[] Utsagn =
    {
        "Behandler og jeg jobbet mot mål vi var enige om",
        "Jeg følte at behandler satte pris på meg",
        "Behandler og jeg respekterte hverandre",
        "Vi var enige om hva som er viktig for meg å jobbe med",
        "Jeg følte at behandler brydde seg om meg, selv om jeg gjorde ting hen ikke godkjenner",
        "Jeg tror måten vi jobbet med problemet/problemene mitt/mine på, var riktig"
    };

    private const string Kategori = "Funksjon, livskvalitet og behandlingsutfall";

    private const string RapportIntroduksjonTekst =
        "SAI (Session Alliance Inventory, Falkenström m.fl. 2015) måler pasientens opplevelse av " +
        "samarbeidsrelasjonen (alliansen) i én konkret time. Besvares normalt rett etter timen, " +
        "gjentatt over tid, og vises sammen med SWLS (livstilfredshet) i en kombinert graf — se " +
        "\"Tilbakemelding over tid\" i rapporten når begge er besvart minst én gang.";

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
            navn: "SAI (allianse i timen)",
            beskrivelse: "Tenk tilbake på timen i dag, og velg tallet som passer best for hvert utsagn.",
            belonningstekst: "Takk for at du fylte ut SAI. Svarene dine inngår i oppfølgingen av behandlingen din.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Timen i dag", Instruksjon, cancellationToken);

        foreach (var utsagn in Utsagn)
        {
            await testService.LeggTilLeddAsync(
                side.Id, utsagn, instruksjon: null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

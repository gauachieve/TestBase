namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// M.I.N.I. (Mini International Neuropsychiatric Interview) — STRUKTURDEMO, IKKE det ekte,
/// lisensierte instrumentet. Bygget etter brukerens eksplisitte instruks: rettighetshaveren
/// ("publisher") ba om å se HVORDAN systemet ville presentere et strukturert diagnostisk intervju
/// FØR de tar stilling til en lisensavtale. Denne testen er derfor et UI/UX-eksempel — modulnavnene
/// (Sheehan &amp; Lecrubiers M.I.N.I. dekker bl.a. depressivt episode, suicidalitet, (hypo)mani,
/// panikklidelse, sosial fobi, tvangslidelse, PTSD, rusmiddelbruk, generalisert angstlidelse og
/// psykotiske symptomer) er offentlig kjent diagnostisk fagterminologi og IKKE i seg selv
/// beskyttet — men SPØRSMÅLENE under er en HELT EGEN, generisk formulering, IKKE en gjengivelse av
/// M.I.N.I. sine faktiske, lisensierte screeningspørsmål eller av det proprietære gren-/
/// hoppelogikk-flytskjemaet (skip-logic) som utgjør instrumentets faktiske diagnostiske verdi.
///
/// MÅ IKKE brukes klinisk. Skal erstattes med reelt lisensiert M.I.N.I.-innhold (versjon 6.0/nyeste)
/// den dagen en lisensavtale er på plass med rettighetshaver (i dag typisk Harm Research/Medical
/// Outcomes Systems, Sheehan-familiens lisensieringsorgan) — se docs/beslutningslogg.md.
/// Skåringen (<see cref="TestBase.Shared.Domain.Tester.Skaaring.MiniStrukturdemoSkaaringsberegner"/>)
/// er bevisst holdt til en RÅ opptelling per modul, ikke noe forsøk på å etterligne det virkelige
/// diagnostiske algoritme-treet.
/// </summary>
public sealed class MiniStrukturdemoTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "mini_strukturdemo";

    private sealed record Modul(string Navn, string[] Sporsmal);

    private static readonly Modul[] Moduler =
    {
        new("Depressivt episode", new[]
        {
            "Har du i løpet av de siste to ukene, det meste av dagen, nesten hver dag, følt deg nedstemt eller trist?",
            "Har du i samme periode mistet interessen eller gleden for de fleste aktiviteter?",
            "Har du merket tydelig endring i søvn, appetitt eller energinivå i denne perioden?"
        }),
        new("Suicidalitet", new[]
        {
            "Har du den siste måneden tenkt at du ville vært bedre død, eller ønsket å skade deg selv?",
            "Har du den siste måneden hatt konkrete tanker om å ta ditt eget liv?",
            "Har du noen gang i livet forsøkt å ta ditt eget liv?"
        }),
        new("(Hypo)manisk episode", new[]
        {
            "Har du hatt en periode hvor du følte deg uvanlig oppstemt, energisk eller irritabel i flere dager?",
            "Følte du i samme periode markert redusert behov for søvn uten å bli trett?"
        }),
        new("Panikklidelse", new[]
        {
            "Har du opplevd plutselige anfall med sterk frykt eller ubehag, med kroppslige symptomer som hjertebank eller pustevansker?",
            "Har du vært bekymret for å få nye slike anfall, eller endret atferd på grunn av dem?"
        }),
        new("Sosial fobi", new[]
        {
            "Er du markert redd for situasjoner der du kan bli observert eller vurdert av andre?",
            "Unngår du slike sosiale situasjoner, eller utholder dem med sterkt ubehag?"
        }),
        new("Tvangslidelse (OCD)", new[]
        {
            "Har du tilbakevendende, plagsomme tanker eller bilder som er vanskelige å bli kvitt?",
            "Utfører du gjentatte handlinger eller ritualer for å redusere ubehag eller forhindre noe fryktet?"
        }),
        new("PTSD", new[]
        {
            "Har du opplevd eller vært vitne til en svært skremmende eller livstruende hendelse?",
            "Får du fortsatt plagsomme minner, mareritt, eller sterkt kroppslig ubehag knyttet til hendelsen?"
        }),
        new("Rusmiddelbruk", new[]
        {
            "Har bruk av alkohol eller andre rusmidler skapt problemer for deg det siste året (helse, jobb, relasjoner)?",
            "Har du prøvd å redusere bruken uten å lykkes?"
        }),
        new("Generalisert angstlidelse", new[]
        {
            "Har du følt deg overdrevent bekymret eller anspent for flere ting, mesteparten av tiden, i minst 6 måneder?",
            "Er denne bekymringen vanskelig å kontrollere?"
        }),
        new("Psykotiske symptomer", new[]
        {
            "Har du noen gang hørt stemmer eller sett ting som andre ikke kunne høre eller se?",
            "Har du hatt sterke overbevisninger som andre rundt deg mente var uvirkelige eller usanne?"
        })
    };

    private const string Kategori = "Diagnostikk, tverrgående og øvrige verktøy";

    private const string RapportIntroduksjonTekst =
        "STRUKTURDEMO — IKKE det ekte, lisensierte M.I.N.I.-instrumentet. Bygget for å vise " +
        "rettighetshaveren hvordan systemet presenterer et strukturert diagnostisk intervju, som " +
        "grunnlag for en eventuell lisensavtale. Spørsmålene under er EGNE, generiske formuleringer, " +
        "ikke M.I.N.I. sine faktiske screeningspørsmål eller gren-/hoppelogikk. Skal erstattes med " +
        "reelt lisensiert innhold før klinisk bruk.";

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
            navn: "M.I.N.I. — strukturdemo (IKKE lisensiert innhold)",
            beskrivelse: "Demonstrasjon av systemets struktur for et modulbasert diagnostisk intervju — spørsmålene er egne, generiske eksempler, ikke det ekte M.I.N.I.-instrumentet. Fylles ut av behandler basert på klinisk intervju.",
            belonningstekst: "Vurderingen er lagret.",
            kode: Kode,
            cancellationToken: cancellationToken);
        await testService.SettFyllesUtAvBehandlerAsync(test.Id, true, cancellationToken);

        foreach (var modul in Moduler)
        {
            var side = await testService.LeggTilSideAsync(test.Id, modul.Navn, null, cancellationToken);
            foreach (var sporsmal in modul.Sporsmal)
            {
                await testService.LeggTilLeddAsync(side.Id, sporsmal, null, TestSvartype.JaNei, null, cancellationToken);
            }
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

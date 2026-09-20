namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Regenererer PDS-ICD-11 (Personality Disorder Severity – ICD-11 Scale),
/// SELVRAPPORTERINGSVERSJONEN (klinikerversjonen er bevisst ikke bygget, jf.
/// eksplisitt instruks). Ordrett norsk tekst fra den offisielle norske
/// oversettelsen (Tore Willy Lie og Lars Lien, 2022, godkjent av
/// rettighetshaverne), hentet fra rettighetshavernes egen distribusjons-side
/// https://www.personality.today/pds-icd-11/ (NORWEGIAN.pdf). Oversettelse
/// og bruk er "til fri bruk etter avtale med forfatterne" (Bo Bach,
/// bbpn@regionsjaelland.dk / Martin Sellbom, martin.sellbom@otago.ac.nz) —
/// en høflighets-e-post til dem før produksjonssetting anbefales, men er
/// IKKE en betingelse for bruk. © Copyright 2020 Bo Bach, Tiffany Brown og
/// Martin Sellbom. Offisielt oversatt — INGEN "ikke offisielt oversatt"-merking.
/// Hvert ledd har EGNE, unike svaralternativer (bipolar per-ledd-skala, ikke
/// en delt Likert-skala) — verdiene lagret er VISNINGSREKKEFØLGE-indekser
/// (0..4 / 0..3), IKKE de endelige poengene, for å unngå at to alternativer
/// med samme reelle poengverdi (f.eks. begge polene skåres 2) vises som
/// "valgt" samtidig i utfyllingssiden (radio-knappenes checked-sammenligning
/// er verdi-basert, se TestLeddSvaralternativer). Se
/// PdsIcd11Skaaringsberegner for oversettelsen fra indeks til faktisk poeng.
/// IKKE juridisk/klinisk kvalitetssikret av oss.
/// </summary>
public sealed class PdsIcd11TestSeeder : IInnebygdTestSeeder
{
    public string Kode => "pds_icd11";

    /// <summary>Kort, pasientvennlig navn (2026-09-20) — se ItqTestSeeder.Navn for begrunnelse.</summary>
    public const string Navn = "Personlighetsfunksjon ICD-11 PDS";

    private const string Kategori = "Personlighet, relasjoner og sosial fungering";

    public sealed record Ledd(string Sporsmalstekst, string[] Alternativer);

    public static readonly IReadOnlyList<Ledd> Ledd1Til10 = new[]
    {
        new Ledd("1. Identitet", new[]
        {
            "Jeg har ofte ingen følelse av hvem jeg er, spesielt når jeg er sammen med andre mennesker",
            "Jeg er noen ganger forvirret over hvem jeg er, spesielt når jeg er sammen med andre mennesker",
            "Jeg har en stabil følelse av hvem jeg er",
            "Følelsen av hvem jeg er, er generelt altfor fastlåst og begrenset (f.eks. i relasjon til arbeid eller til en annen person)",
            "Uansett hva omstendighetene er, så er følelsen av hvem jeg er veldig begrenset eller urokkelig"
        }),
        new Ledd("2. Selvfølelse", new[]
        {
            "Mesteparten av tiden føler jeg meg verdiløs, noe som påvirker hvordan jeg forholder meg til andre",
            "Jeg har det ofte ikke så godt med meg selv, noe som innimellom påvirker hvordan jeg forholder meg til andre mennesker",
            "Jeg har det som regel godt med meg selv",
            "Jeg føler ofte at jeg er bedre enn andre, noe som påvirker hvordan jeg forholder meg til andre mennesker",
            "Jeg føler meg ofte betydelig bedre enn andre, noe som påvirker hvordan jeg forholder meg til andre mennesker"
        }),
        new Ledd("3. Selvoppfattelse", new[]
        {
            "Jeg har ingen sterke sider",
            "Jeg har få sterke sider",
            "Jeg kjenner mine styrker og svakheter",
            "Jeg har få svakheter eller begrensninger",
            "Jeg har ingen svakheter eller begrensninger"
        }),
        new Ledd("4. Mål", new[]
        {
            "Jeg er sjelden i stand til å sette meg og følge egne mål",
            "Jeg synes at det av og til er vanskelig å sette og følge mål",
            "Jeg har ingen problemer med å sette og følge realistiske mål",
            "Jeg synes noen ganger det er vanskelig å endre målene mine, selv når de er for vanskelige å oppnå",
            "Jeg synes som regel det er vanskelig å endre målene mine, selv når de er nesten umulig å oppnå"
        }),
        new Ledd("5. Interesse for relasjoner", new[]
        {
            "Jeg har ingen interesse av å være med andre og gjør hva som helst for å unngå dem",
            "Jeg har liten interesse av å være med andre og derfor unngår jeg dem",
            "Jeg har en god balanse mellom å være alene og være med andre",
            "Jeg føler meg noen ganger utilpass når jeg ikke er med andre",
            "Jeg føler meg ofte utilpass når jeg ikke er med andre"
        }),
        new Ledd("6. Forstå andres perspektiv", new[]
        {
            "Jeg tenker aldri på andre menneskers tanker og følelser",
            "Jeg tenker sjelden på andre menneskers tanker og følelser",
            "Jeg kan lett forholde meg til andre menneskers tanker og følelser",
            "Jeg tenker ofte for mye på hva andre tenker og føler",
            "Jeg tenker alltid for mye på hva andre tenker og føler"
        }),
        new Ledd("7. Gjensidighet i relasjoner", new[]
        {
            "Folk klager alltid på at jeg er for selvopptatt i relasjoner",
            "Folk har noen ganger klaget på at jeg er for selvopptatt i relasjoner",
            "Jeg klarer å etablere og holde på nære og gjensidig tilfredsstillende relasjoner",
            "Av og til klarer jeg ikke å avslutte relasjoner, selv når de er skadelige for meg",
            "Jeg klarer sjelden å avslutte relasjoner, selv når de er skadelige for meg"
        }),
        new Ledd("8. Håndtering av uenighet", new[]
        {
            "Jeg er ofte uenig med andre, noe som gir alvorlige relasjonelle problemer",
            "Jeg er noen ganger uenig med andre, noe som gir relasjonelle problemer",
            "Jeg klarer å håndtere uenighet i relasjoner på en samarbeidsvillig måte",
            "Jeg unngår ofte uenigheter ved å gi etter for andre, selv om jeg kommer uheldig ut av situasjonen",
            "Jeg unngår uenigheter og konflikter med andre for enhver pris"
        }),
        new Ledd("9. Følelsesmessig kontroll og uttrykk", new[]
        {
            "Ofte klarer jeg ikke å kontrollere følelsene mine, noe som gir alvorlige problemer med andre mennesker",
            "Noen ganger har jeg problemer med å kontrollere følelsene mine, noe som gir noen problemer med andre mennesker",
            "Jeg klarer som regel å kontrollere og uttrykke følelsene mine på en hensiktsmessig måte",
            "Folk klager noen ganger på at jeg ikke viser følelser",
            "Folk klager ofte på at jeg ikke viser følelser i det hele tatt"
        }),
        new Ledd("10. Regulering av atferd", new[]
        {
            "Jeg reagerer ofte så raskt eller impulsivt at det gir store problemer",
            "Jeg reagerer noen ganger impulsivt uten å vurdere konsekvensene, noe som gir problemer",
            "Jeg klarer som regel å være spontan samtidig som jeg har god kontroll over mine handlinger",
            "Jeg har noen ganger så mye kontroll over mine handlinger at jeg ikke får det samme ut av livet som andre",
            "Jeg har ofte så mye kontroll over mine handlinger at jeg nesten ikke får noe ut av livet"
        })
    };

    public static readonly IReadOnlyList<Ledd> Ledd11Til14 = new[]
    {
        new Ledd("11. Opplevelse av virkeligheten under stress", new[]
        {
            "Under følelsesmessig stress er min opplevelse av situasjoner som regel riktig",
            "Når jeg er veldig følelsesmessig stresset så er opplevelsen min av situasjonene noe forvrengt (f.eks. forventer at det verste skal skje, føler meg avvist når jeg blir kritisert av andre)",
            "Jeg mister noen ganger kontakt med virkeligheten når jeg blir følelsesmessig stresset (f.eks. mistenksom, uvirkelighetsfølelse, eller at ting rundt meg føles som en drøm)",
            "Jeg mister ofte kontakt med virkeligheten når jeg er følelsesmessig stresset (f.eks. svært sterk mistenksomhet, ser eller hører ting som andre ikke kan se eller høre, eller ha ut-av-kroppen opplevelser)"
        }),
        new Ledd("12. Skade seg selv", new[]
        {
            "Jeg skader aldri meg selv",
            "Jeg skader sjelden meg selv",
            "Jeg skader meg selv noen ganger",
            "Jeg skader ofte meg selv"
        }),
        new Ledd("13. Skade andre (både med og uten vilje)", new[]
        {
            "Jeg skader aldri andre",
            "Jeg skader sjelden andre",
            "Jeg skader andre noen ganger",
            "Jeg skader ofte andre"
        }),
        new Ledd(
            "14. Når du tenker på svarene dine ovenfor, hvor mye problemer gir de deg i viktige områder i livet ditt (f.eks. personlig, familie, sosialt, utdannelse, jobb)?",
            new[] { "Ikke i det hele tatt", "Litt", "Noe", "Mye" })
    };

    private const string RapportIntroduksjonTekst =
        "PDS-ICD-11 (Personality Disorder Severity – ICD-11 Scale, selvrapporteringsversjon) er et screeninginstrument " +
        "for alvorlighetsgrad av personlighetsproblematikk/-fungering i tråd med ICD-11. 14 ledd, sumskår 0-32. " +
        "Foreslåtte grenseverdier (Bo Bach, foredrag sept. 2022): 12-16 mild-moderat, 16-20 moderat-alvorlig, 20+ " +
        "svært alvorlig. Kan ikke alene brukes som grunnlag for å sette en personlighetsforstyrrelse-diagnose. " +
        "Referanse: Bach, B., Brown, T. A., Mulder, R. T., Newton-Howes, G., Simonsen, E., & Sellbom, M. (2021). " +
        "Development and Initial Evaluation of the ICD-11 Personality Disorder Severity Scale: PDS-ICD-11. " +
        "Personality and Mental Health. https://doi.org/10.1002/pmh.1510.";

    private static string TilSvaralternativer(string[] tekster)
    {
        var deler = tekster.Select((tekst, indeks) => $"{indeks}:{tekst}");
        return string.Join(",", deler);
    }

    public async Task SeedAsync(TestService testService, CancellationToken cancellationToken = default)
    {
        await testService.SikreStandardkategorierAsync(cancellationToken);

        var eksisterende = await testService.HentTestVedKodeAsync(Kode, cancellationToken);
        if (eksisterende is not null)
        {
            await testService.KoblTestTilKategoriAsync(eksisterende.Id, Kategori, cancellationToken);
            await testService.SettRapportIntroduksjonAsync(eksisterende.Id, RapportIntroduksjonTekst, cancellationToken);
            await testService.SettIcdElleveKlarAsync(eksisterende.Id, true, cancellationToken);
            await testService.SettNavnAsync(eksisterende.Id, Navn, cancellationToken);
            return;
        }

        var test = await testService.OpprettTestAsync(
            navn: Navn,
            beskrivelse: "Vennligst velg det utsagnet innenfor hvert område som best beskriver deg.",
            belonningstekst: "Takk for at du fylte ut PDS-ICD-11. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Personlighetsfungering", null, cancellationToken);
        foreach (var ledd in Ledd1Til10.Concat(Ledd11Til14))
        {
            await testService.LeggTilLeddAsync(
                side.Id, ledd.Sporsmalstekst, null, TestSvartype.LikertSkala, TilSvaralternativer(ledd.Alternativer), cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
        await testService.SettIcdElleveKlarAsync(test.Id, true, cancellationToken);
    }
}

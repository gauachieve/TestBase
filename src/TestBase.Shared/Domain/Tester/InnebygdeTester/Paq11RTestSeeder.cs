namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Regenererer PAQ-11R (Revised Personality Assessment Questionnaire for
/// ICD-11 personality traits) — 17 ledd, 5 trekkdomener. © Copyright 2022 by
/// Youl-Ri Kim and Peter Tyrer. Engelsk originaltekst hentet fra
/// rettighetshavernes distribusjonsside
/// https://www.personality.today/kopi-af-pds-icd-11-scales/ (PAQ-11R_English.pdf).
/// INGEN offisiell norsk oversettelse finnes ennå (kun PAQ-11R selv, ikke
/// original-PAQ-11, har foreløpig norsk forskningsforankring — se
/// docs/beslutningslogg.md "ICD-11-tester" for valget mellom PAQ-11/PAQ-11R).
/// Denne norske teksten er VÅR EGEN oversettelse fra engelsk, IKKE
/// psykometrisk validert eller godkjent av rettighetshaverne. Merket via
/// Test.OversettelseNotat — vises kun til behandler/admin, IKKE i selve
/// testnavnet (rettet 2026-09-20, se docs/beslutningslogg.md "ICD-11-tester,
/// del 2"). En høflighets-e-post til forfatterne før bredere bruk er
/// ønskelig. IKKE juridisk/klinisk kvalitetssikret av oss.
/// </summary>
public sealed class Paq11RTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "paq11r";

    /// <summary>Kort, pasientvennlig navn (2026-09-20) — se ItqTestSeeder.Navn for begrunnelse.</summary>
    public const string Navn = "Personlighetsscreening ICD-11 PAQ-11R";

    private const string OversettelseNotatTekst = "Ikke offisielt oversatt – kun til uttesting";

    public const string Skala = "0:Aldri,1:Sjelden,2:Noen ganger,3:Ofte,4:Alltid";

    private static readonly string[] Sporsmal =
    {
        "Hvor godt kommer du generelt overens med andre mennesker?",
        "Stoler du vanligvis på dem?",
        "Forventer du at det verste skal skje i livet?",
        "Er du en person med høye standarder?",
        "Har du noen gang gjort ting impulsivt og angret på det etterpå?",
        "Planlegger du alt i detalj i livet?",
        "Sier folk noen ganger at du er for kresen eller samvittighetsfull, eller til og med perfeksjonist?",
        "Har du noen virkelig nære relasjoner?",
        "Bryr du deg om andre mennesker?",
        "Føler du deg dystrere om fremtiden enn de fleste andre?",
        "Gjør du noen ganger ting i farten uten å bry deg om konsekvensene?",
        "Mangler du selvtillit?",
        "Har du en tendens til å manipulere andre for å få det du vil ha i livet?",
        "Blir du ofte sint eller aggressiv?",
        "Bekymrer du deg noen ganger for ting de fleste andre ikke ville brydd seg om?",
        "Er du mer nervøs enn de fleste andre?",
        "Er du en person som liker å holde seg unna andre mennesker?"
    };

    private const string Kategori = "Personlighet, relasjoner og sosial fungering";

    private const string RapportIntroduksjonTekst =
        "PAQ-11R (Revised Personality Assessment Questionnaire for ICD-11 personality traits) er et kort " +
        "selvrapporteringsinstrument som screener fem trekkdomener jf. ICD-11: anankasme, tilbaketrekning, " +
        "disinhibition, dyssosialitet og negativ affektivitet. 17 ledd, skala 0-4. Domenegrenseverdiene er " +
        "foreløpige (ikke endelig fastsatt av kildeforfatterne). © Copyright 2022 Youl-Ri Kim og Peter Tyrer. " +
        "NB: Norsk tekst er IKKE en offisiell oversettelse — se klassekommentaren i Paq11RTestSeeder.";

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
            await testService.SettOversettelseNotatAsync(eksisterende.Id, OversettelseNotatTekst, cancellationToken);
            return;
        }

        var test = await testService.OpprettTestAsync(
            navn: Navn,
            beskrivelse: "Vennligst les utsagnene under, og angi for hver av dem hvor mye dette har stemt for deg " +
                         "normalt sett.",
            belonningstekst: "Takk for at du fylte ut PAQ-11R. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Personlighetstrekk", null, cancellationToken);
        foreach (var sporsmal in Sporsmal)
        {
            await testService.LeggTilLeddAsync(side.Id, sporsmal, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
        await testService.SettIcdElleveKlarAsync(test.Id, true, cancellationToken);
        await testService.SettOversettelseNotatAsync(test.Id, OversettelseNotatTekst, cancellationToken);
    }
}

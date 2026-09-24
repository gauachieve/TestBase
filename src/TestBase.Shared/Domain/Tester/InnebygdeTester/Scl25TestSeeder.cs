namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// SCL-25 / Hopkins Symptom Checklist-25 (HSCL-25), utviklet ved Johns
/// Hopkins (Derogatis et al., videreutviklet av Mollica et al. for
/// flyktninghelse) — offentlig tilgjengelig, mye brukt i Norge (NAV,
/// primærhelsetjeneste, flyktninghelseundersøkelser). 25 ledd: angst-delskala
/// (ledd 1-10) og depresjons-delskala (ledd 11-25, HVOR ledd 24
/// "tanker om å avslutte livet" er et selvmordsscreeningledd som flagges
/// spesielt, se Scl25Skaaringsberegner). Norsk oversettelse her er egen,
/// IKKE hentet fra en spesifikk sitert norsk kilde ennå — bør kvalitetssikres
/// før reell klinisk bruk. Se docs/beslutningslogg.md.
/// </summary>
public sealed class Scl25TestSeeder : IInnebygdTestSeeder
{
    public string Kode => "scl25";

    private const string Skala = "1:Ikke i det hele tatt,2:Litt,3:Ganske mye,4:Svært mye";

    private static readonly string[] Angst =
    {
        "1. Plutselig frykt uten grunn",
        "2. En følelse av redsel",
        "3. Nervøsitet eller indre uro",
        "4. Skjelving",
        "5. Anspenthet eller opphisselse",
        "6. Hjerteklapp eller hjertebank",
        "7. Skjelvinger",
        "8. En følelse av å være anspent eller på vakt",
        "9. Panikkanfall",
        "10. Så rastløs at du ikke klarer å sitte stille"
    };

    private static readonly string[] Depresjon =
    {
        "11. Manglende energi, alt går langsommere",
        "12. Å klandre deg selv for ting",
        "13. Gråtetokter",
        "14. Tap av interesse for seksuell samkvem",
        "15. Dårlig matlyst",
        "16. Søvnproblemer",
        "17. En følelse av håpløshet med tanke på fremtiden",
        "18. Følelse av tristhet",
        "19. En følelse av å være fanget",
        "20. Bekymring for mye om forskjellige ting",
        "21. Manglende interesse for ting",
        "22. En følelse av at alt du gjør er et slit",
        "23. En følelse av verdiløshet",
        "24. Tanker om å avslutte livet",
        "25. En følelse av at alt går galt"
    };

    private const string Kategori = "Diagnostikk, tverrgående og øvrige verktøy";

    private const string RapportIntroduksjonTekst =
        "SCL-25 / Hopkins Symptom Checklist-25 er et bredt kartleggingsverktøy for angst- og " +
        "depresjonssymptomer, mye brukt i norsk primærhelsetjeneste. 25 spørsmål om siste uke, " +
        "hvert skåret 1–4. Ledd 24 (tanker om å avslutte livet) flagges spesielt ved forhøyet svar.";

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
            navn: "SCL-25 (Angst og depresjon)",
            beskrivelse: "I hvilken grad har du vært plaget av følgende i løpet av den siste uken, inkludert i dag?",
            belonningstekst: "Takk for at du fylte ut SCL-25. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var sideAngst = await testService.LeggTilSideAsync(test.Id, "Del 1", null, cancellationToken);
        foreach (var sporsmal in Angst)
        {
            await testService.LeggTilLeddAsync(sideAngst.Id, sporsmal, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        var sideDepresjon = await testService.LeggTilSideAsync(test.Id, "Del 2", null, cancellationToken);
        foreach (var sporsmal in Depresjon)
        {
            await testService.LeggTilLeddAsync(sideDepresjon.Id, sporsmal, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

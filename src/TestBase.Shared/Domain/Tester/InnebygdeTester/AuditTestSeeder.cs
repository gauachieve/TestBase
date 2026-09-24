namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// AUDIT (Alcohol Use Disorders Identification Test) — utviklet av WHO
/// (Babor, Higgins-Biddle, Saunders &amp; Monteiro, 2001), offentlig
/// tilgjengelig screeningverktøy for risikofylt alkoholbruk. 10 ledd, hvert
/// med EGNE svaralternativer (ikke en delt skala) — ledd 1-8 er 0-4, ledd 9-10
/// er 0/2/4. Norsk oversettelse her er egen, IKKE hentet fra en spesifikk
/// sitert norsk kilde ennå (Helsedirektoratets offisielle oversettelse bør
/// brukes til kvalitetssikring før reell klinisk bruk) — se
/// docs/beslutningslogg.md.
/// </summary>
public sealed class AuditTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "audit";

    private const string Kategori = "Rus og avhengighet";

    private const string RapportIntroduksjonTekst =
        "AUDIT (Alcohol Use Disorders Identification Test, WHO) er et screeningverktøy for risikofylt " +
        "alkoholbruk. 10 spørsmål, sumskår 0-40.";

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
            navn: "AUDIT (Alkoholbruk)",
            beskrivelse: "Kryss av for det svaret som passer best for deg om alkoholbruken din.",
            belonningstekst: "Takk for at du fylte ut AUDIT. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Alkoholbruk", null, cancellationToken);

        await testService.LeggTilLeddAsync(side.Id, "1. Hvor ofte drikker du alkohol?", null, TestSvartype.LikertSkala,
            "0:Aldri,1:Sjelden,2:2-4 ganger i måneden,3:2-3 ganger i uken,4:4 ganger eller mer i uken", cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "2. Hvor mange alkoholenheter drikker du en typisk dag du drikker?", null, TestSvartype.LikertSkala,
            "0:1-2,1:3-4,2:5-6,3:7-9,4:10 eller flere", cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "3. Hvor ofte drikker du seks enheter eller mer ved samme anledning?", null, TestSvartype.LikertSkala,
            "0:Aldri,1:Sjeldnere enn månedlig,2:Månedlig,3:Ukentlig,4:Daglig eller nesten daglig", cancellationToken);

        const string Frekvensskala = "0:Aldri,1:Sjeldnere enn månedlig,2:Månedlig,3:Ukentlig,4:Daglig eller nesten daglig";
        await testService.LeggTilLeddAsync(side.Id, "4. Hvor ofte har du i løpet av det siste året opplevd at du ikke klarte å stoppe å drikke når du først hadde begynt?", null, TestSvartype.LikertSkala, Frekvensskala, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "5. Hvor ofte har du i løpet av det siste året ikke klart å gjøre det som normalt ble forventet av deg på grunn av drikking?", null, TestSvartype.LikertSkala, Frekvensskala, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "6. Hvor ofte har du i løpet av det siste året hatt behov for å drikke alkohol om morgenen for å komme i gang etter mye drikking dagen før?", null, TestSvartype.LikertSkala, Frekvensskala, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "7. Hvor ofte har du i løpet av det siste året hatt skyldfølelse eller dårlig samvittighet etter å ha drukket?", null, TestSvartype.LikertSkala, Frekvensskala, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "8. Hvor ofte har du i løpet av det siste året ikke husket hva som skjedde kvelden før på grunn av drikking?", null, TestSvartype.LikertSkala, Frekvensskala, cancellationToken);

        const string JaSkala = "0:Nei,2:Ja, men ikke i løpet av det siste året,4:Ja, i løpet av det siste året";
        await testService.LeggTilLeddAsync(side.Id, "9. Har du eller noen andre blitt skadet fordi du har drukket?", null, TestSvartype.LikertSkala, JaSkala, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "10. Har en slektning, venn, lege eller annet helsepersonell vært bekymret for drikkingen din, eller foreslått at du bør redusere?", null, TestSvartype.LikertSkala, JaSkala, cancellationToken);

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

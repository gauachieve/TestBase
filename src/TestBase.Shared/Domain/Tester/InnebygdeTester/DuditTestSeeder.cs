namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// DUDIT (Drug Use Disorders Identification Test) — utviklet av Berman,
/// Bergman, Palmstierna &amp; Schlyter (2003, Karolinska Institutet) etter
/// samme mal som WHOs AUDIT, fritt tilgjengelig for klinisk/akademisk bruk.
/// 11 ledd, ledd 1-9 er 0-4, ledd 10-11 er 0/2/4 (samme struktur som AUDIT).
/// Norsk oversettelse her er egen, IKKE hentet fra en spesifikk sitert norsk
/// kilde ennå — bør kvalitetssikres før reell klinisk bruk. Se
/// docs/beslutningslogg.md.
/// </summary>
public sealed class DuditTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "dudit";

    private const string Kategori = "Rus og avhengighet";

    private const string RapportIntroduksjonTekst =
        "DUDIT (Drug Use Disorders Identification Test, Berman et al. 2003) er et screeningverktøy for " +
        "narkotikabruk, etter samme mal som AUDIT. 11 spørsmål, sumskår 0-44. Cutoff for mulig " +
        "rusrelatert problem er kjønnsavhengig (se rapporten) — vurder alltid i lys av pasientens kjønn.";

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
            navn: "DUDIT (Narkotikabruk)",
            beskrivelse: "Kryss av for det svaret som passer best for deg om bruk av narkotika/rusmidler (utenom alkohol).",
            belonningstekst: "Takk for at du fylte ut DUDIT. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var side = await testService.LeggTilSideAsync(test.Id, "Narkotikabruk", null, cancellationToken);

        const string FrekvensskalaMndSiste = "0:Aldri,1:Sjeldnere enn månedlig,2:Månedlig,3:Ukentlig,4:Daglig eller nesten daglig";
        await testService.LeggTilLeddAsync(side.Id, "1. Hvor ofte bruker du andre rusmidler enn alkohol?", null, TestSvartype.LikertSkala, FrekvensskalaMndSiste, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "2. Bruker du mer enn ett rusmiddel på samme dag?", null, TestSvartype.LikertSkala, FrekvensskalaMndSiste, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "3. Hvor mange ganger bruker du rusmidler en typisk dag du bruker?", null, TestSvartype.LikertSkala,
            "0:0,1:1-2,2:3-4,3:5-6,4:7 eller flere", cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "4. Hvor ofte er du så påvirket av rusmidler at det går utover jobb, skole eller husarbeid?", null, TestSvartype.LikertSkala, FrekvensskalaMndSiste, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "5. Hvor ofte har du hatt lyst til å slutte, men ikke klart det?", null, TestSvartype.LikertSkala, FrekvensskalaMndSiste, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "6. Hvor ofte har du brukt rusmidler og deretter blitt involvert i noe du senere angret på?", null, TestSvartype.LikertSkala, FrekvensskalaMndSiste, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "7. Hvor ofte har du hatt hukommelsestap eller andre problemer på grunn av rusbruk?", null, TestSvartype.LikertSkala, FrekvensskalaMndSiste, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "8. Føler du selv at du bruker rusmidler for mye?", null, TestSvartype.LikertSkala,
            "0:Aldri,1:Sjelden,2:Av og til,3:Ofte,4:Alltid", cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "9. Har du forsøkt å redusere eller slutte med rusmidler uten å lykkes?", null, TestSvartype.LikertSkala,
            "0:Nei, aldri,2:Ja, i løpet av det siste året,4:Ja, men ikke i løpet av det siste året", cancellationToken);

        const string JaSkala = "0:Nei,2:Ja, men ikke i løpet av det siste året,4:Ja, i løpet av det siste året";
        await testService.LeggTilLeddAsync(side.Id, "10. Har du eller noen andre blitt skadet (fysisk eller psykisk) fordi du har brukt rusmidler?", null, TestSvartype.LikertSkala, JaSkala, cancellationToken);
        await testService.LeggTilLeddAsync(side.Id, "11. Har en slektning, venn, lege eller annet helsepersonell vært bekymret for rusbruken din, eller foreslått at du bør redusere?", null, TestSvartype.LikertSkala, JaSkala, cancellationToken);

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

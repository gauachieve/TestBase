namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// Regenererer ITQ (International Trauma Questionnaire) — screening av PTSD
/// og kompleks PTSD (KPTSD) jf. ICD-11. Ordrett norsk tekst fra den offisielle
/// norske oversettelsen (Bækkelund, H., Sele, P. &amp; Berg, A.O. (2019).
/// International Trauma Questionnaire (ITQ) Norwegian version. Forsknings-
/// instituttet, Modum Bad. www.modum-bad.no), hentet fra NKVTS sin åpne,
/// gratis publisering: https://www.nkvts.no/content/uploads/2020/05/ITQ.pdf.
/// Gratis og åpent tilgjengelig — INGEN "ikke offisielt oversatt"-merking.
/// IKKE juridisk/klinisk kvalitetssikret av oss, samme forbehold som de
/// øvrige innebygde testene.
/// </summary>
public sealed class ItqTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "itq";

    /// <summary>
    /// Kort, pasientvennlig navn (2026-09-20, jf. bugliste) — abbreviaturen
    /// beholdes til slutt slik at behandler fortsatt kan kjenne testen igjen.
    /// Ingen "ikke offisielt oversatt"-merknad her (se Test.OversettelseNotat)
    /// siden ITQ HAR en offisiell norsk oversettelse.
    /// </summary>
    public const string Navn = "Traumescreening ICD-11 ITQ";

    public const string SkalaSymptom = "0:Ingenting,1:Noe,2:Moderat,3:Mye,4:Ekstremt";

    private const string SkalaHendelsestid =
        "1:mindre enn 6 måneder siden,2:6 til 12 måneder siden,3:1 til 5 år siden,4:5 til 10 år siden," +
        "5:10 til 20 år siden,6:mer enn 20 år siden";

    private static readonly string[] PtsdSymptomer =
    {
        "Urovekkende drømmer der deler av hendelsen avspilles igjen og igjen, eller som er klart relatert til hendelsen?",
        "Sterke bilder eller minner som noen ganger kommer inn i bevisstheten din, og der du føler at det som hendte skjer igjen her-og-nå?",
        "Unngår indre påminnelser om den belastende hendelsen (for eksempel tanker, følelser eller kroppslige fornemmelser)?",
        "Unngår ytre påminnelser om den belastende hendelsen (for eksempel personer, steder, samtaletema, gjenstander, aktiviteter eller situasjoner)?",
        "Er årvåken, på utkikk eller på vakt?",
        "Føler deg skvetten eller lettskremt?"
    };

    private static readonly string[] PtsdFunksjon =
    {
        "Påvirket dine relasjoner eller ditt sosiale liv?",
        "Påvirket jobben din, eller din evne til å arbeide?",
        "Påvirket andre viktige områder av livet ditt, som foreldrerollen, skole, studier eller andre viktige aktiviteter?"
    };

    private static readonly string[] DsoSymptomer =
    {
        "Når jeg er opprørt, tar det meg lang tid å roe meg ned igjen.",
        "Jeg kjenner meg nummen eller følelsesmessig avstengt.",
        "Jeg føler meg mislykket.",
        "Jeg føler meg verdiløs.",
        "Jeg føler meg fjern eller avskåret fra andre mennesker.",
        "Jeg opplever det vanskelig å være følelsesmessig nær andre."
    };

    private static readonly string[] DsoFunksjon =
    {
        "Skapt bekymring eller uro i dine relasjoner eller ditt sosiale liv?",
        "Påvirket jobben din, eller din evne til å arbeide?",
        "Påvirket andre viktige områder av livet ditt, som foreldrerollen, skole, studier eller andre viktige aktiviteter?"
    };

    private const string Kategori = "Traumer, dissosiasjon og belastninger";

    private const string RapportIntroduksjonTekst =
        "ITQ (International Trauma Questionnaire) er et kort selvrapporteringsinstrument som måler kjernesymptomene " +
        "ved PTSD og kompleks PTSD (KPTSD) i tråd med ICD-11. Diagnostisk algoritme: PTSD krever minst ett symptom " +
        "fra hver av de tre PTSD-symptomgruppene (gjenopplevelse, unngåelse, følelse av overhengende fare) samt " +
        "funksjonstap knyttet til disse. KPTSD krever i tillegg minst ett symptom fra hver av de tre gruppene for " +
        "forstyrrelser i selvorganisering (affektiv dysregulering, negativ selvoppfattelse, relasjonsforstyrrelse) " +
        "samt funksjonstap knyttet til disse. En person kan kun få én av diagnosene, ikke begge. " +
        "Referanse: Cloitre, M., Shevlin, M., Brewin, C.R., Bisson, J.I., Roberts, N.P., Maercker, A., Karatzias, T., " +
        "Hyland, P. (2018). The International Trauma Questionnaire: Development of a self-report measure of ICD-11 " +
        "PTSD and Complex PTSD. Acta Psychiatrica Scandinavica. DOI: 10.1111/acps.12956.";

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
            beskrivelse: "Vennligst oppgi hendelsen som gir deg mest plager og besvar spørsmålene med tanke på " +
                         "denne hendelsen. Under finner du en liste over problemer som mennesker som har vært " +
                         "igjennom belastende eller traumatiske hendelser noen ganger opplever.",
            belonningstekst: "Takk for at du fylte ut ITQ. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var sideOm = await testService.LeggTilSideAsync(test.Id, "Om hendelsen", null, cancellationToken);
        await testService.LeggTilLeddAsync(sideOm.Id, "Kort beskrivelse av hendelsen", null, TestSvartype.Fritekst, null, cancellationToken);
        await testService.LeggTilLeddAsync(sideOm.Id, "Når skjedde hendelsen?", null, TestSvartype.LikertSkala, SkalaHendelsestid, cancellationToken);

        var sidePtsd = await testService.LeggTilSideAsync(
            test.Id, "Gjenopplevelse, unngåelse og fare",
            "Vennligst les hvert spørsmål nøye, og angi hvor mye du har vært plaget av det problemet den siste måneden.",
            cancellationToken);
        foreach (var tekst in PtsdSymptomer)
        {
            await testService.LeggTilLeddAsync(sidePtsd.Id, tekst, null, TestSvartype.LikertSkala, SkalaSymptom, cancellationToken);
        }

        var sidePtsdFunksjon = await testService.LeggTilSideAsync(
            test.Id, "Funksjon (PTSD-symptomer)", "I løpet av den siste måneden, har problemene over:", cancellationToken);
        foreach (var tekst in PtsdFunksjon)
        {
            await testService.LeggTilLeddAsync(sidePtsdFunksjon.Id, tekst, null, TestSvartype.LikertSkala, SkalaSymptom, cancellationToken);
        }

        var sideDso = await testService.LeggTilSideAsync(
            test.Id, "Følelser, selvbilde og relasjoner",
            "Spørsmålene handler om måter du typisk føler, måter du typisk tenker om deg selv på, og måter du " +
            "typisk forholder deg til andre. Besvar ved å tenke over hvor sann hver påstand er om deg.",
            cancellationToken);
        foreach (var tekst in DsoSymptomer)
        {
            await testService.LeggTilLeddAsync(sideDso.Id, tekst, null, TestSvartype.LikertSkala, SkalaSymptom, cancellationToken);
        }

        var sideDsoFunksjon = await testService.LeggTilSideAsync(
            test.Id, "Funksjon (selvorganisering)",
            "I løpet av den siste måneden, har disse problemene med følelser, oppfatninger om deg selv og i relasjoner:",
            cancellationToken);
        foreach (var tekst in DsoFunksjon)
        {
            await testService.LeggTilLeddAsync(sideDsoFunksjon.Id, tekst, null, TestSvartype.LikertSkala, SkalaSymptom, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
        await testService.SettIcdElleveKlarAsync(test.Id, true, cancellationToken);
    }
}

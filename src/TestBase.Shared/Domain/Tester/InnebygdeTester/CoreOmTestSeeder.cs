namespace TestBase.Shared.Domain.Tester.InnebygdeTester;

/// <summary>
/// CORE-OM (Clinical Outcomes in Routine Evaluation – Outcome Measure), den
/// fulle 34-ledds versjonen som CORE-10 (se Core10TestSeeder) er en
/// kortversjon av — opprinnelig Evans, Mellor-Clark, Barkham et al.
/// (2000/2002, CORE System Trust, University of Sheffield). Fritt
/// tilgjengelig for klinisk bruk med registrering, men ORDLYDEN er
/// opphavsrettslig beskyttet — denne versjonen er OMSKREVET/PARAFRASERT,
/// IKKE en verbatim kopi av det offisielle skjemaet. 4 domener: Subjektiv
/// velvære (4 ledd), Problemer/symptomer (12 ledd: angst/depresjon/fysisk/
/// traume), Livsfunksjon (12 ledd: generell/nære relasjoner/sosialt), Risiko
/// (6 ledd: risiko for seg selv 4, risiko for andre 2). 10 positivt
/// formulerte ledd reverse-skåres. Bør kvalitetssikres mot en offisiell,
/// lisensiert norsk oversettelse før reell klinisk bruk. Se
/// docs/beslutningslogg.md.
/// </summary>
public sealed class CoreOmTestSeeder : IInnebygdTestSeeder
{
    public string Kode => "core_om";

    private const string Skala = "0:Ikke i det hele tatt,1:Av og til,2:Noen ganger,3:Ofte,4:Hele tiden";

    private static readonly string[] SubjektivVelvare =
    {
        "1. Jeg har følt meg i stand til å mestre det når ting går galt.",
        "2. Jeg har følt meg optimistisk med tanke på fremtiden min.",
        "3. Jeg har følt meg overveldet av problemene mine.",
        "4. Jeg har følt meg fornøyd med livet mitt."
    };

    private static readonly string[] ProblemerSymptomer =
    {
        "5. Jeg har følt meg svært anspent, engstelig eller nervøs.",
        "6. Jeg har hatt vansker med å slappe av.",
        "7. Jeg har vært plaget av panikk eller sterk frykt uten åpenbar grunn.",
        "8. Jeg har følt meg nedfor eller trist.",
        "9. Jeg har følt meg skyldig uten grunn.",
        "10. Jeg har følt meg ensom.",
        "11. Jeg har hatt problemer med søvnen.",
        "12. Jeg har hatt vondt et eller annet sted i kroppen uten klar årsak.",
        "13. Jeg har følt meg fysisk sliten og uten energi.",
        "14. Jeg har blitt plaget av uønskede bilder eller minner.",
        "15. Jeg har unngått ting, steder eller personer som minner meg om noe vanskelig.",
        "16. Jeg har følt meg skvetten eller på vakt uten grunn."
    };

    private static readonly string[] Livsfunksjon =
    {
        "17. Jeg har klart å gjøre det meste av det jeg trengte å gjøre.",
        "18. Jeg har taklet hverdagslige oppgaver greit.",
        "19. Jeg har følt meg ute av stand til å gjøre noe som helst.",
        "20. Jeg har hatt gode, nære relasjoner til noen i livet mitt.",
        "21. Jeg har følt meg ensom eller isolert fra andre.",
        "22. Jeg har hatt konflikter eller vansker i nære relasjoner.",
        "23. Jeg har deltatt i sosiale aktiviteter jeg vanligvis liker.",
        "24. Jeg har unngått å være sammen med andre mennesker.",
        "25. Jeg har følt meg vel i sosiale sammenhenger.",
        "26. Jeg har klart å gå på jobb, skole eller studier som normalt.",
        "27. Jeg har hatt vansker med å konsentrere meg om oppgaver.",
        "28. Jeg har klart å ta vare på praktiske ting hjemme (f.eks. husarbeid, økonomi)."
    };

    private static readonly string[] Risiko =
    {
        "29. Jeg har hatt tanker om å skade meg selv.",
        "30. Jeg har følt at livet ikke er verdt å leve.",
        "31. Jeg har hatt planer om å ta livet mitt.",
        "32. Jeg har skadet meg selv fysisk.",
        "33. Jeg har vært nær ved å skade andre.",
        "34. Jeg har truet med å skade andre."
    };

    private const string Kategori = "Funksjon, livskvalitet og behandlingsutfall";

    private const string RapportIntroduksjonTekst =
        "CORE-OM (Clinical Outcomes in Routine Evaluation – Outcome Measure) er den fulle 34-ledds " +
        "versjonen av CORE-systemet, med 4 domener: subjektiv velvære, problemer/symptomer, " +
        "livsfunksjon og risiko. Risikoleddene (29-34) flagges alltid separat, uavhengig av totalskår.";

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
            navn: "CORE-OM (Psykisk helse, full versjon)",
            beskrivelse: "Tenk over den siste uken og kryss av det svaret som passer best for hvert utsagn.",
            belonningstekst: "Takk for at du fylte ut CORE-OM. Din behandler vil se over svarene dine.",
            kode: Kode,
            cancellationToken: cancellationToken);

        var sideSw = await testService.LeggTilSideAsync(test.Id, "Velvære", null, cancellationToken);
        foreach (var s in SubjektivVelvare)
        {
            await testService.LeggTilLeddAsync(sideSw.Id, s, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        var sideP = await testService.LeggTilSideAsync(test.Id, "Problemer og symptomer", null, cancellationToken);
        foreach (var s in ProblemerSymptomer)
        {
            await testService.LeggTilLeddAsync(sideP.Id, s, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        var sideF = await testService.LeggTilSideAsync(test.Id, "Livsfunksjon", null, cancellationToken);
        foreach (var s in Livsfunksjon)
        {
            await testService.LeggTilLeddAsync(sideF.Id, s, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        var sideR = await testService.LeggTilSideAsync(test.Id, "Risiko", null, cancellationToken);
        foreach (var s in Risiko)
        {
            await testService.LeggTilLeddAsync(sideR.Id, s, null, TestSvartype.LikertSkala, Skala, cancellationToken);
        }

        await testService.KoblTestTilKategoriAsync(test.Id, Kategori, cancellationToken);
        await testService.SettRapportIntroduksjonAsync(test.Id, RapportIntroduksjonTekst, cancellationToken);
    }
}

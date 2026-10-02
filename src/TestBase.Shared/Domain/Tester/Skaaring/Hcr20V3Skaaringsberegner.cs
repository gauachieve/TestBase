namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// HCR-20 V3-"skåring": IKKE en sumskår-utregning (se
/// <see cref="TestBase.Shared.Domain.Tester.InnebygdeTester.Hcr20V3TestSeeder"/> — metodikken er
/// strukturert profesjonelt skjønn). Rapportens hovedinnhold er klinikerens EGNE Trinn 7-
/// konklusjoner (Fremtidig vold/prioritering, Alvorlig fysisk skade, Umiddelbar vold, Annen risiko),
/// hentet direkte fra de fire siste leddene på testens siste side — ALDRI utledet fra en formel.
/// Råskår/-maks er en rent DESKRIPTIV kontekstopplysning (antall av de 20 historiske/kliniske/
/// risikohåndteringsfaktorene vurdert som Moderat/Høy relevans), IKKE risikokonklusjonen selv.
///
/// Leddene identifiseres ved å gruppere etter TestSideId (5 sider: Historisk, Klinisk,
/// Risikohåndtering, Formulering/scenarier/håndtering, Konklusjon) sortert etter LAVESTE
/// TestLeddId per gruppe (ekte, monotont stigende PK) — samme robusthetsmønster som
/// Scid5PfSkaaringsberegner. Innenfor Historisk/Klinisk/Risikohåndtering-sidene er leddene seedet
/// ÉN PER FAKTOR (kombinert "Tilstede og relevans for fremtidig risiko", se RETTET 2026-10-02 i
/// Hcr20V3TestSeeder) — hvert ledd leses direkte, ikke lenger i Tilstede/Relevans-par.
/// </summary>
public sealed class Hcr20V3Skaaringsberegner : ITestSkaaringsberegnerMedLedd
{
    public string TestKode => "hcr20_v3";

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("HCR-20 V3 krever ledd-informasjon for å skille Tilstede/Relevans-par og Trinn 7-konklusjonen — bruk BeregnSkaaringMedLedd.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd)
    {
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);
        int? Verdi(TestLedd? ledd) =>
            ledd is not null && svarPerLeddId.TryGetValue(ledd.Id, out var v) && int.TryParse(v, out var tall) ? tall : null;

        var sider = alleLedd
            .GroupBy(l => l.TestSideId)
            .OrderBy(g => g.Min(l => l.Id))
            .Select(g => g.OrderBy(l => l.Id).ToList())
            .ToList();

        var faktorSider = sider.Take(3).ToList(); // Historisk, Klinisk, Risikohåndtering
        var relevanteFaktorer = 0;
        var totaltAntallFaktorer = 0;

        foreach (var side in faktorSider)
        {
            foreach (var ledd in side)
            {
                totaltAntallFaktorer++;
                var relevansVerdi = Verdi(ledd);
                if (relevansVerdi is >= 2) // 2=Moderat, 3=Høy
                {
                    relevanteFaktorer++;
                }
            }
        }

        var konklusjonSide = sider.Count > 4 ? sider[4] : null;
        var trinn7 = konklusjonSide?.Count == 4 ? konklusjonSide : null;

        string TrinnLabel(int? verdi, string[] tekster) =>
            verdi is >= 0 && verdi < tekster.Length ? tekster[verdi.Value] : "(ikke vurdert)";

        var lavModHoy = new[] { "Lav", "Moderat", "Høy" };
        var neiMuligJa = new[] { "Nei", "Mulig", "Ja" };

        var fremtidigVold = TrinnLabel(Verdi(trinn7?[0]), lavModHoy);
        var alvorligSkade = TrinnLabel(Verdi(trinn7?[1]), lavModHoy);
        var umiddelbarVold = TrinnLabel(Verdi(trinn7?[2]), lavModHoy);
        var annenRisiko = TrinnLabel(Verdi(trinn7?[3]), neiMuligJa);

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Fremtidig vold/prioritering", fremtidigVold, fremtidigVold == "Lav"),
            new("Alvorlig fysisk skade", alvorligSkade, alvorligSkade == "Lav"),
            new("Umiddelbar vold", umiddelbarVold, umiddelbarVold == "Lav"),
            new("Annen risiko", annenRisiko, annenRisiko == "Nei")
        };

        var prosentSkaar = totaltAntallFaktorer == 0 ? 0 : (int)Math.Round(relevanteFaktorer * 100m / totaltAntallFaktorer);

        var fortolkning =
            $"Klinikerens strukturerte konklusjon (Trinn 7): fremtidig vold/prioritering = {fremtidigVold}, " +
            $"alvorlig fysisk skade = {alvorligSkade}, umiddelbar vold = {umiddelbarVold}, annen risiko = {annenRisiko}. " +
            $"({relevanteFaktorer}/{totaltAntallFaktorer} faktorer vurdert som Moderat/Høy relevans — REN KONTEKST, " +
            "IKKE grunnlaget for konklusjonen over, som er klinikerens eget strukturerte skjønn, ikke en sumformel.)";

        return new TestSkaaring(relevanteFaktorer, totaltAntallFaktorer, prosentSkaar, fortolkning, indikatorer);
    }
}

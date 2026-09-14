namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Skåring for WHO-5 VAS (jf. Who5VasTestSeeder): hvert ledd besvares 0–100
/// (glidebryter) i stedet for 0–5 (Likert), så råskår = sum av de fem svarene
/// (0–500) og prosentskår = råskår / 5 — matematisk identisk med gjennomsnittet
/// av de fem VAS-svarene, som gir samme 0–100-tolkning som den originale
/// Likert-baserte WHO-5 sin "råskår × 4".
///
/// Grenseverdiene er hentet DIREKTE fra normeringslitteraturen på selve
/// PROSENTSKALAEN (i motsetning til Likert-versjonen, som må runde til
/// nærmeste grenseverdi på en 0/4/8/…/100-skala): en prosentskår ≤ 50 er den
/// anbefalte terskelen for å screene videre for depresjon (Topp CW,
/// Østergaard SD, Søndergaard S, Bech P (2015). "The WHO-5 Well-Being Index:
/// A Systematic Review of the Literature." Psychotherapy and Psychosomatics,
/// 84(3), 167–176 — sensitivitet 79-88 %, spesifisitet 76-88 % avhengig av
/// populasjon), og ≤ 28 er en strengere, mye brukt
/// terskel for uttalt lav velvære/sannsynlig depresjon i samme litteratur.
/// Siden VAS gir en KONTINUERLIG skår (ikke bundet til trinn på 4), kan disse
/// to grenseverdiene her faktisk gi ULIKT utfall — i motsetning til den
/// diskrete Likert-versjonen, der de alltid følger hverandre (se
/// Who5Skaaringsberegner sin klassekommentar).
/// </summary>
public sealed class Who5VasSkaaringsberegner : ITestSkaaringsberegner
{
    private const int VelvaereGrense = 50;
    private const int DepresjonGrense = 28;

    /// <summary>Terskel for "lavt enkeltsvar" — samme kliniske resonnement som Likert-versjonens "0 eller 1 av 5", skalert til VAS sin 0–100-skala (1/5 × 100 = 20).</summary>
    private const int LavtEnkeltsvarGrense = 20;

    public string TestKode => "who5_vas";

    public IReadOnlyList<TestSkaaringReferanselinje> Referanselinjer { get; } = new[]
    {
        new TestSkaaringReferanselinje("Velvære", VelvaereGrense),
        new TestSkaaringReferanselinje("Depresjon", DepresjonGrense)
    };

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar)
    {
        var verdier = svar.Select(s => int.Parse(s.SvarVerdi)).ToList();
        var raaSkaar = verdier.Sum();
        var prosentSkaar = (int)Math.Round(raaSkaar / 5.0, MidpointRounding.AwayFromZero);
        var lavtEnkeltsvar = verdier.Any(v => v <= LavtEnkeltsvarGrense);
        var trengerUtredning = prosentSkaar <= VelvaereGrense || lavtEnkeltsvar;
        var sannsynligDepresjon = prosentSkaar <= DepresjonGrense;

        var fortolkning = trengerUtredning
            ? $"Prosentskår {prosentSkaar} (råskår {raaSkaar}/500). På eller under grenseverdien for " +
              $"velvære ({VelvaereGrense}) eller lav skår på ett eller flere enkeltspørsmål — WHO-5-litteraturen " +
              "anbefaler å gå videre med nærmere undersøkelse (f.eks. diagnostiske kriterier for depressiv episode)."
            : $"Prosentskår {prosentSkaar} (råskår {raaSkaar}/500). Over grenseverdien for velvære — " +
              "WHO-5-litteraturen anbefaler ikke å gå videre med nærmere undersøkelse på bakgrunn av denne testen alene.";

        var indikatorer = new List<TestSkaaringIndikator>
        {
            new("Velvære", prosentSkaar > VelvaereGrense ? "Velvære" : "Ikke velvære", prosentSkaar > VelvaereGrense),
            new("Depresjon", sannsynligDepresjon ? "Indikerer depresjon" : "Indikerer ikke depresjon", !sannsynligDepresjon)
        };

        return new TestSkaaring(raaSkaar, 500, prosentSkaar, fortolkning, indikatorer);
    }
}

using TestBase.Shared.Domain.Pasienter;

namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// MPFI-24 (se Mpfi24TestSeeder for itemtekst/kildehenvisning) — 12 delskalaer à 2 ledd, hver
/// skåret som gjennomsnitt (1-6), sammenlignet mot KJØNNSSPESIFIKKE normtall fra bruker-levert
/// "MPFI_24 skåring.xlsx". Normal-innslagspunktet for manglende kjønn er
/// Test.KreverBiologiskKjonn (håndhevet i TestTildelingsService FØR tildeling) — denne klassen er
/// et ANDRE forsvarslag: en null <paramref name="biologiskKjonn"/> her gir en tydelig
/// GyldighetsAdvarsel i stedet for å kaste (se ITestSkaaringsberegnerMedBiologiskKjonn).
///
/// Leddgruppering bruker ekte TestLedd.Id (ikke listeposisjon) — samme "MedLedd"-robusthetsmønster
/// som CORE-OM/SCID-5-PF, selv om denne testen (foreløpig) kun har én side: et hoppet-over ledd i
/// midten av skjemaet skal ikke kunne forskyve hvilket svar som havner i hvilken delskala.
///
/// INGEN sumskår på tvers av fleksibilitet+rigiditet (de er to UAVHENGIGE akser, ikke motpoler på
/// én skala — en respondent kan skåre høyt på begge, se Hexaflex-modellen) — SkjulProsent=true,
/// samme prinsipp som CORE-A/M.I.N.I. Rapporten viser i stedet to globalskårer (gjennomsnitt av de
/// 6 delskalaene i hver gruppe) og alle 12 delskårer, hver med kjønnets normtall og differanse.
/// </summary>
public sealed class Mpfi24Skaaringsberegner : ITestSkaaringsberegnerMedBiologiskKjonn
{
    public string TestKode => "mpfi_24";

    private sealed record Delskala(string Navn, int LeddIndeksA, int LeddIndeksB, double NormMenn, double NormKvinner, bool HoyereErBedre);

    // Leddindeks er 0-basert posisjon i testens 24 ledd, sortert — IKKE rå TestLedd.Id (som
    // varierer per database). Se BeregnSkaaringMedBiologiskKjonn for selve TestLedd.Id-oppslaget.
    private static readonly Delskala[] FleksibilitetDelskalaer =
    {
        new("Aksept", 0, 1, 3.5, 3.4, true),
        new("Nærvær", 2, 3, 3.9, 3.8, true),
        new("Selvet (perspektiv)", 4, 5, 4.0, 4.0, true),
        new("Defusjon", 6, 7, 3.6, 3.4, true),
        new("Verdier", 8, 9, 4.1, 4.1, true),
        new("Forpliktende handling", 10, 11, 4.1, 4.1, true)
    };

    // Fusjon-delskalaen er BEVISST ledd 19-20 (indeks 18,19) — IKKE regnearkets feilaktige
    // ledd 19-21, se Mpfi24TestSeeder sin kommentar om den funnede/rettede drag-fill-feilen.
    private static readonly Delskala[] RigiditetDelskalaer =
    {
        new("Opplevelsesunngåelse", 12, 13, 3.0, 3.2, false),
        new("Manglende kontakt med nået", 14, 15, 2.5, 2.6, false),
        new("Begrepsselv", 16, 17, 2.6, 2.7, false),
        new("Fusjon", 18, 19, 2.8, 3.0, false),
        new("Manglende kontakt med verdier", 20, 21, 2.6, 2.5, false),
        new("Passivitet", 22, 23, 2.5, 2.5, false)
    };

    private const double GlobalNormMennFleksibilitet = 3.9;
    private const double GlobalNormKvinnerFleksibilitet = 3.8;
    private const double GlobalNormMennRigiditet = 2.7;
    private const double GlobalNormKvinnerRigiditet = 2.7;

    public TestSkaaring BeregnSkaaring(IReadOnlyList<TestSvar> svar) =>
        throw new NotSupportedException("MPFI-24 krever ledd- og kjønnsinformasjon — bruk BeregnSkaaringMedBiologiskKjonn.");

    public TestSkaaring BeregnSkaaringMedLedd(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd) =>
        BeregnSkaaringMedBiologiskKjonn(svar, alleLedd, null);

    public TestSkaaring BeregnSkaaringMedBiologiskKjonn(IReadOnlyList<TestSvar> svar, IReadOnlyList<TestLedd> alleLedd, BiologiskKjonn? biologiskKjonn)
    {
        if (biologiskKjonn is null)
        {
            return new TestSkaaring(
                0, 0, 0,
                "Kan ikke beregne MPFI-24-resultat: pasientens biologiske kjønn er ikke registrert, " +
                "og normtabellen er kjønnsspesifikk. Fullfør pasientens profil (biologisk kjønn ved " +
                "fødsel) og åpne rapporten på nytt.",
                GyldighetsAdvarsel: "Mangler registrert biologisk kjønn — resultatet er IKKE beregnet.");
        }

        // Sortert liste, 0-basert indeks matcher Delskala sine LeddIndeksA/B direkte — samme
        // "laveste TestLeddId per side"-robusthet som Scid5PfSkaaringsberegner, selv om denne
        // testen (foreløpig) kun har én side.
        var sortertLedd = alleLedd.OrderBy(l => l.Id).ToList();
        var svarPerLeddId = svar.ToDictionary(s => s.TestLeddId, s => s.SvarVerdi);

        double? VerdiForIndeks(int indeks)
        {
            if (indeks >= sortertLedd.Count)
            {
                return null;
            }
            return svarPerLeddId.TryGetValue(sortertLedd[indeks].Id, out var v) && double.TryParse(v, System.Globalization.CultureInfo.InvariantCulture, out var tall)
                ? tall
                : null;
        }

        var erMann = biologiskKjonn == BiologiskKjonn.Mann;

        List<(string Navn, double? Skaare, double Norm, bool HoyereErBedre)> BeregnGruppe(Delskala[] delskalaer) =>
            delskalaer.Select(d =>
            {
                var a = VerdiForIndeks(d.LeddIndeksA);
                var b = VerdiForIndeks(d.LeddIndeksB);
                double? skaare = a is not null && b is not null ? (a.Value + b.Value) / 2.0 : null;
                return (d.Navn, skaare, erMann ? d.NormMenn : d.NormKvinner, d.HoyereErBedre);
            }).ToList();

        var fleksibilitet = BeregnGruppe(FleksibilitetDelskalaer);
        var rigiditet = BeregnGruppe(RigiditetDelskalaer);

        var fleksibilitetSkaarer = fleksibilitet.Where(d => d.Skaare is not null).Select(d => d.Skaare!.Value).ToList();
        var rigiditetSkaarer = rigiditet.Where(d => d.Skaare is not null).Select(d => d.Skaare!.Value).ToList();
        var globalFleksibilitet = fleksibilitetSkaarer.Count > 0 ? fleksibilitetSkaarer.Average() : (double?)null;
        var globalRigiditet = rigiditetSkaarer.Count > 0 ? rigiditetSkaarer.Average() : (double?)null;
        var globalNormFleksibilitet = erMann ? GlobalNormMennFleksibilitet : GlobalNormKvinnerFleksibilitet;
        var globalNormRigiditet = erMann ? GlobalNormMennRigiditet : GlobalNormKvinnerRigiditet;

        string F(double tall) => tall.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        string Diff(double skaare, double norm) => skaare >= norm ? $"+{F(skaare - norm)}" : F(skaare - norm);

        var indikatorer = new List<TestSkaaringIndikator>();
        foreach (var d in fleksibilitet)
        {
            if (d.Skaare is null)
            {
                continue;
            }
            indikatorer.Add(new TestSkaaringIndikator(
                $"Fleksibilitet — {d.Navn}", $"{F(d.Skaare.Value)}/{F(d.Norm)}", d.Skaare.Value >= d.Norm));
        }
        foreach (var d in rigiditet)
        {
            if (d.Skaare is null)
            {
                continue;
            }
            indikatorer.Add(new TestSkaaringIndikator(
                $"Rigiditet — {d.Navn}", $"{F(d.Skaare.Value)}/{F(d.Norm)}", d.Skaare.Value <= d.Norm));
        }
        if (globalFleksibilitet is not null)
        {
            indikatorer.Add(new TestSkaaringIndikator("Fleksibilitet — global", $"{F(globalFleksibilitet.Value)}/{F(globalNormFleksibilitet)}", globalFleksibilitet.Value >= globalNormFleksibilitet));
        }
        if (globalRigiditet is not null)
        {
            indikatorer.Add(new TestSkaaringIndikator("Rigiditet — global", $"{F(globalRigiditet.Value)}/{F(globalNormRigiditet)}", globalRigiditet.Value <= globalNormRigiditet));
        }

        var kjonnTekst = erMann ? "menn" : "kvinner";
        var fortolkning = globalFleksibilitet is not null && globalRigiditet is not null
            ? $"Global fleksibilitet: {F(globalFleksibilitet.Value)} (norm {kjonnTekst}: {F(globalNormFleksibilitet)}, differanse {Diff(globalFleksibilitet.Value, globalNormFleksibilitet)}). " +
              $"Global rigiditet: {F(globalRigiditet.Value)} (norm {kjonnTekst}: {F(globalNormRigiditet)}, differanse {Diff(globalRigiditet.Value, globalNormRigiditet)}). " +
              "Fleksibilitet og rigiditet er to UAVHENGIGE akser (Hexaflex-modellen), ikke motpoler på én skala — se delskårene under for detaljer per prosess."
            : "Ikke nok besvarte ledd til å beregne en global skåre.";

        return new TestSkaaring(0, 0, 0, fortolkning, indikatorer, SkjulProsent: true);
    }
}

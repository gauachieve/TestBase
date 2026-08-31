namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Resultatet av å skåre én fullført TestTildeling. <paramref name="Indikatorer"/>
/// er valgfrie, navngitte kategoriske konklusjoner utover selve tallskåren
/// (se TestSkaaringIndikator) — null/tom for tester som ikke har noen (de
/// fleste), populert av f.eks. Who5Skaaringsberegner.
/// </summary>
public sealed record TestSkaaring(
    int RaaSkaar, int RaaSkaarMaks, int ProsentSkaar, string Fortolkning,
    IReadOnlyList<TestSkaaringIndikator>? Indikatorer = null);

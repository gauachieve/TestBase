namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Navngitt horisontal referanselinje fra normeringslitteraturen (f.eks. WHO-5
/// sine etablerte grenseverdier for velvære/depresjon) — vist i "utvikling
/// over tid"-grafen når en pasient har fylt ut samme test flere ganger. Rent
/// valgfritt per test, se ITestSkaaringsberegner.Referanselinjer — de fleste
/// tester har ingen og trenger ikke endres for å understøtte dette.
/// </summary>
public sealed record TestSkaaringReferanselinje(string Navn, int ProsentVerdi);

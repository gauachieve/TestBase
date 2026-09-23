namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// Én navngitt grenseverdi/cutoff fra normeringslitteraturen, i SAMME enhet som
/// testen selv skal vises i (se ITestSkaaringsberegner.VisSomProsentIHistogram) —
/// råskår for de fleste tester (f.eks. GADIT sin cutoff på 5 av 8), prosent for
/// de få testene der litteraturen selv rapporterer i prosent (WHO-5/WHO-5 VAS).
/// Brukt av grupperapportens histogram ("Spredning") til å tegne en merket
/// vertikal linje — se ITestSkaaringsberegner.Histogramgrenser og
/// docs/beslutningslogg.md. Tom liste er normalen; de fleste tester har enten
/// ingen enkelt cutoff-tall eller flere sammensatte domene-grenser som ikke lar
/// seg oppsummere i én linje.
/// </summary>
public sealed record TestSkaaringGrenseverdi(string Navn, int Verdi);

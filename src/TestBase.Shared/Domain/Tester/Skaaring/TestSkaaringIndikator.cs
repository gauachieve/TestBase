namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>
/// En navngitt, kategorisk konklusjon utover selve rå-/prosentskåren (f.eks.
/// WHO-5s "velvære"/"depresjon"-indikatorer, se Who5Skaaringsberegner) —
/// generisk nok til at andre testers skåringsberegnere også kan levere sine
/// egne, ikke WHO-5-spesifikk. Rendres som fremhevede "badges" i rapporten
/// (se Behandlerportal/Pasienter/Rapport.cshtml). <see cref="Positiv"/> styrer
/// fargen (grønn/rød) — betyr klinisk gunstig utfall, IKKE nødvendigvis en høy
/// tallverdi (f.eks. "Indikerer ikke depresjon" er positivt).
/// </summary>
public sealed record TestSkaaringIndikator(string Navn, string Verdi, bool Positiv);

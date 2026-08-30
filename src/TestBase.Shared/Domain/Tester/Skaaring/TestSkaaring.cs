namespace TestBase.Shared.Domain.Tester.Skaaring;

/// <summary>Resultatet av å skåre én fullført TestTildeling.</summary>
public sealed record TestSkaaring(int RaaSkaar, int RaaSkaarMaks, int ProsentSkaar, string Fortolkning);

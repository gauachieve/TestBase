namespace TestBase.Shared.Domain.Tester;

/// <summary>
/// Ett sete-prissjikt for klinikk-abonnementet (se docs/beslutningslogg.md
/// "Abonnement/subscription-system") — superadmin-redigerbar liste, lest av
/// KlinikkPrisService. Antall aktive (ikke-arkiverte) behandlere knyttet til
/// en Partner avgjør hvilket sjikt som gjelder (ikke gradert — hele
/// klinikken prises til SAMME kr/sete, bestemt av total headcount).
/// </summary>
public sealed class KlinikkSeterTier
{
    public long Id { get; set; }

    /// <summary>Nedre grense (inklusiv) for antall seter dette sjiktet gjelder for.</summary>
    public int FraAntallSeter { get; set; }

    /// <summary>Øvre grense (inklusiv) — null betyr "og oppover" (høyeste sjikt).</summary>
    public int? TilAntallSeter { get; set; }

    public decimal KrPerSeteManed { get; set; }
}

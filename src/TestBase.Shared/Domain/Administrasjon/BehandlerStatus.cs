namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Livssyklusen til en behandler-konto, jf. Del 2 i kravdokumentet:
/// administrator inviterer → behandler fullfører egne stamdata → aktiv →
/// evt. fryst eller arkivert av administrator senere.
/// </summary>
public enum BehandlerStatus
{
    Invitert,
    Aktiv,
    Fryst,
    Arkivert
}

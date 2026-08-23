namespace TestBase.Shared.Domain.Pasienter;

/// <summary>
/// Jf. Del 3/4 i kravdokumentet: en pasient forblir <see cref="Invitert"/> til
/// Del 4 (pasientsystemet) bygger pasientens egen fullføringsside — samme
/// mønster som Behandler i fase 2 før fase 3 bygde videre på den.
/// </summary>
public enum PasientStatus
{
    Invitert,
    Aktiv,
    Arkivert
}

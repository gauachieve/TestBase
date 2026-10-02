namespace TestBase.Shared.Domain.Administrasjon;

/// <summary>
/// Singleton-rad (alltid Id=1) som styrer om admin/behandler sin FELLES
/// innloggingsside (Pages/Konto/LoggInn) faktisk BRUKER ekte BankID akkurat
/// nå — lest per kall av LoggInnModel, IKKE cachet ved oppstart, slik at en
/// Superadmin/Utvikler kan slå ekte BankID av/på uten `azd provision`/omstart
/// (se Admin/MinSide). ATSKILT fra `Miljo:EktBankIdProfesjonell`
/// (azd-miljøvariabel): den konfig-flagget avgjør om selve OIDC-schemaet i
/// det hele tatt REGISTRERES ved oppstart (krever fortsatt omstart — en
/// ASP.NET Core-autentiseringsscheme kan ikke legges til dynamisk), mens
/// denne raden avgjør om LoggInnModel faktisk UTFORDRER til det allerede
/// registrerte schemaet eller faller tilbake til MockBankIdProvider.
/// `ErAktiv` defaulter til `true` (IKKE samme Mock-first-prinsipp som
/// BetaBetalingsinnstilling) fordi raden ble introdusert mens ekte BankID
/// allerede var aktivt og ønsket på live — en default på `false` ville gitt
/// en overraskende regresjon til mock ved første deploy av denne funksjonen.
/// Se docs/beslutningslogg.md "Ekte BankID — driftsbryter uten redeploy".
/// </summary>
public sealed class EktBankIdInnstilling
{
    public long Id { get; set; }
    public bool ErAktiv { get; set; } = true;
    public long? SistEndretAvUserId { get; set; }
    public DateTimeOffset? SistEndretUtc { get; set; }
}

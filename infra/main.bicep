targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Navn på azd-miljøet (brukes til å avlede ressursnavn)')
param environmentName string

@minLength(1)
@description('Primær Azure-region for alle ressurser')
param location string

@description('Administrator-brukernavn for MySQL Flexible Server')
param mysqlAdministratorLogin string = 'testbaseadmin'

@description('Delt nøkkel for StagingGate (se Security/StagingGate.cs) — tom verdi deaktiverer sperren. Settes via azd-miljøvariabelen STAGING_GATE_ACCESS_KEY, ALDRI som literal her (unngår at nøkkelen havner i kildekontroll).')
@secure()
param stagingGateAccessKey string = ''

@description('SMS-avsendernavn (f.eks. "PsyTest") — tom verdi gir MockSmsSender. Settes via azd-miljøvariabelen SMS_SENDER_ID (se docs/beslutningslogg.md "SMS-integrasjon").')
param smsSenderId string = ''

@description('Vonage API-nøkkel for SMS — tom verdi gir MockSmsSender. Settes via azd-miljøvariabelen VONAGE_API_KEY.')
@secure()
param vonageApiKey string = ''

@description('Vonage API-hemmelighet for SMS — tom verdi gir MockSmsSender. Settes via azd-miljøvariabelen VONAGE_API_SECRET, ALDRI som literal her.')
@secure()
param vonageApiSecret string = ''

@description('Idura (BankID-testintegrasjon) OIDC Authority, f.eks. https://psytest.test.idura.broker — tom verdi deaktiverer BankID-testintegrasjonen (se docs/beslutningslogg.md).')
param bankIdIduraAuthority string = ''

@description('Idura Client ID for BankID-testintegrasjonen — ikke hemmelig, men settes via azd-miljøvariabel for konsistens.')
param bankIdIduraClientId string = ''

@description('Idura Client Secret for BankID-testintegrasjonen — settes via azd-miljøvariabelen BANKID_IDURA_CLIENT_SECRET, ALDRI som literal her.')
@secure()
param bankIdIduraClientSecret string = ''

@description('Ekte personnummer for en seedet administrator-konto (IKKE syntetisk testdata) — tom verdi deaktiverer seedingen. Settes via azd-miljøvariabelen SEED_ADMIN_PERSONNUMMER, ALDRI som literal her (se docs/beslutningslogg.md "Seed av brukerens egen admin-konto").')
@secure()
param seedAdminPersonnummer string = ''

@description('Fullt navn for den seedede administrator-kontoen. Settes via azd-miljøvariabelen SEED_ADMIN_NAVN, ALDRI som literal her.')
@secure()
param seedAdminNavn string = ''

@description('Mobilnummer (for ekte SMS-2FA) for den seedede administrator-kontoen. Settes via azd-miljøvariabelen SEED_ADMIN_MOBILNR, ALDRI som literal her.')
@secure()
param seedAdminMobilNr string = ''

@description('E-post (for ekte e-post-2FA) for den seedede administrator-kontoen. Settes via azd-miljøvariabelen SEED_ADMIN_EPOST, ALDRI som literal her.')
@secure()
param seedAdminEpost string = ''

var resourceToken = uniqueString(subscription().id, environmentName, location)
var tags = {
  'azd-env-name': environmentName
}

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    location: location
    resourceToken: resourceToken
    tags: tags
    mysqlAdministratorLogin: mysqlAdministratorLogin
    stagingGateAccessKey: stagingGateAccessKey
    smsSenderId: smsSenderId
    vonageApiKey: vonageApiKey
    vonageApiSecret: vonageApiSecret
    bankIdIduraAuthority: bankIdIduraAuthority
    bankIdIduraClientId: bankIdIduraClientId
    bankIdIduraClientSecret: bankIdIduraClientSecret
    seedAdminPersonnummer: seedAdminPersonnummer
    seedAdminNavn: seedAdminNavn
    seedAdminMobilNr: seedAdminMobilNr
    seedAdminEpost: seedAdminEpost
  }
}

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output SERVICE_WEB_ENDPOINT_URL string = resources.outputs.appServiceUrl
output AZURE_KEY_VAULT_NAME string = resources.outputs.keyVaultName
output MYSQL_SERVER_NAME string = resources.outputs.mysqlServerName

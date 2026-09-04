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

@description('Forhåndsregistrert alfanumerisk SMS-avsender-ID (f.eks. "PsyTest") — tom verdi gir MockSmsSender. Settes via azd-miljøvariabelen SMS_SENDER_ID når Azure har godkjent søknaden (se docs/beslutningslogg.md "SMS-integrasjon").')
param smsSenderId string = ''

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
  }
}

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output SERVICE_WEB_ENDPOINT_URL string = resources.outputs.appServiceUrl
output AZURE_KEY_VAULT_NAME string = resources.outputs.keyVaultName
output MYSQL_SERVER_NAME string = resources.outputs.mysqlServerName

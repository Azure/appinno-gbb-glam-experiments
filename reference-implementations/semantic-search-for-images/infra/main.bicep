targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

param ingestionExists bool
@secure()
param ingestionDefinition object
param uiBackendExists bool
@secure()
param uiBackendDefinition object
param uiFrontendExists bool
@secure()
param uiFrontendDefinition object

@description('Id of the user or app to assign application roles')
param principalId string

param ingestionTriggerBlobContainerName string = 'images'

// Tags that should be applied to all resources.
// 
// Note that 'azd-service-name' tags should be applied separately to service host resources.
// Example usage:
//   tags: union(tags, { 'azd-service-name': <service name in azure.yaml> })
var tags = {
  'azd-env-name': environmentName
}

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module monitoring './shared/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
  }
  scope: rg
}

module dashboard './shared/dashboard-web.bicep' = {
  name: 'dashboard'
  params: {
    name: '${abbrs.portalDashboards}${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    location: location
    tags: tags
  }
  scope: rg
}

module registry './shared/registry.bicep' = {
  name: 'registry'
  params: {
    location: location
    tags: tags
    name: '${abbrs.containerRegistryRegistries}${resourceToken}'
  }
  scope: rg
}

module keyVault './shared/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    tags: tags
    name: '${abbrs.keyVaultVaults}${resourceToken}'
    principalId: principalId
  }
  scope: rg
}

module cosmos './shared/cosmos-account.bicep' = {
  name: 'cosmos'
  params: {
    keyVaultName: keyVault.outputs.name
    kind: 'GlobalDocumentDB'
    name: '${abbrs.documentDBDatabaseAccounts}${resourceToken}'
    previewCapabilities: [ { name: 'EnableNoSQLVectorSearch' } ]
  }
  scope: rg
}

module storage './shared/storage-account.bicep' = {
  name: 'storage'
  params: {
    name: '${abbrs.storageStorageAccounts}${resourceToken}'
    containers: [
      { name: ingestionTriggerBlobContainerName }
      { name: 'processed' }
    ]
  }
  scope: rg
}

module aiServices './shared/ai-services.bicep' = {
  name: 'ai-services'
  params: {
    name: '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    kind: 'CognitiveServices'
  }
  scope: rg
}

module appsEnv './shared/apps-env.bicep' = {
  name: 'apps-env'
  params: {
    name: '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    tags: tags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    logAnalyticsWorkspaceName: monitoring.outputs.logAnalyticsWorkspaceName
  }
  scope: rg
}

module ingestion './app/ingestion.bicep' = {
  name: 'ingestion'
  params: {
    name: '${abbrs.appContainerApps}ingestion-${resourceToken}'
    location: location
    tags: tags
    identityName: '${abbrs.managedIdentityUserAssignedIdentities}ingestion-${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    containerAppsEnvironmentName: appsEnv.outputs.name
    containerRegistryName: registry.outputs.name
    exists: ingestionExists
    appDefinition: ingestionDefinition
    additionalAppSettings: [
      {
        name: 'AppSettings__StorageAccount__Uri'
        value: storage.outputs.primaryEndpoints.blob
      }
      {
        name: 'AppSettings__CosmosDb__Uri'
        value: cosmos.outputs.endpoint
      }
      {
        name: 'AppSettings__AiServices__Uri'
        value: aiServices.outputs.endpoint
      }
    ]
    triggerStorageAccountBlobContainerName: ingestionTriggerBlobContainerName
    triggerStorageAccountName: storage.outputs.name
  }
  scope: rg
}

module uiBackend './app/ui-backend.bicep' = {
  name: 'ui-backend'
  params: {
    name: '${abbrs.appContainerApps}ui-backend-${resourceToken}'
    location: location
    tags: tags
    identityName: '${abbrs.managedIdentityUserAssignedIdentities}ui-backend-${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    containerAppsEnvironmentName: appsEnv.outputs.name
    containerRegistryName: registry.outputs.name
    exists: uiBackendExists
    appDefinition: uiBackendDefinition
    allowedOrigins: [
      'https://${abbrs.appContainerApps}ui-frontend-${resourceToken}.${appsEnv.outputs.domain}'
    ]
    additionalAppSettings: [
      {
        name: 'AppSettings__CosmosDb__Uri'
        value: cosmos.outputs.endpoint
      }
      {
        name: 'AppSettings__AiServices__Uri'
        value: aiServices.outputs.endpoint
      }
    ]
  }
  scope: rg
}

module uiFrontend './app/ui-frontend.bicep' = {
  name: 'ui-frontend'
  params: {
    name: '${abbrs.appContainerApps}ui-frontend-${resourceToken}'
    location: location
    tags: tags
    identityName: '${abbrs.managedIdentityUserAssignedIdentities}ui-frontend-${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    containerAppsEnvironmentName: appsEnv.outputs.name
    containerRegistryName: registry.outputs.name
    exists: uiFrontendExists
    appDefinition: uiFrontendDefinition
    apiUrls: [
      uiBackend.outputs.uri
    ]
  }
  scope: rg
}

module security './app/security.bicep' = {
  name: 'rbac-security'
  params: {
    backendAppPrincipalId: uiBackend.outputs.principalId
    cognitiveServicesAccountName: aiServices.outputs.name
    cosmosAccountName: cosmos.outputs.name
    frontendAppPrincipalId: uiFrontend.outputs.principalId
    ingestionAppPrincipalId: ingestion.outputs.principalId
    storageAccountName: storage.outputs.name
  }
  scope: rg
}

output AZURE_CONTAINER_REGISTRY_ENDPOINT string = registry.outputs.loginServer
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.name
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.outputs.endpoint
output AZURE_COSMOSDB_ACCOUNT_NAME string = cosmos.outputs.name
output AZURE_RESOURCE_GROUP string = rg.name
output SERVICE_INGESTION_JOB_NAME string = ingestion.outputs.name
output SERVICE_UI_BACKEND_ENDPOINT string = uiBackend.outputs.uri

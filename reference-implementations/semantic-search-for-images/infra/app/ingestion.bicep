param name string
param location string = resourceGroup().location
param tags object = {}

param identityName string
param containerRegistryName string
param containerAppsEnvironmentName string
param applicationInsightsName string
param exists bool
@secure()
param appDefinition object
param additionalAppSettings array

param triggerStorageAccountName string
param triggerStorageAccountBlobContainerName string

var appSettingsArray = filter(array(appDefinition.settings), i => i.name != '')
var secrets = map(filter(appSettingsArray, i => i.?secret != null), i => {
  name: i.name
  value: i.value
  secretRef: i.?secretRef ?? take(replace(replace(toLower(i.name), '_', '-'), '.', '-'), 32)
})
var env = union(map(filter(appSettingsArray, i => i.?secret == null), i => {
  name: i.name
  value: i.value
}), additionalAppSettings)

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: containerRegistryName
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: containerAppsEnvironmentName
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: triggerStorageAccountName
}

resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: containerRegistry
  name: guid(subscription().id, resourceGroup().id, identity.id, 'acrPullRole')
  properties: {
    roleDefinitionId:  subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalType: 'ServicePrincipal'
    principalId: identity.properties.principalId
  }
}

module fetchLatestImage '../modules/fetch-container-image-job.bicep' = {
  name: '${name}-fetch-image'
  params: {
    exists: exists
    name: name
  }
}

resource job 'Microsoft.App/jobs@2023-05-02-preview' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': 'ingestion' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    configuration: {
      replicaTimeout: 86400 // 24 hours (large ingestion files take time)
      replicaRetryLimit: 1
      triggerType: 'Event'
      eventTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
        scale: {
          minExecutions: 0
          maxExecutions: 10
          pollingInterval: 60
          rules: [
            {
              name: 'azure-blob-trigger-scale-rule'
              type: 'azure-blob'
              auth: [
                {
                  secretRef: 'storage-account-connection-string'
                  triggerParameter: 'connection'
                }
              ]
              metadata: {
                blobContainerName: triggerStorageAccountBlobContainerName
                blobCount: '2'
              }
            }
          ]
        }
      }
      registries: [
        {
          server: '${containerRegistryName}.azurecr.io'
          identity: identity.id
        }
      ]
      secrets: union(
        [
          {
            name: 'storage-account-connection-string'
            value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
          }
        ],
        map(secrets, secret => 
          {
            name: secret.secretRef
            value: secret.value
          }
        )
      )
    }
    template: {
      containers: [
        {
          image: fetchLatestImage.outputs.?containers[?0].?image ?? 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          name: 'main'
          env: union(
            [
              {
                name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
                value: applicationInsights.properties.ConnectionString
              }
              {
                name: 'PORT'
                value: '80'
              }
              {
                name: 'AZURE_CLIENT_ID'
                value: identity.properties.clientId
              }
            ],
            env,
            map(secrets, secret => 
              {
                name: secret.name
                secretRef: secret.secretRef
              }
            )
          )
          resources: {
            cpu: json('1.0')
            memory: '2.0Gi'
          }
        }
      ]
    }
  }
}

output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
output name string = job.name
output id string = job.id
output principalId string = identity.properties.principalId

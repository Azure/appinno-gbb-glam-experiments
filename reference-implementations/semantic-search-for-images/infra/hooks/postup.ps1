# -------------------------------------------------------
# postup.ps1
# -------------------------------------------------------
# This is required because Azure Developer CLI does not
# support deployment of Azure Container Apps Jobs at the
# time this was written. See referenced feature request:
# [https://github.com/Azure/azure-dev/issues/2743]
# -------------------------------------------------------

Write-Host ""
Write-Host ("**Deploying ingestion service (azd hook: postup)**" | ConvertFrom-Markdown -AsVT100EncodedString).VT100EncodedString
Write-Host "  - Loading azd .env parameters from current environment"

$acrEndpoint = azd env get-value AZURE_CONTAINER_REGISTRY_ENDPOINT
$rg = azd env get-value AZURE_RESOURCE_GROUP
$jobName = azd env get-value SERVICE_INGESTION_JOB_NAME

Write-Host "  - Push lastest packaged azd-deploy-* tagged image to ACR"

# Login, tag, and push container image to ACR
$imageId = docker images -q semantic-search-for-images-ingestion
$imageName = docker image inspect $imageId --format="{{index .RepoTags 1}}"
$acrName = $acrEndpoint.Substring(0, $acrEndpoint.IndexOf("."))
$null = az acr login --name $acrName

$azureImageName = $acrEndpoint + "/" + $imageName
$null = docker tag $imageName $azureImageName

$null = docker push $azureImageName --quiet

# Update local AZD .env
Write-Host "  - Setting the service image name in environment after publish"
azd env set SERVICE_INGESTION_IMAGE_NAME $azureImageName

# Update ingestion Container App Job
Write-Host "  - Update Azure Container App Job"
$null = az containerapp job update --output none --only-show-errors `
    --resource-group $rg `
    --name $jobName `
    --image $azureImageName

# Run once for database initialization
Write-Host "  - Run once for database initialization"
$null = az containerapp job start --output none --only-show-errors `
    --resource-group $rg `
    --name $jobName

# Success!
Write-Host "  (✓) Done: " -ForegroundColor Green -NoNewLine
Write-Host "Deploying ingestion service"

# Print out the storage account URI for easy access
$storage_upload_container_uri = azd env get-value STORAGE_UPLOAD_CONTAINER_URI
Write-Host ""
Write-Host ("Upload your CSV file to seed your data - visit the **images** container and select **Upload**:" | ConvertFrom-Markdown -AsVT100EncodedString).VT100EncodedString
Write-Host "  $storage_upload_container_uri"

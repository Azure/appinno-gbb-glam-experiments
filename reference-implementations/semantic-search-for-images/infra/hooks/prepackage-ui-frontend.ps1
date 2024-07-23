# -------------------------------------------------------
# prepackage-ui-frontend.ps1
# -------------------------------------------------------
# This is required because React binds to environment
# variables at build time.
# -------------------------------------------------------

Write-Host ""
Write-Host ("**Preparing ui-frontend for packaging (azd hook: prepackage-ui-frontend)**" | ConvertFrom-Markdown -AsVT100EncodedString).VT100EncodedString
Write-Host "  - Loading azd .env parameters from current environment"

# Use get-value
$backendEndpoint = azd env get-value SERVICE_UI_BACKEND_ENDPOINT

# Create .env.local and append needed values
Write-Host "  - Write a temporary ./src/ui-frontend/.env.local file"

$textApi = "REACT_APP_AZURE_TEXT_API_URL=" + $backendEndpoint + "/api/SemanticSearch/text"
$null = New-Item -path "src\ui-frontend" -name ".env.local" -type "file" -value $textApi -Force
$imageStreamApi = "`nREACT_APP_AZURE_IMAGE_API_URL=" + $backendEndpoint + "/api/SemanticSearch/imageStream"
$null = Add-Content -path "src\ui-frontend\.env.local" -value $imageStreamApi

# Success!
Write-Host "  (✓) Done: " -ForegroundColor Green -NoNewLine
Write-Host "Preparing ui-frontend for packaging"
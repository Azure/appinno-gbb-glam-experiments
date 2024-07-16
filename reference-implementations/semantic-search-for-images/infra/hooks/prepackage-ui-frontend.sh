set -e

# -------------------------------------------------------
# prepackage-ui-frontend.sh
# -------------------------------------------------------
# This is required because React binds to environment
# variables at build time.
# -------------------------------------------------------

echo "\033[1mPreparing ui-frontend for packaging (azd hook: prepackage-ui-frontend)\033[0m"
echo ""
echo "  - Loading azd .env file from current environment"

# Use the `get-values` azd command to retrieve environment variables from the `.env` file
while IFS='=' read -r key value; do
    value=$(echo "$value" | sed 's/^"//' | sed 's/"$//')
    export "$key=$value"
done <<EOF
$(azd env get-values) 
EOF

# Write required env vars to local .env.local
echo "  - Write a temporary ./src/ui-frontend/.env.local file"
echo "REACT_APP_AZURE_TEXT_API_URL=\"${SERVICE_UI_BACKEND_ENDPOINT}/api/SemanticSearch/text\"" > ./src/ui-frontend/.env.local
echo "REACT_APP_AZURE_IMAGE_API_URL=\"${SERVICE_UI_BACKEND_ENDPOINT}/api/SemanticSearch/imageStream\"" >> ./src/ui-frontend/.env.local

# Success!
echo "  \033[0;32m(✓) Done:\033[0m Preparing ui-frontend for packaging"

set -e

# -------------------------------------------------------
# postup.sh
# -------------------------------------------------------
# This is required because Azure Developer CLI does not
# support deployment of Azure Container Apps Jobs at the
# time this was written. See referenced feature request:
# [https://github.com/Azure/azure-dev/issues/2743]
# -------------------------------------------------------

echo "\033[1mDeploying ingestion service (azd hook: postup)\033[0m"
echo ""
echo "  - Loading azd .env file from current environment"

# Use the `get-values` azd command to retrieve environment variables from the `.env` file
while IFS='=' read -r key value; do
    value=$(echo "$value" | sed 's/^"//' | sed 's/"$//')
    export "$key=$value"
done <<EOF
$(azd env get-values) 
EOF

# Login, tag, and push container image to ACR
echo "  - Push latest packaged azd-deploy-* tagged image to ACR"

IMAGE_ID=$(docker images -q semantic-search-for-images-ingestion)
IMAGE_NAME=$(docker image inspect $IMAGE_ID --format="{{index .RepoTags 1}}")
ACR_NAME=$(echo $AZURE_CONTAINER_REGISTRY_ENDPOINT | cut -d'.' -f 1)
az acr login --name $ACR_NAME > /dev/null

AZURE_IMAGE_NAME="$AZURE_CONTAINER_REGISTRY_ENDPOINT/$IMAGE_NAME"
docker tag $IMAGE_NAME $AZURE_IMAGE_NAME > /dev/null

docker push $AZURE_IMAGE_NAME --quiet > /dev/null

# Update local .env
echo "  - Setting the service image name in environment after publish"
azd env set SERVICE_INGESTION_IMAGE_NAME $AZURE_IMAGE_NAME > /dev/null

# Update ingestion Container App Job
echo "  - Update Azure Container App Job"
az containerapp job update --output none --only-show-errors \
    --resource-group "$AZURE_RESOURCE_GROUP" \
    --name "$SERVICE_INGESTION_JOB_NAME" \
    --image "$AZURE_IMAGE_NAME" \
    > /dev/null

# Run once (to initialize database)
echo "  - Run once for database initialization"
az containerapp job start --output none --only-show-errors \
    --resource-group "$AZURE_RESOURCE_GROUP" \
    --name "$SERVICE_INGESTION_JOB_NAME" \
    > /dev/null

# Success!
echo "  \033[0;32m(✓) Done:\033[0m Deploying ingestion service"
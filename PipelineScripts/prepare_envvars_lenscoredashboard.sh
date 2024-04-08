#!/bin/bash

# Check if an argument is provided
if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <environment> <filename>"
    exit 1
fi

# Define the environment argument
ENVIRONMENT=$1
FILENAME=$2

# Define a mapping of Docker env vars to GitLab CI/CD env vars for different environments
declare -A env_var_mapping

case $ENVIRONMENT in
    production)
        env_var_mapping=(
            ["LENS_CORE_DASHBOARD_PORT"]="LENS_CORE_DASHBOARD_PORT_PROD"
            ["IdentityProviderUrl"]="IDENTITY_PROVIDER_URL_PROD"
            ["AppBaseUrl"]="DASHBOARD_BASE_PATH_REALPROD"
        )
        ;;
    staging)
        env_var_mapping=(
            ["LENS_CORE_DASHBOARD_PORT"]="LENS_CORE_DASHBOARD_PORT_STAGING"
            ["IdentityProviderUrl"]="IDENTITY_PROVIDER_URL_STAGING"
            ["AppBaseUrl"]="DASHBOARD_BASE_PATH_PROD"
        )
        ;;
    preview)
        env_var_mapping=(
            ["LENS_CORE_DASHBOARD_PORT"]="LENS_CORE_DASHBOARD_PORT_PREVIEW"
            ["IdentityProviderUrl"]="IDENTITY_PROVIDER_URL_PREVIEW"
            ["AppBaseUrl"]="DASHBOARD_BASE_PATH_STAGING"
        )
        ;;
    *)
        echo "Invalid environment: $ENVIRONMENT. Must be 'production', 'staging', or 'preview'."
        exit 1
        ;;
esac

# Create a docker env file
echo "# Auto-generated docker env file for $ENVIRONMENT" > "$FILENAME"

# Loop through the mapping and write to the docker env file
missing_vars=()
for docker_var in "${!env_var_mapping[@]}"; do
    ci_var="${env_var_mapping[$docker_var]}"
    
    if [ -z "${!ci_var}" ]; then
        missing_vars+=("$ci_var")
    else
        echo "$docker_var=${!ci_var}" >> "$FILENAME"
    fi
done

# Check if any variables were missing and print error if any, sorted alphabetically
if [ ${#missing_vars[@]} -ne 0 ]; then
    echo "Error: The following host environment variables are required but not set:"
    for var in $(echo "${missing_vars[@]}" | tr ' ' '\n' | sort); do
        echo "  - $var"
    done
    exit 1
fi
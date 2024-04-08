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
            ["SCRUMBOARD_PORT"]="SCRUMBOARD_PORT_PROD"
            ["IdentityProviderUrl"]="IDENTITY_PROVIDER_URL_PROD"
            ["UserCodesPermittedToReadCheckIns"]="USER_CODES_PERMITTED_TO_READ_CHECK_INS_REALPROD"
            ["EnableFeedbackForms"]="ENABLE_FEEDBACK_FORMS"
            ["EnableSeedData"]="ENABLE_SEED_DATA"
            ["EnableWebHooks"]="ENABLE_WEBHOOKS"
            ["Database__Host"]="REALPROD_MARIADB_HOST"
            ["Database__Username"]="REALPROD_MARIADB_USER"
            ["Database__Password"]="REALPROD_MARIADB_PASSWORD"
            ["Database__DatabaseName"]="REALPROD_MARIADB_SCRUMBOARD_DATABASE"
            ["Database__Port"]="REALPROD_MARIADB_PORT"
            ["UsageDataDatabase__Host"]="REALPROD_MARIADB_HOST"
            ["UsageDataDatabase__Username"]="REALPROD_MARIADB_USER"
            ["UsageDataDatabase__Password"]="REALPROD_MARIADB_PASSWORD"
            ["UsageDataDatabase__DatabaseName"]="REALPROD_MARIADB_SCRUMBOARD_USAGE_DATA_DATABASE"
            ["UsageDataDatabase__Port"]="REALPROD_MARIADB_PORT"
            ["StudentGuide__Enabled"]="ENABLE_STUDENT_GUIDE"
            ["StudentGuide__ContentPath"]="STUDENT_GUIDE_CONTENT_PATH_REALPROD"
            ["StudentGuide__GitlabZipPath"]="STUDENT_GUIDE_GITLAB_ZIP_PATH"
            ["StudentGuide__GitlabTagPath"]="STUDENT_GUIDE_GITLAB_TAG_PATH"
            ["StudentGuide__GitlabAccessToken"]="STUDENT_GUIDE_GITLAB_ACCESS_TOKEN"
            ["LiveUpdate__IgnoreSslValidation"]="LIVE_UPDATE_IGNORE_SSL_VALIDATION_PROD"
        )
        ;;
    staging)
        env_var_mapping=(
            ["SCRUMBOARD_PORT"]="SCRUMBOARD_PORT_STAGING"
            ["AppBaseUrl"]="APP_BASE_URL_PROD"
            ["IdentityProviderUrl"]="IDENTITY_PROVIDER_URL_STAGING"
            ["UserCodesPermittedToReadCheckIns"]="USER_CODES_PERMITTED_TO_READ_CHECK_INS_PROD"
            ["EnableFeedbackForms"]="ENABLE_FEEDBACK_FORMS"
            ["EnableSeedData"]="ENABLE_SEED_DATA"
            ["EnableWebHooks"]="ENABLE_WEBHOOKS"
            ["Database__Host"]="MARIA_DB_HOST_STAGING"
            ["Database__Username"]="MARIA_DB_USERNAME"
            ["Database__Password"]="MARIA_DB_PASSWORD"
            ["Database__DatabaseName"]="MARIA_DB_SCRUMBOARD_DATABASE_PROD"
            ["Database__Port"]="MARIA_DB_PORT"
            ["UsageDataDatabase__Host"]="MARIA_DB_HOST_STAGING"
            ["UsageDataDatabase__Username"]="MARIA_DB_USERNAME"
            ["UsageDataDatabase__Password"]="MARIA_DB_PASSWORD"
            ["UsageDataDatabase__DatabaseName"]="MARIA_DB_SCRUMBOARD_USAGE_DATA_DATABASE_PROD"
            ["UsageDataDatabase__Port"]="MARIA_DB_PORT"
            ["StudentGuide__Enabled"]="ENABLE_STUDENT_GUIDE"
            ["StudentGuide__ContentPath"]="STUDENT_GUIDE_CONTENT_PATH_PROD"
            ["StudentGuide__GitlabZipPath"]="STUDENT_GUIDE_GITLAB_ZIP_PATH"
            ["StudentGuide__GitlabTagPath"]="STUDENT_GUIDE_GITLAB_TAG_PATH"
            ["StudentGuide__GitlabAccessToken"]="STUDENT_GUIDE_GITLAB_ACCESS_TOKEN"
            ["LiveUpdate__IgnoreSslValidation"]="LIVE_UPDATE_IGNORE_SSL_VALIDATION_STAGING"
        )
        ;;
    preview)
        env_var_mapping=(
            ["SCRUMBOARD_PORT"]="SCRUMBOARD_PORT_PREVIEW"
            ["AppBaseUrl"]="APP_BASE_URL_STAGING"
            ["IdentityProviderUrl"]="IDENTITY_PROVIDER_URL_PREVIEW"
            ["UserCodesPermittedToReadCheckIns"]="USER_CODES_PERMITTED_TO_READ_CHECK_INS_STAGING"
            ["EnableFeedbackForms"]="ENABLE_FEEDBACK_FORMS"
            ["EnableSeedData"]="ENABLE_SEED_DATA_PREVIEW"
            ["EnableWebHooks"]="ENABLE_WEBHOOKS"
            ["Database__Host"]="MARIA_DB_HOST_PREVIEW"
            ["Database__Username"]="MARIA_DB_USERNAME"
            ["Database__Password"]="MARIA_DB_PASSWORD"
            ["Database__DatabaseName"]="MARIA_DB_SCRUMBOARD_DATABASE_STAGING"
            ["Database__Port"]="MARIA_DB_PORT"
            ["UsageDataDatabase__Host"]="MARIA_DB_HOST_PREVIEW"
            ["UsageDataDatabase__Username"]="MARIA_DB_USERNAME"
            ["UsageDataDatabase__Password"]="MARIA_DB_PASSWORD"
            ["UsageDataDatabase__DatabaseName"]="MARIA_DB_SCRUMBOARD_USAGE_DATA_DATABASE_STAGING"
            ["UsageDataDatabase__Port"]="MARIA_DB_PORT"
            ["StudentGuide__Enabled"]="ENABLE_STUDENT_GUIDE"
            ["StudentGuide__ContentPath"]="STUDENT_GUIDE_CONTENT_PATH_STAGING"
            ["StudentGuide__GitlabZipPath"]="STUDENT_GUIDE_GITLAB_ZIP_PATH"
            ["StudentGuide__GitlabTagPath"]="STUDENT_GUIDE_GITLAB_TAG_PATH"
            ["StudentGuide__GitlabAccessToken"]="STUDENT_GUIDE_GITLAB_ACCESS_TOKEN"
            ["LiveUpdate__IgnoreSslValidation"]="LIVE_UPDATE_IGNORE_SSL_VALIDATION_PREVIEW"
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
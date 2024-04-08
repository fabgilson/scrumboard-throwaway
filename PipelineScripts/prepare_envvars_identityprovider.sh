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
            ["IDENTITY_PROVIDER_PORT"]="IDENTITY_PROVIDER_PORT_PROD"
            ["Database__Host"]="REALPROD_MARIADB_HOST"
            ["Database__Username"]="REALPROD_MARIADB_USER"
            ["Database__Password"]="REALPROD_MARIADB_PASSWORD"
            ["Database__DatabaseName"]="REALPROD_MARIADB_IDP_DATABASE"
            ["Database__Port"]="REALPROD_MARIADB_PORT"
            ["Ldap__HostName"]="LDAP_HOST_NAME"
            ["Ldap__HostPort"]="LDAP_HOST_PORT"
            ["Ldap__UseSsl"]="LDAP_USE_SSL"
            ["Ldap__IgnoreCertificateVerification"]="LDAP_IGNORE_CERT_VALIDATION"
            ["Ldap__DomainName"]="LDAP_DOMAIN_NAME"
            ["Ldap__UserQueryBase"]="USER_QUERY_BASE"
            ["Ldap__DefaultAdminUserCodes"]="USER_CODES_PERMITTED_TO_READ_CHECK_INS_REALPROD"
            ["SigningKey"]="REALPROD_SECRET_SIGNING_KEY"
        )
        ;;
    staging)
        env_var_mapping=(
            ["IDENTITY_PROVIDER_PORT"]="IDENTITY_PROVIDER_PORT_STAGING"
            ["Database__Host"]="MARIA_DB_HOST_STAGING"
            ["Database__Username"]="MARIA_DB_USERNAME"
            ["Database__Password"]="MARIA_DB_PASSWORD"
            ["Database__DatabaseName"]="MARIA_DB_IDP_DATABASE_PROD"
            ["Database__Port"]="MARIA_DB_PORT"
            ["Ldap__HostName"]="LDAP_HOST_NAME"
            ["Ldap__HostPort"]="LDAP_HOST_PORT"
            ["Ldap__UseSsl"]="LDAP_USE_SSL"
            ["Ldap__IgnoreCertificateVerification"]="LDAP_IGNORE_CERT_VALIDATION"
            ["Ldap__DomainName"]="LDAP_DOMAIN_NAME"
            ["Ldap__UserQueryBase"]="USER_QUERY_BASE"
            ["Ldap__DefaultAdminUserCodes"]="USER_CODES_PERMITTED_TO_READ_CHECK_INS_PROD"
            ["SigningKey"]="SECRET_SIGNING_KEY"
        )
        ;;
    preview)
        env_var_mapping=(
            ["IDENTITY_PROVIDER_PORT"]="IDENTITY_PROVIDER_PORT_PREVIEW"
            ["Database__Host"]="MARIA_DB_HOST_PREVIEW"
            ["Database__Username"]="MARIA_DB_USERNAME"
            ["Database__Password"]="MARIA_DB_PASSWORD"
            ["Database__DatabaseName"]="MARIA_DB_IDP_DATABASE_STAGING"
            ["Database__Port"]="MARIA_DB_PORT"
            ["Ldap__HostName"]="LDAP_HOST_NAME"
            ["Ldap__HostPort"]="LDAP_HOST_PORT"
            ["Ldap__UseSsl"]="LDAP_USE_SSL"
            ["Ldap__IgnoreCertificateVerification"]="LDAP_IGNORE_CERT_VALIDATION"
            ["Ldap__DomainName"]="LDAP_DOMAIN_NAME"
            ["Ldap__UserQueryBase"]="USER_QUERY_BASE"
            ["Ldap__DefaultAdminUserCodes"]="USER_CODES_PERMITTED_TO_READ_CHECK_INS_STAGING"
            ["SigningKey"]="SECRET_SIGNING_KEY"
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
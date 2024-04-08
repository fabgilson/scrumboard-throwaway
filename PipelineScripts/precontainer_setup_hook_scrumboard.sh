#!/bin/bash

# Define the folder name based on environment variables or defaults
FOLDER_NAME="${HOME}/${APP_NAME:-scrumboard}-${CI_ENVIRONMENT_NAME:-development}"

echo "Ensuring that student guide folder exists and has correct permissions for docker container"

# Create the directory 
if mkdir -p "$FOLDER_NAME"; then
    echo "Directory '$FOLDER_NAME' created or already exists."
else
    echo "Failed to create directory '$FOLDER_NAME'. Exiting."
    exit 1
fi

# Change group to docker 
if chgrp docker "$FOLDER_NAME"; then
    echo "Group changed to 'docker' for '$FOLDER_NAME'."
else
    echo "Failed to change group to 'docker' for '$FOLDER_NAME'. Exiting."
    exit 1
fi

# Change permissions to allow group write access
if chmod g+w "$FOLDER_NAME"; then
    echo "Write permissions set for group on '$FOLDER_NAME'."
else
    echo "Failed to set write permissions for group on '$FOLDER_NAME'. Exiting."
    exit 1
fi

echo "Setup complete."

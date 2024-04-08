#!/bin/bash

init() {
    echo "Attempting to build solution to docker images for easier development..."
  
    # Check if Docker is installed
    if ! command -v docker &> /dev/null
    then
        echo "Error: Docker is not installed. Please install Docker and try again."
        exit 1
    fi
    
    # Function to build Docker images with error handling
    build_docker_image() {
        local directory="$1"
        local tag="$2"
        
        cd "$directory" || { echo "Error: Failed to change to directory $directory"; exit 1; }
        docker build -t "$tag" . --build-arg ScrumBoardBuildStarterTag=latest || { echo "Error: Docker build failed for $tag"; exit 1; }
        cd - || { echo "Error: Failed to change back to the previous directory"; exit 1; }
    }
    
    # Build the Docker images
    docker build -t scrumboard-build-starter:latest -f Dockerfile . || { echo "Error: Docker build failed for scrumboard-build-starter:local"; exit 1; }
    build_docker_image "ScrumBoard" "scrumboard:latest"
    build_docker_image "IdentityProvider" "identityprovider:latest"
    build_docker_image "LensCoreDashboard" "lenscoredashboard:latest"
    
    echo "Docker images have been built and added to local repository."
    echo "If you are only going to be working on the scrumboard, run this script again with argument 'setup-scrumboard-dev' to have all other dependencies started automatically."
}

setup_scrumboard_dev_environment() {
    echo "Setting up scrumboard dev environment..."
    
    MARIADB_BIND_TO_HOST_PORT=23306 docker compose up -d mariadb
    docker compose up -d lenscoredashboard
    SeedSampleUserAccounts=true Ldap__IgnoreCertificateVerification=true docker compose up -d identityprovider
}

tear_down() {
    echo "Tearing down the environment..."
    
    docker compose down
}

# Check if an argument is provided
if [ $# -eq 0 ]; then
    echo "Error: No argument supplied. Please provide 'init', 'setup-scrumboard-dev', or 'tear-down' as an argument."
    exit 1
fi

# Execute the corresponding function based on the argument
case $1 in
    init) 
        init
        ;;
    setup-scrumboard-dev) 
        setup_scrumboard_dev_environment
        ;;
    tear-down)
        tear_down
        ;;
    *)
        echo "Error: Invalid argument. Please use 'init', 'setup-scrumboard-dev', or 'tear-down'."
        exit 1
        ;;
esac

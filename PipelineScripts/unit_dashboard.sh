#!/bin/bash

# Check if UID and GID are provided
if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <FINAL_UID> <FINAL_GID>"
    exit 1
fi

FINAL_UID=$1
FINAL_GID=$2

# Run the tests and generate the coverage report
dotnet test LensCoreDashboard.Tests \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=opencover \
  /p:CoverletOutput=../CoverageReports/dashboard.opencover.xml \
  /p:ExcludeByFile=\"**/IdentityProvider/Migrations/*.cs,**/ScrumBoard/Migrations/*.cs\" \
  -- xunit.parallelizeAssembly=true \
  --no-build

# Check if the test command was successful
if [ $? -eq 0 ]; then
    echo "Tests ran successfully."
else
    echo "Tests failed. Exiting."
    exit 1
fi

# Change the ownership of the coverage file
chown "$FINAL_UID":"$FINAL_GID" CoverageReports/dashboard.opencover.xml

# Check if the chown command was successful
if [ $? -eq 0 ]; then
    echo "Ownership of the coverage file has been changed successfully."
else
    echo "Failed to change the ownership of the coverage file."
    exit 1
fi

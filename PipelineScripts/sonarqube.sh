export PATH="$PATH:/root/.dotnet/tools"

dotnet clean

if [[ ! -v CI_MERGE_REQUEST_IID ]] || [[ -z "${CI_MERGE_REQUEST_IID}" ]]; then
    dotnet sonarscanner begin \
      /k:"se-projects_lens-group_lens-resurrected_AYNZE-eWp515Jz-qIDjz" \
      /d:sonar.host.url="${SONAR_HOST_URL}" \
      /d:sonar.login="${SONAR_TOKEN}" \
      /d:sonar.cs.opencover.reportsPaths="CoverageReports/**" \
      /v:"2.0.0-release" \
      /d:sonar.exclusions="**/Migrations/**/*" \
      /d:sonar.branch.name="${CI_COMMIT_REF_NAME}"
else
    dotnet sonarscanner begin \
      /k:"se-projects_lens-group_lens-resurrected_AYNZE-eWp515Jz-qIDjz" \
      /d:sonar.host.url="${SONAR_HOST_URL}" \
      /d:sonar.login="${SONAR_TOKEN}" \
      /d:sonar.cs.opencover.reportsPaths="CoverageReports/**" \
      /v:"2.0.0-release" \
      /d:sonar.exclusions="**/Migrations/**/*" \
      /d:sonar.pullrequest.key="${CI_MERGE_REQUEST_IID}" \
      /d:sonar.pullrequest.branch="${CI_MERGE_REQUEST_SOURCE_BRANCH_NAME}" \
      /d:sonar.pullrequest.base="${CI_MERGE_REQUEST_TARGET_BRANCH_NAME}"
fi

dotnet build lens-resurrected.sln
dotnet sonarscanner end /d:sonar.login="${SONAR_TOKEN}"

# Change the ownership of the coverage files
chown -R "$FINAL_UID":"$FINAL_GID" CoverageReports/

# Check if the chown command was successful
if [ $? -eq 0 ]; then
    echo "Ownership of the coverage file has been changed successfully."
else
    echo "Failed to change the ownership of the coverage file."
    exit 1
fi
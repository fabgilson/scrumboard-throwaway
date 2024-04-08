export E2E_TEST_BROWSER=chrome
dotnet test ScrumBoard.SpecFlow \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=opencover \
  /p:CoverletOutput=../CoverageReports/scrum-board-end-to-end-chrome.opencover.xml \
  /p:ExcludeByFile=\"**/IdentityProvider/Migrations/*.cs,**/ScrumBoard/Migrations/*.cs\" \
  --no-build
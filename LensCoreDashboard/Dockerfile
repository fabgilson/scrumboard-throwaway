ARG ScrumBoardBuildStarterTag
FROM scrumboard-build-starter:${ScrumBoardBuildStarterTag} as starter
RUN dotnet publish LensCoreDashboard -c Release -o LensCoreDashboard/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN groupadd -r nonroot && useradd -r -g nonroot nonroot 

WORKDIR /LensCoreDashboard
COPY --from=starter --chown=nonroot:nonroot /app/LensCoreDashboard/out .
USER nonroot
ENTRYPOINT ["dotnet", "LensCoreDashboard.dll"]
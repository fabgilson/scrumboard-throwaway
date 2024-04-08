ARG ScrumBoardBuildStarterTag
FROM scrumboard-build-starter:${ScrumBoardBuildStarterTag} as starter
RUN dotnet publish ScrumBoard -c Release -o ScrumBoard/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN groupadd -r nonroot && useradd -r -g nonroot nonroot 

WORKDIR /ScrumBoard
COPY --from=starter --chown=nonroot:nonroot /app/ScrumBoard/out .
USER nonroot
ENTRYPOINT ["dotnet", "ScrumBoard.dll"]
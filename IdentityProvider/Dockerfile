ARG ScrumBoardBuildStarterTag
FROM scrumboard-build-starter:${ScrumBoardBuildStarterTag} as starter
RUN dotnet publish IdentityProvider -c Release -o IdentityProvider/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN groupadd -r nonroot && useradd -r -g nonroot nonroot 

WORKDIR /IdentityProvider
COPY --from=starter --chown=nonroot:nonroot /app/IdentityProvider/out .
USER nonroot
ENTRYPOINT ["dotnet", "IdentityProvider.dll"]
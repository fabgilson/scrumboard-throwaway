FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app/

ADD PipelineScripts/* ./
COPY . ./

RUN dotnet build

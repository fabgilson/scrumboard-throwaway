ARG ScrumBoardBuildStarterTag
FROM scrumboard-build-starter:${ScrumBoardBuildStarterTag} as build

# Copy node files to final image
FROM node:18 as nodejs
FROM build as final
COPY --from=nodejs /usr/local /usr/local

# Set necessary ENVVARS for Java and install JDK
ENV LANG en_US.UTF-8
ENV JAVA_HOME /usr/lib/jvm/msopenjdk-17-amd64
ENV PATH "${JAVA_HOME}/bin:${PATH}"
COPY --from=mcr.microsoft.com/openjdk/jdk:17-ubuntu $JAVA_HOME $JAVA_HOME

# Install SonarScanner
RUN dotnet tool install --global dotnet-sonarscanner

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/Japan.Core/Japan.Core.csproj src/Japan.Core/
COPY src/Japan.Web/Japan.Web.csproj src/Japan.Web/
RUN dotnet restore src/Japan.Web/Japan.Web.csproj
COPY src/Japan.Core/ src/Japan.Core/
COPY src/Japan.Web/ src/Japan.Web/
RUN dotnet publish src/Japan.Web/Japan.Web.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
COPY data/grammar.json ./data/grammar.json
COPY data/connectors.json ./data/connectors.json
ENV ASPNETCORE_ENVIRONMENT=Production
ENV GrammarData__Path=data/grammar.json
ENV ConnectorData__Path=data/connectors.json
EXPOSE 8080
USER root
ENTRYPOINT ["sh", "-c", "export ASPNETCORE_HTTP_PORTS=\"${PORT:-8080}\"; exec dotnet Japan.Web.dll"]

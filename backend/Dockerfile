FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy manifest files first so the restore layer is cached unless deps change
COPY LegalDocumentAISearch.sln Directory.Build.props Directory.Packages.props nuget.config ./
COPY src/LegalDocumentAISearch.Domain/LegalDocumentAISearch.Domain.csproj                 src/LegalDocumentAISearch.Domain/
COPY src/LegalDocumentAISearch.Application/LegalDocumentAISearch.Application.csproj       src/LegalDocumentAISearch.Application/
COPY src/LegalDocumentAISearch.Infrastructure/LegalDocumentAISearch.Infrastructure.csproj src/LegalDocumentAISearch.Infrastructure/
COPY src/LegalDocumentAISearch.Api/LegalDocumentAISearch.Api.csproj                       src/LegalDocumentAISearch.Api/
COPY tests/LegalDocumentAISearch.UnitTests/LegalDocumentAISearch.UnitTests.csproj         tests/LegalDocumentAISearch.UnitTests/

RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore

COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish src/LegalDocumentAISearch.Api/LegalDocumentAISearch.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "LegalDocumentAISearch.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NuGet.config", "./"]
COPY ["backend/src/Api.Host/CohereLoanAndMortgage.Api.Host.csproj", "backend/src/Api.Host/"]
COPY ["backend/src/LoanWorkflow.Governance/LoanWorkflow.Governance.csproj", "backend/src/LoanWorkflow.Governance/"]
COPY ["backend/src/LoanWorkflow.Mcp/LoanWorkflow.Mcp.csproj", "backend/src/LoanWorkflow.Mcp/"]
RUN dotnet restore "backend/src/Api.Host/CohereLoanAndMortgage.Api.Host.csproj" --configfile "/src/NuGet.config"
COPY backend/src/Api.Host/ backend/src/Api.Host/
COPY backend/src/LoanWorkflow.Governance/ backend/src/LoanWorkflow.Governance/
COPY backend/src/LoanWorkflow.Mcp/ backend/src/LoanWorkflow.Mcp/
COPY dataset-seed/ dataset-seed/
WORKDIR /src/backend/src/Api.Host
RUN dotnet publish "CohereLoanAndMortgage.Api.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false -m:1 --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/dataset-seed /app/dataset-seed
ENV Dataset__RootPath=/app/dataset-seed
ENV Dataset__PolicyFilePath=/app/dataset-seed/policies/general_policy.txt
ENTRYPOINT ["dotnet", "CohereLoanAndMortgage.Api.Host.dll"]

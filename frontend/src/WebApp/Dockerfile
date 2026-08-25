FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NuGet.config", "./"]
COPY ["frontend/src/WebApp/Cohere.LoanProcessing.WebApp.csproj", "frontend/src/WebApp/"]
RUN dotnet restore frontend/src/WebApp/Cohere.LoanProcessing.WebApp.csproj --configfile "/src/NuGet.config"
COPY . .
RUN dotnet publish frontend/src/WebApp/Cohere.LoanProcessing.WebApp.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY dataset-seed /app/dataset-seed
ENV ASPNETCORE_URLS=http://+:8080
ENV DatasetSeed__RootPath=/app/dataset-seed
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cohere.LoanProcessing.WebApp.dll"]

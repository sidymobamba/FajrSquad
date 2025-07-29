# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia solution e progetti
COPY *.sln .
COPY FajrSquad.API/*.csproj ./FajrSquad.API/
COPY FajrSquad.Core/*.csproj ./FajrSquad.Core/
COPY FajrSquad.Infrastructure/*.csproj ./FajrSquad.Infrastructure/
COPY FajrSquad.Tests/*.csproj ./FajrSquad.Tests/

# Ripristino dei pacchetti NuGet
RUN dotnet restore

# Copia tutto il resto
COPY . .

# Compila e pubblica in modalità Release
RUN dotnet publish FajrSquad.API/FajrSquad.API.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway espone la porta 8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Entry point dell'app
ENTRYPOINT ["dotnet", "FajrSquad.API.dll"]

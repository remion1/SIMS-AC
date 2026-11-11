# ══════════════════════════════════════════════════════════════════════════════
# Stage 1 – Build
# ══════════════════════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file first for layer-cached restore
COPY ["Sims.Backend.csproj", "./"]
RUN dotnet restore "Sims.Backend.csproj"

# Copy the rest of the source and publish a Release build
COPY . .
RUN dotnet publish "Sims.Backend.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ══════════════════════════════════════════════════════════════════════════════
# Stage 2 – Runtime
# ══════════════════════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# .NET 10  default port for containers
EXPOSE 8080

# Copy only the published output from the build stage
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Sims.Backend.dll"]

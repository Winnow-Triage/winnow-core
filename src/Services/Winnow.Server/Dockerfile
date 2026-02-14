# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers (better caching)
COPY ["Winnow.Server/Winnow.Server.csproj", "Winnow.Server/"]
RUN dotnet restore "Winnow.Server/Winnow.Server.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Winnow.Server"
RUN dotnet build -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Winnow.Server.dll"]
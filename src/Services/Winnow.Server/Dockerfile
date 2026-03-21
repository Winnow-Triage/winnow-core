# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Services/Winnow.Server/Winnow.Server.csproj", "Winnow.Server/"]
RUN dotnet restore "Winnow.Server/Winnow.Server.csproj"

COPY src/Services/Winnow.Server/ Winnow.Server/
WORKDIR "/src/Winnow.Server"
RUN dotnet build -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Winnow.Server.dll"]
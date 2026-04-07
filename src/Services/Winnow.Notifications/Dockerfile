# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore
COPY ["src/Services/Winnow.Notifications/Winnow.Notifications.csproj", "src/Services/Winnow.Notifications/"]
COPY ["src/Services/Winnow.Contracts/Winnow.Contracts.csproj", "src/Services/Winnow.Contracts/"]
COPY ["src/Services/Winnow.API/Winnow.API.csproj", "src/Services/Winnow.API/"]
COPY ["src/Services/Winnow.Integrations/Winnow.Integrations.csproj", "src/Services/Winnow.Integrations/"]
COPY ["src/Sdks/Winnow.Sdk.DotNet/Winnow.Sdk.csproj", "src/Sdks/Winnow.Sdk.DotNet/"]

RUN dotnet restore "src/Services/Winnow.Notifications/Winnow.Notifications.csproj"

# Copy full source and publish
COPY . .
RUN dotnet publish "src/Services/Winnow.Notifications/Winnow.Notifications.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
# We use 'aspnet' instead of 'runtime' because this service references Winnow.API, 
# which requires the ASP.NET Core shared framework.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Winnow.Notifications.dll"]

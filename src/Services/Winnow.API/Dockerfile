FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_EnableDiagnostics=0

# Copy everything from the root context so the project references (Contracts, Integrations, Sdk) are found
COPY . .
RUN dotnet restore src/Services/Winnow.API/Winnow.API.csproj
RUN dotnet publish src/Services/Winnow.API/Winnow.API.csproj -c Release -o /app/publish

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install native dependencies for ONNX
RUN apt-get update && apt-get install -y libgomp1 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Winnow.API.dll"]

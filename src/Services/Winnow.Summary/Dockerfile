FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_EnableDiagnostics=0
COPY . .
RUN dotnet restore src/Services/Winnow.Summary/Winnow.Summary.csproj
RUN dotnet publish src/Services/Winnow.Summary/Winnow.Summary.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install native dependencies for ONNX
RUN apt-get update && apt-get install -y libgomp1 && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Winnow.Summary.dll"]

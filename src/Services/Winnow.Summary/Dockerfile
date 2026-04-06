FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_EnableDiagnostics=0
COPY . .
RUN dotnet restore src/Services/Winnow.Summary/Winnow.Summary.csproj

# Added -p:UseAppHost=false. This tells .NET not to generate a redundant 
# native executable file, saving about 10-15MB per image.
RUN dotnet publish src/Services/Winnow.Summary/Winnow.Summary.csproj \
    -c Release \
    -o /app/publish \
    -p:UseAppHost=false 

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgomp1 curl \
    && rm -rf /var/lib/apt/lists/* \
    && apt-get clean

COPY --from=build /app/publish .
EXPOSE 8080

ENTRYPOINT ["dotnet", "Winnow.Summary.dll"]
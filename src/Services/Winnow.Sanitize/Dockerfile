FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV DOTNET_EnableDiagnostics=0
COPY . .
RUN dotnet restore src/Services/Winnow.Sanitize/Winnow.Sanitize.csproj
RUN dotnet publish src/Services/Winnow.Sanitize/Winnow.Sanitize.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Winnow.Sanitize.dll"]

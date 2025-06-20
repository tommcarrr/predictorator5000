# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Predictorator/Predictorator.csproj Predictorator/
RUN dotnet restore Predictorator/Predictorator.csproj
COPY . .
WORKDIR /src/Predictorator
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Predictorator.dll"]

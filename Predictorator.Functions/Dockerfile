FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet9 AS base
WORKDIR /home/site/wwwroot

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Predictorator.Core/*.csproj Predictorator.Core/
COPY Predictorator.Functions/*.csproj Predictorator.Functions/
RUN dotnet restore Predictorator.Functions/Predictorator.Functions.csproj
COPY . .
RUN dotnet publish Predictorator.Functions/Predictorator.Functions.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=build /app/publish .

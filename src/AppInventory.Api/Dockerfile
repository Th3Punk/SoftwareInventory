FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AppInventory.Api/AppInventory.Api.csproj", "AppInventory.Api/"]
COPY ["AppInventory.Core/AppInventory.Core.csproj", "AppInventory.Core/"]
COPY ["AppInventory.Infrastructure/AppInventory.Infrastructure.csproj", "AppInventory.Infrastructure/"]
RUN dotnet restore "AppInventory.Api/AppInventory.Api.csproj"
COPY . .
RUN dotnet build "AppInventory.Api/AppInventory.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AppInventory.Api/AppInventory.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AppInventory.Api.dll"]

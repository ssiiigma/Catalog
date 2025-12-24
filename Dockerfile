FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Catalog.API/Catalog.API.csproj", "Catalog.API/"]
COPY ["src/Catalog.Infrastructure/Catalog.Infrastructure.csproj", "Catalog.Infrastructure/"]
COPY ["src/Catalog.Application/Catalog.Application.csproj", "Catalog.Application/"]
COPY ["src/Catalog.Core/Catalog.Core.csproj", "Catalog.Core/"]
RUN dotnet restore "Catalog.API/Catalog.API.csproj"
COPY . .
WORKDIR "/src/Catalog.API"
RUN dotnet build "Catalog.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Catalog.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Catalog.API.dll"]
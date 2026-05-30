FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["nuget.config", "nuget.config"]
COPY ["AquaHome/AquaHome.API/AquaHome.API.csproj",         "AquaHome/AquaHome.API/"]
COPY ["AquaHome/AquaHome.Domain/AquaHome.Domain.csproj",   "AquaHome/AquaHome.Domain/"]
COPY ["AquaHome/AquaHome.EFCore/AquaHome.EFCore.csproj",   "AquaHome/AquaHome.EFCore/"]
COPY ["Share/FishLover.Shared/FishLover.Shared.csproj",     "Share/FishLover.Shared/"]
RUN dotnet restore "AquaHome/AquaHome.API/AquaHome.API.csproj"

COPY . .
WORKDIR "/src/AquaHome/AquaHome.API"
RUN dotnet publish "AquaHome.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AquaHome.API.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["nuget.config", "nuget.config"]
COPY ["AquaHome/AquaHome.Worker/AquaHome.Worker.csproj", "AquaHome/AquaHome.Worker/"]
COPY ["AquaHome/AquaHome.Domain/AquaHome.Domain.csproj", "AquaHome/AquaHome.Domain/"]
COPY ["AquaHome/AquaHome.EFCore/AquaHome.EFCore.csproj", "AquaHome/AquaHome.EFCore/"]
COPY ["AquaHome/AquaHome.Common/AquaHome.Common.csproj", "AquaHome/AquaHome.Common/"]
COPY ["Share/FishLover.Shared/FishLover.Shared.csproj", "Share/FishLover.Shared/"]
RUN dotnet restore "AquaHome/AquaHome.Worker/AquaHome.Worker.csproj"

COPY . .
WORKDIR "/src/AquaHome/AquaHome.Worker"
RUN dotnet publish "AquaHome.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AquaHome.Worker.dll"]

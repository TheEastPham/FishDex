FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["nuget.config", "nuget.config"]
COPY ["UserManagement/UserManagement.API/UserManagement.API.csproj",       "UserManagement/UserManagement.API/"]
COPY ["UserManagement/UserManagement.Domain/UserManagement.Domain.csproj", "UserManagement/UserManagement.Domain/"]
COPY ["UserManagement/UserManagement.EFCore/UserManagement.EFCore.csproj", "UserManagement/UserManagement.EFCore/"]
COPY ["Share/FishLover.Shared/FishLover.Shared.csproj",                    "Share/FishLover.Shared/"]
RUN dotnet restore "UserManagement/UserManagement.API/UserManagement.API.csproj"

COPY . .
WORKDIR "/src/UserManagement/UserManagement.API"
RUN dotnet publish "UserManagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserManagement.API.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
USER app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/IdentityApi.Domain/IdentityApi.Domain.csproj", "src/IdentityApi.Domain/"]
COPY ["src/IdentityApi.Application/IdentityApi.Application.csproj", "src/IdentityApi.Application/"]
COPY ["src/IdentityApi.Infrastructure/IdentityApi.Infrastructure.csproj", "src/IdentityApi.Infrastructure/"]
COPY ["src/IdentityApi.WebApi/IdentityApi.WebApi.csproj", "src/IdentityApi.WebApi/"]

RUN dotnet restore "src/IdentityApi.WebApi/IdentityApi.WebApi.csproj"

COPY . .

RUN dotnet build "src/IdentityApi.WebApi/IdentityApi.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/IdentityApi.WebApi/IdentityApi.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityApi.WebApi.dll"]

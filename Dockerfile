
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/DeveloperStore.WebApi/DeveloperStore.WebApi.csproj", "src/DeveloperStore.WebApi/"]
COPY ["src/DeveloperStore.Application/DeveloperStore.Application.csproj", "src/DeveloperStore.Application/"]
COPY ["src/DeveloperStore.Common/DeveloperStore.Common.csproj", "src/DeveloperStore.Common/"]
COPY ["src/DeveloperStore.Domain/DeveloperStore.Domain.csproj", "src/DeveloperStore.Domain/"]
COPY ["src/DeveloperStore.IoC/DeveloperStore.IoC.csproj", "src/DeveloperStore.IoC/"]
COPY ["src/DeveloperStore.ORM/DeveloperStore.ORM.csproj", "src/DeveloperStore.ORM/"]
RUN dotnet restore "./src/DeveloperStore.WebApi/DeveloperStore.WebApi.csproj"
COPY . .
WORKDIR "/src/src/DeveloperStore.WebApi"
RUN dotnet build "./DeveloperStore.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DeveloperStore.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeveloperStore.WebApi.dll"]

# Multi-stage build: SDK for compilation, runtime image for final container
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies (layer-cached when .csproj is unchanged)
COPY ["BotNetApi/BotNetApi.csproj", "BotNetApi/"]
RUN dotnet restore "BotNetApi/BotNetApi.csproj"

# Copy source and build
COPY . .
WORKDIR "/src/BotNetApi"
RUN dotnet build "BotNetApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BotNetApi.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BotNetApi.dll"]

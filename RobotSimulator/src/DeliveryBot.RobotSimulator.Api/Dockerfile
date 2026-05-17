FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/DeliveryBot.RobotSimulator.Api/ ./src/DeliveryBot.RobotSimulator.Api/
COPY src/DeliveryBot.RobotSimulator.Core/ ./src/DeliveryBot.RobotSimulator.Core/
COPY src/DeliveryBot.RobotSimulator.Events/ ./src/DeliveryBot.RobotSimulator.Events/
COPY src/DeliveryBot.RobotSimulator.Infrastructure/ ./src/DeliveryBot.RobotSimulator.Infrastructure/

RUN dotnet restore ./src/DeliveryBot.RobotSimulator.Api/DeliveryBot.RobotSimulator.Api.csproj

RUN dotnet publish ./src/DeliveryBot.RobotSimulator.Api/DeliveryBot.RobotSimulator.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DeliveryBot.RobotSimulator.Api.dll"]
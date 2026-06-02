# ReadBotsFunction

.NET 8 isolated Azure Functions project for the Readable Bot Network Representation feature.

## Functions

- `ReadRobotOutputEvents` consumes simulator `robot-output` events from Event Hub.
- Valid robot events are projected into one current bot document in the Cosmos DB `bots` container.
- Rejected or failed events are logged and written to the Cosmos DB `function-diagnostics` container when possible.

## Cosmos DB

The Function App expects these app settings:

- `ReadableBotNetwork__CosmosAccountEndpoint`
- `ReadableBotNetwork__CosmosDatabaseName`
- `ReadableBotNetwork__BotsContainerName`
- `ReadableBotNetwork__DiagnosticsContainerName`

The `bots` container uses `/botId` as its partition key. Each bot document also uses the bot ID as its `id`.

## Event Hub

The Event Hub trigger expects these app settings:

- `RobotOutputEventHubName`
- `RobotOutputEventHubConsumerGroup`
- `RobotOutputEventHubIdentity__fullyQualifiedNamespace`
- `RobotOutputEventHubIdentity__credential`

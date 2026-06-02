using ReadableBotState.ReadModel;

namespace ReadableBotState.Projection;

public sealed record ProjectionResult(
    bool ShouldPersist,
    BotReadModel? Document,
    string Message);

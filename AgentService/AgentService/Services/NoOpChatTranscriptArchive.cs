namespace AgentService.Services;

public sealed class NoOpChatTranscriptArchive : IChatTranscriptArchive
{
    public Task ArchiveAsync(AgentChatTranscriptRecord transcript, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

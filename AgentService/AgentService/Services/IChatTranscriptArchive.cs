namespace AgentService.Services;

public interface IChatTranscriptArchive
{
    Task ArchiveAsync(AgentChatTranscriptRecord transcript, CancellationToken cancellationToken = default);
}

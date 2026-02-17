namespace YMS.Server;

public interface IOpenLabMailer
{
    Task SendReservationEventAsync(OpenLabMailMessage message, CancellationToken cancellationToken = default);
}

public record OpenLabMailMessage(
    string IssueNo,
    string Action,
    string Subject,
    IReadOnlyList<string> ToAddresses,
    string Body);

public class Mailer(ILogger<Mailer> logger) : IOpenLabMailer
{
    public Task SendReservationEventAsync(OpenLabMailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[OpenLab Mail] Action={Action}, IssueNo={IssueNo}, To={To}, Subject={Subject}",
            message.Action,
            message.IssueNo,
            string.Join(';', message.ToAddresses),
            message.Subject);

        logger.LogInformation("[OpenLab Mail] Body: {Body}", message.Body);

        return Task.CompletedTask;
    }
}

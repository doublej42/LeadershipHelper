namespace LeadershipHelper.Application.Auth;

public interface IEmailSender
{
    Task SendOtpAsync(string toAddress, string code, CancellationToken cancellationToken = default);

    /// <summary>Notifies a situation owner that a contributor has added an action pending their review.</summary>
    Task SendActionPendingApprovalAsync(string toAddress, string ownerName, string situationTitle, string editUrl, CancellationToken cancellationToken = default);
}

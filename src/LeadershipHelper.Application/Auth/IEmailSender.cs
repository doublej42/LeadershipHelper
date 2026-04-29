namespace LeadershipHelper.Application.Auth;

public interface IEmailSender
{
    Task SendOtpAsync(string toAddress, string code, CancellationToken cancellationToken = default);
}

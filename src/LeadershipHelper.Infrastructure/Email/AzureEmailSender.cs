using Azure;
using Azure.Communication.Email;
using LeadershipHelper.Application.Auth;
using Microsoft.Extensions.Options;

namespace LeadershipHelper.Infrastructure.Email;

public sealed class AzureEmailSender : IEmailSender
{
    private readonly EmailClient _client;
    private readonly string _fromAddress;

    public AzureEmailSender(IOptions<AzureEmailOptions> options)
    {
        var opts = options.Value;
        _client = new EmailClient(opts.ConnectionString);
        _fromAddress = opts.EmailFrom;
    }

    public async Task SendOtpAsync(string toAddress, string code, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage(
            senderAddress: _fromAddress,
            recipients: new EmailRecipients([new EmailAddress(toAddress)]),
            content: new EmailContent("Your login code")
            {
                PlainText = $"Your one-time login code is: {code}\n\nThis code expires in 60 minutes.",
                Html = $"<p>Your one-time login code is: <strong>{code}</strong></p><p>This code expires in 60 minutes.</p>",
            });

        await _client.SendAsync(WaitUntil.Started, message, cancellationToken);
    }
}

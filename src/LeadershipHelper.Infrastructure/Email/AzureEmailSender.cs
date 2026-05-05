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

    public async Task SendActionPendingApprovalAsync(string toAddress, string ownerName, string situationTitle, string editUrl, CancellationToken cancellationToken = default)
    {
        var subject = $"New action pending approval — {situationTitle}";
        var message = new EmailMessage(
            senderAddress: _fromAddress,
            recipients: new EmailRecipients([new EmailAddress(toAddress)]),
            content: new EmailContent(subject)
            {
                PlainText = $"Hi {ownerName},\n\nA contributor has added a new action to your situation \"{situationTitle}\" that is waiting for your review.\n\nReview it here: {editUrl}",
                Html = $"<p>Hi {System.Net.WebUtility.HtmlEncode(ownerName)},</p>" +
                       $"<p>A contributor has added a new action to your situation <strong>{System.Net.WebUtility.HtmlEncode(situationTitle)}</strong> that is waiting for your review.</p>" +
                       $"<p><a href=\"{editUrl}\">Review pending actions</a></p>",
            });

        await _client.SendAsync(WaitUntil.Started, message, cancellationToken);
    }
}

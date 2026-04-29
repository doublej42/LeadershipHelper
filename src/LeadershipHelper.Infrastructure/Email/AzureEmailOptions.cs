namespace LeadershipHelper.Infrastructure.Email;

public sealed class AzureEmailOptions
{
    public const string SectionName = "Acs";

    public string ConnectionString { get; set; } = string.Empty;
    public string EmailFrom { get; set; } = string.Empty;
}

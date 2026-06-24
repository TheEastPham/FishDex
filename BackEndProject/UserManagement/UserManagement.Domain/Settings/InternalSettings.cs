namespace UserManagement.Domain.Settings;

public class InternalSettings
{
    public const string SectionName = "InternalSettings";
    public string ApiKey { get; set; } = string.Empty;
}

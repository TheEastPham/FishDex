namespace FishDex.Domain.Settings;

public class FishDexSettings
{
    public const string SectionName = "FishDexSettings";
    public int LanguageTopCount { get; init; } = 10;
}

using System.Collections.Frozen;
using VModer.Core.Models;

namespace VModer.Core.Services;

public sealed class SettingsService
{
    public string GameRootFolderPath { get; set; } = string.Empty;
    public string ModRootFolderPath { get; set; } = string.Empty;
    public GameLanguage GameLanguage { get; set; } = GameLanguage.Default;
    public FrozenSet<string> AnalysisBlackList { get; set; } = [];
    public long ParseFileMaxBytesSize { get; set; }
    public string ExtensionPath { get; set; } = string.Empty;
    public FrozenSet<string> ErrorCodeBlackList { get; set; } = [];
}
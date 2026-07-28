using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Server;

namespace VModer.Core.Services;

public sealed class EditorDiagnosisService(LanguageServer server, SettingsService settings)
{
    private readonly SettingsService _settings = settings;

    public Task AddDiagnoseAsync(PublishDiagnosticsParams diagnoseParams)
    {
        if (_settings.ErrorCodeBlackList.Count > 0)
        {
            diagnoseParams.Diagnostics.RemoveAll(d =>
            {
                if (d.Code?.StringValue is { } s && _settings.ErrorCodeBlackList.Contains(s))
                {
                    return true;
                }

                return false;
            });
        }
        return server.Client.PublishDiagnostics(diagnoseParams);
    }
}

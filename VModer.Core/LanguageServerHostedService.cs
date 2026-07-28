using System.Collections.Frozen;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using EmmyLua.LanguageServer.Framework.Protocol.JsonRpc;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.PublishDiagnostics;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.ShowMessage;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Initialize;
using EmmyLua.LanguageServer.Framework.Server;
using EnumsNET;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using VModer.Core.Dto;
using VModer.Core.Handlers;
using VModer.Core.Models;
using VModer.Core.Services;
using VModer.Core.Services.GameResource;
using VModer.Languages;

namespace VModer.Core;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(List<TraitDto>))]
internal partial class TraitContext : JsonSerializerContext;

public sealed class LanguageServerHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly SettingsService _settings;
    private readonly LanguageServer _server;
    private readonly ServerLoggerService _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Process _currentProcess = Process.GetCurrentProcess();
    private readonly EditorDiagnosisService _diagnosisService;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public LanguageServerHostedService(
        IHostApplicationLifetime lifetime,
        SettingsService settings,
        LanguageServer server,
        ServerLoggerService logger,
        IServiceProvider serviceProvider,
        EditorDiagnosisService diagnosisService
    )
    {
        _lifetime = lifetime;
        _settings = settings;
        _server = server;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _diagnosisService = diagnosisService;

        _server.AddRequestHandler("getRuntimeInfo", GetRuntimeInfoAsync);
        _server.AddNotificationHandler("clearImageCache", ClearLocalImageCacheAsync);
        _server.AddRequestHandler("getGeneralTraits", GetGeneralTraitsAsync);
        _server.AddRequestHandler("getLeaderTraits", GetLeaderTraitsAsync);
        _server.AddRequestHandler("getAllModifier", GetAllModifierAsync);
    }

    private Task<JsonDocument?> GetAllModifierAsync(RequestMessage message, CancellationToken token)
    {
        return Task.Run(
            () =>
            {
                try
                {
                    var modifiersService = _serviceProvider.GetRequiredService<ModifiersMessageService>();
                    return modifiersService.GetModifierJson();
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to get all modifiers");
                    _logger.Log("Failed to get all modifiers");
                    return null;
                }
            },
            token
        );
    }

    private Task<JsonDocument?> GetGeneralTraitsAsync(RequestMessage message, CancellationToken token)
    {
        return Task.Run<JsonDocument?>(
            () =>
            {
                var traits = _serviceProvider.GetRequiredService<GeneralTraitsService>().GetAllTraitDto();
                var value = JsonSerializer.SerializeToDocument(traits, TraitContext.Default.ListTraitDto);
                return value;
            },
            token
        );
    }

    private Task<JsonDocument?> GetLeaderTraitsAsync(RequestMessage message, CancellationToken token)
    {
        return Task.Run<JsonDocument?>(
            () =>
            {
                var traits = _serviceProvider.GetRequiredService<LeaderTraitsService>().GetAllTraitDto();
                var value = JsonSerializer.SerializeToDocument(traits, TraitContext.Default.ListTraitDto);
                return value;
            },
            token
        );
    }

    private Task<JsonDocument?> GetRuntimeInfoAsync(
        RequestMessage request,
        CancellationToken cancellationToken
    )
    {
        return Task.Run<JsonDocument?>(
            () =>
            {
                _currentProcess.Refresh();

                long memoryUsedBytes = _currentProcess.PrivateMemorySize64;
                var document = JsonDocument.Parse($"{{\"memoryUsedBytes\": {memoryUsedBytes}}}");
                return document;
            },
            cancellationToken
        );
    }

    private Task ClearLocalImageCacheAsync(
        NotificationMessage notificationMessage,
        CancellationToken cancellationToken
    )
    {
        return Task.Run(
            () =>
            {
                Log.Info("开始清理本地图片缓存.");
                try
                {
                    _serviceProvider.GetRequiredService<IImageService>().ClearCache();
                    _server.Client.ShowMessage(
                        new ShowMessageParams
                        {
                            Type = MessageType.Info,
                            Message = Resources.CleanupSuccessful
                        }
                    );
                }
                catch (Exception e)
                {
                    const string message = "清理本地图片缓存失败.";

                    _server.Client.ShowMessage(
                        new ShowMessageParams { Type = MessageType.Error, Message = message }
                    );
                    Log.Error(e, message);
                    _logger.Log(message);
                }
            },
            cancellationToken
        );
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        List<IHandler> handlers =
        [
            new TextDocumentHandler(),
            new HoverHandler(),
            new DocumentColorHandler(),
            new CodeActionHandler()
        ];
        foreach (var handler in handlers)
        {
            _server.AddHandler(handler);
        }
        _server.OnInitialize(
            (c, s) =>
            {
                Log.Info("Language server initializing...");

                InitialSettings(c);
                s.Name = "VModer";
                s.Version = "1.0.0";

                foreach (var handler in handlers)
                {
                    handler.Initialize();
                }

                Log.Info("Game root path: {Path}", _settings.GameRootFolderPath);
                Log.Info("Workspace root path: {Path}", _settings.ModRootFolderPath);
                return Task.CompletedTask;
            }
        );

        _server.OnInitialized(_ =>
        {
            var analyzersService = App.Services.GetRequiredService<AnalyzeService>();

            _server.SendNotification(new NotificationMessage("analyzeAllFilesStart", null));

            long start = Stopwatch.GetTimestamp();
            Task.WhenAll(analyzersService.AnalyzeAllFilesAsync(cancellationToken))
                .ContinueWith(
                    tasks =>
                    {
                        if (!tasks.IsFaulted)
                        {
                            foreach (var result in tasks.Result)
                            {
                                _diagnosisService.AddDiagnoseAsync(
                                    new PublishDiagnosticsParams
                                    {
                                        Diagnostics = result.Diagnostics,
                                        Uri = result.FilePath
                                    }
                                );
                            }
                        }
                        Log.Info(
                            "分析全部文件耗时: {Milliseconds} ms",
                            Stopwatch.GetElapsedTime(start).TotalMilliseconds
                        );
                        _server.SendNotification(new NotificationMessage("analyzeAllFilesEnd", null));
                        Log.Info("Language server initialized.");
                        _logger.Log("Language server initialized.");
                    },
                    cancellationToken
                );

            return Task.CompletedTask;
        });

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await _server.Run().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Language server error.");
                }
                // 连接中断后关闭应用
                _lifetime.StopApplication();
            },
            cancellationToken
        );
        Log.Info("Language server started.");
        return Task.CompletedTask;
    }

    private void InitialSettings(InitializeParams param)
    {
        string gameRootPath =
            param.InitializationOptions?.RootElement.GetProperty("GameRootFolderPath").GetString()
            ?? string.Empty;
        var blackList =
            param
                .InitializationOptions?.RootElement.GetProperty("Blacklist")
                .EnumerateArray()
                .Select(element => element.GetString() ?? string.Empty) ?? [];
        var errorCodeBlackList =
            param
                .InitializationOptions?.RootElement.GetProperty("ErrorCodeBlacklist")
                .EnumerateArray()
                .Select(element => element.GetString() ?? string.Empty) ?? [];
        // 传来的是 MB, 要转成 byte
        long parseFileMaxBytesSize = (long)(
            (param.InitializationOptions?.RootElement.GetProperty("ParseFileMaxSize").GetDouble() ?? 0)
            * 1024
            * 1024
        );
        string gameLanguage =
            param.InitializationOptions?.RootElement.GetProperty("GameLanguage").GetString() ?? string.Empty;
        string extensionPath =
            param.InitializationOptions?.RootElement.GetProperty("ExtensionPath").GetString() ?? string.Empty;

        _settings.GameRootFolderPath = gameRootPath;
        _settings.ModRootFolderPath = param.RootUri?.FileSystemPath ?? string.Empty;
        _settings.AnalysisBlackList = blackList.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        _settings.ParseFileMaxBytesSize = parseFileMaxBytesSize;
        _settings.GameLanguage = Enums.TryParse<GameLanguage>(gameLanguage, true, out var gameLanguageEnum)
            ? gameLanguageEnum
            : GameLanguage.Default;
        _settings.ExtensionPath = extensionPath;
        _settings.ErrorCodeBlackList = errorCodeBlackList.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        Log.Info("Settings: {@Settings}", _settings);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _currentProcess.Dispose();
        return Task.CompletedTask;
    }
}

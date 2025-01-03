﻿using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Client.ClientCapabilities;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server;
using EmmyLua.LanguageServer.Framework.Protocol.Capabilities.Server.Options;
using EmmyLua.LanguageServer.Framework.Protocol.Message.TextDocument;
using EmmyLua.LanguageServer.Framework.Protocol.Model.TextEdit;
using EmmyLua.LanguageServer.Framework.Server.Handler;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using VModer.Core.Services;

namespace VModer.Core.Handlers;

public sealed class TextDocumentHandler : TextDocumentHandlerBase, IHandler
{
    private readonly GameFilesService _filesService;
    private readonly AnalyzeService _analyzeService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public TextDocumentHandler()
    {
        _filesService = App.Services.GetRequiredService<GameFilesService>();
        _analyzeService = App.Services.GetRequiredService<AnalyzeService>();
    }

    protected override Task Handle(DidOpenTextDocumentParams request, CancellationToken token)
    {
        Log.Info($"Opened file: {request.TextDocument.Uri.Uri}");
        _filesService.AddFile(request.TextDocument.Uri.Uri, request.TextDocument.Text);

        return _analyzeService.AnalyzeFileAsync(request.TextDocument.Uri.Uri);
    }

    protected override Task Handle(DidCloseTextDocumentParams request, CancellationToken token)
    {
        Log.Info($"Closed file: {request.TextDocument.Uri.Uri}");
        _filesService.RemoveFile(request.TextDocument.Uri.Uri);

        return Task.CompletedTask;
    }

    protected override Task Handle(DidChangeTextDocumentParams request, CancellationToken token)
    {
        _filesService.OnFileChanged(request);

        return _analyzeService.AnalyzeFileAsync(request.TextDocument.Uri.Uri);
    }

    protected override Task Handle(WillSaveTextDocumentParams request, CancellationToken token)
    {
        throw new NotImplementedException();
    }

    protected override Task<List<TextEdit>?> HandleRequest(
        WillSaveTextDocumentParams request,
        CancellationToken token
    )
    {
        throw new NotImplementedException();
    }

    public override void RegisterCapability(
        ServerCapabilities serverCapabilities,
        ClientCapabilities clientCapabilities
    )
    {
        serverCapabilities.TextDocumentSync = new TextDocumentSyncOptions
        {
            OpenClose = true,
            Change = TextDocumentSyncKind.Incremental
        };
    }

    public void Initialize()
    {
        
    }
}

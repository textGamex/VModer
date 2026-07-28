using System.Text.Json;
using System.Text.Json.Serialization;
using EmmyLua.LanguageServer.Framework.Protocol.JsonRpc;
using EmmyLua.LanguageServer.Framework.Protocol.Message.Client.ShowMessage;
using EmmyLua.LanguageServer.Framework.Server;

namespace VModer.Core.Services;

public sealed class ServerLoggerService(LanguageServer server)
{
    public void Log(string message)
    {
        server.SendNotification(
            new NotificationMessage(
                "window/logMessage",
                JsonSerializer.SerializeToDocument(
                    new LogMessageParams { Type = MessageType.Log, Message = message },
                    JsonProtocolContext.Default.LogMessageParams
                )
            )
        );
    }

    public sealed class LogMessageParams
    {
        [JsonPropertyName("type")]
        public MessageType Type { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = null!;
    }
}

[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ServerLoggerService.LogMessageParams))]
[JsonSerializable(typeof(MessageType))]
[JsonSerializable(typeof(string))]
internal sealed partial class JsonProtocolContext : JsonSerializerContext;

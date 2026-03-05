using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Models;

namespace VModer.Core.Helpers;

public static class ModifierHelper
{
    public static bool IsModifierNode(Node node, HoverParams request)
    {
        var fileType = GameFileType.FromFilePath(request.TextDocument.Uri.Uri.ToSystemPath());
        return fileType == GameFileType.Modifiers
            || node.Key.Equals("modifier", StringComparison.OrdinalIgnoreCase)
            || node.Key.Equals("modifiers", StringComparison.OrdinalIgnoreCase)
            || node.Key.Equals(Keywords.HiddenModifier, StringComparison.OrdinalIgnoreCase)
            || node.Key.EqualsIgnoreCase("unit_modifiers");
    }
}

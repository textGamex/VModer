using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MethodTimer;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ParadoxPower.ZLinq;
using VModer.Core.Extensions;
using VModer.Core.Models;
using VModer.Core.Services.GameResource.Base;
using ZLinq;

namespace VModer.Core.Services.GameResource;

public sealed class SpriteService
    : CommonResourcesService<SpriteService, FrozenDictionary<string, SpriteInfo>>
{
    [Time("加载界面图片")]
    public SpriteService()
        : base("interface", WatcherFilter.GfxFiles, PathType.Folder, SearchOption.TopDirectoryOnly, true) { }

    private ICollection<FrozenDictionary<string, SpriteInfo>> Sprites => Resources.Values;

    public bool TryGetSpriteInfo(string spriteTypeName, [NotNullWhen(true)] out SpriteInfo? info)
    {
        foreach (var sprite in Sprites)
        {
            if (sprite.TryGetValue(spriteTypeName, out info))
            {
                return true;
            }
        }

        info = null;
        return false;
    }

    protected override FrozenDictionary<string, SpriteInfo> ParseFileToContent(Node rootNode)
    {
        var sprites = new Dictionary<string, SpriteInfo>(16);

        foreach (var child in rootNode.AllArray)
        {
            if (
                !(
                    child.TryGetNode(out var spriteTypes)
                    && spriteTypes.Key.Equals("spriteTypes", StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                continue;
            }

            foreach (
                var spriteType in spriteTypes.NodesValue.Where(node =>
                    node.Key.Equals("spriteType", StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                ParseSpriteTypeNodeToDictionary(spriteType, sprites);
            }
        }

        return sprites.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static void ParseSpriteTypeNodeToDictionary(
        Node spriteTypeNode,
        Dictionary<string, SpriteInfo> sprites
    )
    {
        string? spriteTypeName = null;
        string? textureFilePath = null;
        short frameSum = 1;

        foreach (var leaf in spriteTypeNode.LeavesValue)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals("name", leaf.Key))
            {
                spriteTypeName = leaf.ValueText;
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals("texturefile", leaf.Key))
            {
                textureFilePath = leaf.ValueText;
            }
            else if (
                StringComparer.OrdinalIgnoreCase.Equals("noOfFrames", leaf.Key)
                && leaf.Value.TryGetIntCast(out int frameSumValue)
            )
            {
                Debug.Assert(frameSumValue <= short.MaxValue);
                frameSum = (short)frameSumValue;
            }
        }

        if (spriteTypeName is null || textureFilePath is null)
        {
            return;
        }

        sprites[spriteTypeName] = new SpriteInfo(spriteTypeName, textureFilePath, frameSum);
    }
}

using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Markdown;
using NLog;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ParadoxPower.ZLinq;
using VModer.Core.Extensions;
using VModer.Core.Models;
using VModer.Core.Models.Character;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource;
using VModer.Core.Services.GameResource.Localization;
using VModer.Core.Services.GameResource.Modifiers;
using VModer.Languages;
using ZLinq;

namespace VModer.Core.Services.Hovers;

public sealed class CharacterHoverStrategy : IHoverStrategy
{
    public GameFileType FileType => GameFileType.Character;

    private readonly ModifierDisplayService _modifierDisplayService;
    private readonly LeaderTraitsService _leaderTraitsService;
    private readonly LocalizationFormatService _localizationFormatService;
    private readonly GeneralTraitsService _generalTraitsService;
    private readonly CharacterSkillService _characterSkillService;
    private readonly IImageService _imageService;

    private const int CharacterTypeLevel = 3;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CharacterHoverStrategy(
        ModifierDisplayService modifierDisplayService,
        LeaderTraitsService leaderTraitsService,
        GeneralTraitsService generalTraitsService,
        LocalizationFormatService localizationFormatService,
        CharacterSkillService characterSkillService,
        IImageService imageService
    )
    {
        _modifierDisplayService = modifierDisplayService;
        _leaderTraitsService = leaderTraitsService;
        _generalTraitsService = generalTraitsService;
        _localizationFormatService = localizationFormatService;
        _characterSkillService = characterSkillService;
        _imageService = imageService;
    }

    public string GetHoverText(Node rootNode, HoverParams request)
    {
        return GetCharacterDisplayText(rootNode, request);
    }

    private string GetCharacterDisplayText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var adjacentNode = rootNode.FindAdjacentNodeByPosition(localPosition);

        string result;
        // 当是人物节点时, 逐一进行分析并显示内容
        if (rootNode.IsItemNode("characters", adjacentNode))
        {
            var builder = new MarkdownDocument();

            AddCharacterNameTitle(builder, adjacentNode);

            foreach (var node in adjacentNode.NodesValue)
            {
                string text = GetCharacterDisplayTextByType(node);
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                builder.AppendParagraph(text);
                builder.AppendHorizontalRule();
            }

            if (builder.Length != 0 && builder.ElementAt(builder.Length - 1) is MarkdownHorizontalRule)
            {
                builder.Remove(builder.Length - 1);
            }
            result = builder.ToString();
        }
        else if (adjacentNode.Key.Equals("traits", StringComparison.OrdinalIgnoreCase))
        {
            result = GetTraitsDisplayText(adjacentNode, localPosition);
        }
        else
        {
            result = GetCharacterDisplayTextByType(adjacentNode);
        }

        return result;
    }

    private string GetTraitsDisplayText(Node adjacentNode, LocalPosition localPosition)
    {
        var builder = new MarkdownDocument();
        var child = adjacentNode.FindPointedChildByPosition(localPosition);

        // 当光标放在某一个特质上时
        if (child.TryGetLeafValue(out var traitName))
        {
            var node = Node.Create("traits");
            node.AddChild(traitName);

            AddTraitsDescription(node, builder, LookUpTraitType.All);
        }
        // 当光标放在某一个特质列表上时
        else if (child.TryGetNode(out var traitsNode))
        {
            AddTraitsDescription(traitsNode, builder, LookUpTraitType.All);
        }

        return builder.ToString();
    }

    private void AddCharacterNameTitle(MarkdownDocument builder, Node characterNode)
    {
        var name = characterNode.LeavesValue.FirstOrDefault(leaf =>
            leaf.Key.Equals("name", StringComparison.OrdinalIgnoreCase)
        );

        string nameText = name is null
            ? _localizationFormatService.GetFormatText(characterNode.Key)
            : _localizationFormatService.GetFormatText(name.ValueText);
        builder.AppendHeader(nameText, 2);
        builder.AppendHorizontalRule();
    }

    private string GetCharacterDisplayTextByType(Node node)
    {
        string result = string.Empty;
        if (
            Array.Exists(
                Keywords.GeneralKeywords,
                keyword => keyword.Equals(node.Key, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            result = GetGeneralDisplayText(node);
        }
        else if (node.Key.Equals("advisor", StringComparison.OrdinalIgnoreCase))
        {
            result = GetAdvisorDisplayText(node);
        }
        else if (node.Key.Equals("country_leader", StringComparison.OrdinalIgnoreCase))
        {
            result = GetCountryLeaderDisplayText(node);
        }

        return result;
    }

    private string GetGeneralDisplayText(Node generalNode)
    {
        var builder = new MarkdownDocument();

        builder.AppendHeader(GetGeneralTypeName(generalNode.Key), CharacterTypeLevel);

        var skillSet = SkillType.List.ToDictionary(
            type => type.Value,
            _ => (ushort)0,
            StringComparer.OrdinalIgnoreCase
        );

        foreach (var child in generalNode.AllArray)
        {
            if (child.TryGetLeaf(out var leaf))
            {
                if (skillSet.ContainsKey(leaf.Key) && ushort.TryParse(leaf.ValueText, out ushort value))
                {
                    skillSet[leaf.Key] = value;
                }
            }
            else if (
                child.TryGetNode(out var childNode)
                && childNode.Key.Equals("traits", StringComparison.OrdinalIgnoreCase)
            )
            {
                AddTraitsDescription(childNode, builder, LookUpTraitType.General);
            }
        }

        var skillType = SkillCharacterType.FromCharacterType(generalNode.Key);
        foreach (
            string skillInfo in skillSet
                .AsValueEnumerable()
                .SelectMany(kvp =>
                    GetSkillModifierDescription(SkillType.FromValue(kvp.Key), skillType, kvp.Value)
                )
        )
        {
            builder.AppendParagraph(skillInfo);
        }

        return builder.ToString();
    }

    private IEnumerable<string> GetSkillModifierDescription(
        SkillType skillType,
        SkillCharacterType skillCharacterType,
        ushort level
    )
    {
        var skillModifier = _characterSkillService.GetModifier(skillType, skillCharacterType, level);
        if (skillModifier is null || skillModifier.Modifiers.Count == 0)
        {
            return [];
        }

        return _modifierDisplayService.GetDescription(skillModifier.Modifiers);
    }

    private static string GetGeneralTypeName(string nodeKey)
    {
        return nodeKey switch
        {
            "field_marshal" => Resources.Character_field_marshal,
            "corps_commander" => Resources.Character_corps_commander,
            "navy_leader" => Resources.Character_navy_leader,
            _ => string.Empty
        };
    }

    private string GetAdvisorDisplayText(Node node)
    {
        var builder = new MarkdownDocument();

        builder.AppendHeader(Resources.Character_advisor, CharacterTypeLevel);
        foreach (var child in node.AllArray)
        {
            if (
                child.TryGetNode(out var childNode)
                && childNode.Key.Equals("traits", StringComparison.OrdinalIgnoreCase)
            )
            {
                AddTraitsDescription(childNode, builder, LookUpTraitType.Leader);
            }
            else if (child.TryGetLeaf(out var leaf))
            {
                if (leaf.Key.Equals("slot", StringComparison.OrdinalIgnoreCase))
                {
                    builder.AppendParagraph(
                        $"{Resources.Character_advisor_slot}: {_localizationFormatService.GetFormatText(leaf.ValueText)}"
                    );
                }
            }
        }

        return builder.ToString();
    }

    private void AddTraitsDescription(Node traitsNode, MarkdownDocument builder, LookUpTraitType type)
    {
        var traits = traitsNode.LeafValuesValue.Select(trait => trait.Key);
        builder.AppendHeader($"{Resources.Traits}:", 4);

        foreach (string traitKey in traits)
        {
            string traitName = _localizationFormatService.GetFormatText(traitKey);
            if (
                _imageService.TryGetLocalImagePathBySpriteName(
                    _generalTraitsService.GetSpriteName(traitKey),
                    out string? imageUri
                )
            )
            {
                traitName = $"![icon]({imageUri}){traitName}";
            }
            builder.AppendListItem(traitName, 0);

            IEnumerable<string> modifiers;
            if (type != LookUpTraitType.Leader && _generalTraitsService.TryGetTrait(traitKey, out var trait))
            {
                modifiers = _generalTraitsService.GetModifiersDescription(trait);
            }
            else
            {
                var traitModifiers = GetTraitModifiersByType(traitKey, type);
                modifiers = _modifierDisplayService.GetDescription(traitModifiers);
            }

            foreach (string modifier in modifiers)
            {
                builder.AppendListItem(
                    modifier,
                    modifier.StartsWith(ModifierDisplayService.NodeModifierChildrenPrefix) ? 2 : 1
                );
            }
        }

        builder.AppendHorizontalRule();
    }

    private IEnumerable<IModifier> GetTraitModifiersByType(string traitKey, LookUpTraitType type)
    {
        switch (type)
        {
            case LookUpTraitType.All:
            {
                _generalTraitsService.TryGetTrait(traitKey, out var trait);
                if (trait is not null)
                {
                    return trait.AllModifiers;
                }

                _leaderTraitsService.TryGetValue(traitKey, out var leaderTrait);
                return leaderTrait?.Modifiers ?? [];
            }
            case LookUpTraitType.General:
            {
                _generalTraitsService.TryGetTrait(traitKey, out var trait);
                return trait?.AllModifiers ?? [];
            }
            case LookUpTraitType.Leader:
            {
                _leaderTraitsService.TryGetValue(traitKey, out var trait);
                return trait?.Modifiers ?? [];
            }
            default:
                return [];
        }
    }

    private enum LookUpTraitType : byte
    {
        All,
        General,
        Leader
    }

    private string GetCountryLeaderDisplayText(Node leaderNode)
    {
        var builder = new MarkdownDocument();

        builder.AppendHeader(Resources.Character_country_leader, CharacterTypeLevel);
        var ideology = leaderNode.Leaves.FirstOrDefault(leaf =>
            leaf.Key.Equals("ideology", StringComparison.OrdinalIgnoreCase)
        );
        if (ideology is not null)
        {
            builder.AppendParagraph(
                $"{Resources.Ideology}: {_localizationFormatService.GetFormatText(ideology.ValueText)}"
            );
        }

        var traits = leaderNode.NodesValue.FirstOrDefault(node =>
            node.Key.Equals("traits", StringComparison.OrdinalIgnoreCase)
        );

        if (traits is not null)
        {
            AddTraitsDescription(traits, builder, LookUpTraitType.Leader);
        }

        return builder.ToString();
    }
}

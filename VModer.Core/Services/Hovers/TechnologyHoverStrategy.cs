using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Markdown;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ParadoxPower.ZLinq;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Markdown;
using VModer.Core.Models;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource;
using VModer.Core.Services.GameResource.Localization;
using VModer.Core.Services.GameResource.Modifiers;
using VModer.Languages;
using ZLinq;

namespace VModer.Core.Services.Hovers;

public sealed class TechnologyHoverStrategy(
    ModifierDisplayService modifierDisplayService,
    UnitService unitService,
    LocalizationFormatService localizationFormatService
) : IHoverStrategy
{
    public GameFileType FileType => GameFileType.Technology;

    // https://hoi4.paradoxwikis.com/Technology_modding#Modifiers
    private static readonly string[] LeafKeywords =
    [
        "research_cost",
        "start_year",
        "show_equipment_icon",
        "force_use_small_tech_layout",
        "doctrine",
        "doctrine_name",
        "is_special_project_tech",
        "desc",
        "show_effect_as_desc",
        "xp_unlock_cost",
        "xp_research_type"
    ];

    private const string TechnologiesKeyword = "technologies";
    private const string CategoriesKeyword = "categories";
    private const string EnableEquipmentModulesKeyword = "enable_equipment_modules";

    public string GetHoverText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var adjacentNode = rootNode.FindAdjacentNodeByPosition(localPosition);

        string hoverText;
        if (rootNode.IsItemNode(TechnologiesKeyword, adjacentNode))
        {
            hoverText = GetTechnologyNodeText(rootNode, adjacentNode);
        }
        else if (adjacentNode.Key.EqualsIgnoreCase(CategoriesKeyword))
        {
            hoverText = GetCategoriesDescriptionText(adjacentNode, localPosition);
        }
        else if (adjacentNode.Key.EqualsIgnoreCase(EnableEquipmentModulesKeyword))
        {
            hoverText = GetEnableModulesHoverText(adjacentNode, localPosition);
        }
        // 当为具体的修饰符时
        else
        {
            var builder = new MarkdownDocument();
            var pointedChild = adjacentNode.FindPointedChildByPosition(localPosition);

            ProcessChildForModifiers(pointedChild, adjacentNode, rootNode, builder);
            hoverText = builder.ToString();
        }

        return hoverText;
    }

    private string GetEnableModulesHoverText(Node adjacentNode, LocalPosition localPosition)
    {
        var builder = new MarkdownDocument();
        var pointedChild = adjacentNode.FindPointedChildByPosition(localPosition);
        if (pointedChild.TryGetLeafValue(out var leafValue))
        {
            // 直接显示叶子值
            builder.AppendParagraph(localizationFormatService.GetFormatText(leafValue.ValueText));
        }
        else if (pointedChild.TryGetNode(out var node))
        {
            AddEnableModulesDescription(node, builder);
        }

        return builder.ToString();
    }

    private void AddEnableModulesDescription(Node node, MarkdownDocument builder)
    {
        builder.AppendHeader(Resources.EnableEquipmentModules, 3);
        foreach (var enableModules in node.LeafValuesValue)
        {
            builder.AppendListItem(
                localizationFormatService.GetFormatText(enableModules.ValueText),
                0
            );
        }
    }

    private string GetTechnologyNodeText(Node rootNode, Node node)
    {
        var builder = new MarkdownDocument();
        foreach (var child in node.AllArray)
        {
            ProcessChildForModifiers(child, node, rootNode, builder);
        }

        // 类别信息会被直接插入到头部, 所以我们需要在处理结束后再将科技名称插入到头部
        builder.Insert(0, new MarkdownHeader(localizationFormatService.GetFormatText(node.Key), 3));
        builder.Insert(1, new MarkdownHorizontalRule());
        return builder.ToString();
    }

    private string GetCategoriesDescriptionText(Node categoriesNode, LocalPosition localPosition)
    {
        var builder = new MarkdownDocument();
        var pointedChild = categoriesNode.FindPointedChildByPosition(localPosition);
        if (pointedChild.TryGetNode(out var node))
        {
            AddCategoriesDescription(node.LeafValues, builder);
        }
        // 当光标放在某一个类别上时
        else if (pointedChild.TryGetLeafValue(out var leafValue))
        {
            AddCategoriesDescription([leafValue], builder);
        }

        return builder.ToString();
    }

    private void ProcessChildForModifiers(Child child, Node parent, Node rootNode, MarkdownDocument builder)
    {
        if (
            child.TryGetLeaf(out var leaf)
            && !Array.Exists(
                LeafKeywords,
                keyword => keyword.Equals(leaf.Key, StringComparison.OrdinalIgnoreCase)
            )
            // 防止错误地显示某些判断条件的Hover
            && (unitService.Contains(parent.Key) || rootNode.IsItemNode(TechnologiesKeyword, parent))
        )
        {
            AddDescription(modifierDisplayService.GetDescription(LeafModifier.FromLeaf(leaf)));
        }
        else if (child.TryGetNode(out var node))
        {
            if (unitService.Contains(node.Key))
            {
                foreach (
                    string description in modifierDisplayService.GetDescription(NodeModifier.FromNode(node))
                )
                {
                    AddDescription(description);
                }
            }
            else if (node.Key.Equals(CategoriesKeyword, StringComparison.OrdinalIgnoreCase))
            {
                AddCategoriesDescription(node.LeafValues, builder);
            }
            else if (node.Key.EqualsIgnoreCase(EnableEquipmentModulesKeyword))
            {
                AddEnableModulesDescription(node, builder);
            }
        }

        return;

        void AddDescription(string description)
        {
            builder.AppendParagraph(
                description.StartsWith(ModifierDisplayService.NodeModifierChildrenPrefix)
                    ? $"- {description}"
                    : description
            );
        }
    }

    private void AddCategoriesDescription(IEnumerable<LeafValue> leafValues, MarkdownDocument builder)
    {
        builder.Insert(0, new MarkdownHeader(Resources.Categories, 3));
        builder.Insert(1, new MarkdownHorizontalRule());

        // 在我们需要反转一下, 以确保显示顺序和实际顺序一致
        foreach (var leafValue in leafValues.AsValueEnumerable().Reverse())
        {
            builder.Insert(
                1,
                new MarkdownListItem(localizationFormatService.GetFormatText(leafValue.ValueText), 1)
            );
        }
    }
}

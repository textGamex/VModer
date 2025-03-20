﻿using EmmyLua.LanguageServer.Framework.Protocol.Message.Hover;
using Markdown;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using VModer.Core.Extensions;
using VModer.Core.Infrastructure.Markdown;
using VModer.Core.Models;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource;
using VModer.Core.Services.GameResource.Localization;
using VModer.Core.Services.GameResource.Modifiers;
using VModer.Languages;

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
        "desc"
    ];

    public string GetHoverText(Node rootNode, HoverParams request)
    {
        var localPosition = request.Position.ToLocalPosition();
        var adjacentNode = rootNode.FindAdjacentNodeByPosition(localPosition);

        string hoverText;
        if (rootNode.IsItemNode("technologies", adjacentNode))
        {
            hoverText = GetTechnologyNodeText(rootNode, adjacentNode);
        }
        else if (adjacentNode.Key.Equals("categories", StringComparison.OrdinalIgnoreCase))
        {
            hoverText = GetCategoriesDescriptionText(adjacentNode, localPosition);
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

    private string GetTechnologyNodeText(Node rootNode, Node node)
    {
        var builder = new MarkdownDocument();
        foreach (var child in node.AllArray)
        {
            ProcessChildForModifiers(child, node, rootNode, builder);
        }

        return builder.ToString();
    }

    private string GetCategoriesDescriptionText(Node adjacentNode, LocalPosition localPosition)
    {
        var builder = new MarkdownDocument();
        var pointedChild = adjacentNode.FindPointedChildByPosition(localPosition);
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
            && (unitService.Contains(parent.Key) || rootNode.IsItemNode("technologies", parent))
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
            else if (node.Key.Equals("categories", StringComparison.OrdinalIgnoreCase))
            {
                AddCategoriesDescription(node.LeafValues, builder);
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
        foreach (var leafValue in leafValues.Reverse())
        {
            builder.Insert(
                1,
                new MarkdownListItem(localizationFormatService.GetFormatText(leafValue.ValueText), 1)
            );
        }
    }
}

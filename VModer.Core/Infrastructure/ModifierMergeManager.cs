﻿using System.Globalization;
using System.Runtime.InteropServices;
using NLog;
using VModer.Core.Models;
using VModer.Core.Models.Modifiers;

namespace VModer.Core.Infrastructure;

/// <summary>
/// 修饰符合并管理器
/// </summary>
public sealed class ModifierMergeManager
{
    private readonly Dictionary<string, decimal> _leafModifiers = [];
    private readonly Dictionary<string, Dictionary<string, decimal>> _nodeModifiers = [];
    private readonly List<LeafModifier> _customEffectTooltipLocalizationKeys = [];

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void Clear()
    {
        _leafModifiers.Clear();
        _nodeModifiers.Clear();
        _customEffectTooltipLocalizationKeys.Clear();
    }

    public void Add(IModifier modifier)
    {
        if (modifier.Type == ModifierType.Leaf)
        {
            AddLeafModifier((LeafModifier)modifier);
        }
        else if (modifier.Type == ModifierType.Node)
        {
            AddNodeModifier((NodeModifier)modifier);
        }
        else
        {
            Log.Warn("Unknown modifier type: {Type}", modifier.Type);
        }
    }

    public void AddRange(IEnumerable<IModifier> modifiers)
    {
        foreach (var modifier in modifiers)
        {
            Add(modifier);
        }
    }

    public void Remove(IModifier modifier)
    {
        if (modifier.Type == ModifierType.Leaf)
        {
            RemoveLeafModifier((LeafModifier)modifier);
        }
        else if (modifier.Type == ModifierType.Node)
        {
            RemoveNodeModifier((NodeModifier)modifier);
        }
        else
        {
            Log.Warn("Unknown modifier type: {Type}", modifier.Type);
        }
    }

    public void RemoveAll(IEnumerable<IModifier> modifiers)
    {
        foreach (var modifier in modifiers)
        {
            Remove(modifier);
        }
    }

    private void RemoveNodeModifier(NodeModifier removedNodeModifier)
    {
        if (!_nodeModifiers.TryGetValue(removedNodeModifier.Key, out var modifierMap))
        {
            return;
        }

        foreach (var removedModifier in removedNodeModifier.Modifiers)
        {
            switch (removedModifier.Type)
            {
                case ModifierType.Leaf:
                    RemoveLeafModifierInDictionary(modifierMap, (LeafModifier)removedModifier);
                    break;
                case ModifierType.Node:
                    RemoveNodeModifier((NodeModifier)removedModifier);
                    break;
                default:
                    Log.Warn("Unknown modifier type: {Type}", removedModifier.Type);
                    break;
            }
        }

        if (modifierMap.Count == 0)
        {
            _nodeModifiers.Remove(removedNodeModifier.Key);
        }
    }

    private void RemoveLeafModifier(LeafModifier modifier)
    {
        if (StringComparer.OrdinalIgnoreCase.Equals(modifier.Key, LeafModifier.CustomEffectTooltipKey))
        {
            _customEffectTooltipLocalizationKeys.Remove(modifier);
            return;
        }

        RemoveLeafModifierInDictionary(_leafModifiers, modifier);
    }

    private static void RemoveLeafModifierInDictionary(
        Dictionary<string, decimal> dictionary,
        LeafModifier modifier
    )
    {
        decimal value = decimal.TryParse(modifier.Value, out decimal result) ? result : 0;
        if (dictionary.TryGetValue(modifier.Key, out decimal rawValue))
        {
            decimal newValue = rawValue - value;
            if (newValue == decimal.Zero)
            {
                dictionary.Remove(modifier.Key);
            }
            else
            {
                dictionary[modifier.Key] = newValue;
            }
            return;
        }

        dictionary[modifier.Key] = -value;
    }

    public IEnumerable<IModifier> GetMergedModifiers()
    {
        var leafModifiers = _leafModifiers.Select(
            IModifier (pair) =>
                new LeafModifier(
                    pair.Key,
                    pair.Value.ToString(CultureInfo.InvariantCulture),
                    GameValueType.Float
                )
        );
        var nodeModifiers = _nodeModifiers.Select(
            IModifier (pair) =>
                new NodeModifier(
                    pair.Key,
                    pair.Value.Select(y => new LeafModifier(
                        y.Key,
                        y.Value.ToString(CultureInfo.InvariantCulture),
                        GameValueType.Float
                    ))
                )
        );

        return _customEffectTooltipLocalizationKeys.Concat(leafModifiers).Concat(nodeModifiers);
    }

    private void AddLeafModifier(LeafModifier modifier)
    {
        if (StringComparer.OrdinalIgnoreCase.Equals(LeafModifier.CustomEffectTooltipKey, modifier.Key))
        {
            _customEffectTooltipLocalizationKeys.Add(modifier);
            return;
        }

        AddLeafModifierToDictionary(_leafModifiers, modifier);
    }

    private void AddNodeModifier(NodeModifier modifier)
    {
        if (_nodeModifiers.TryGetValue(modifier.Key, out var modifierMap))
        {
            foreach (var childModifier in modifier.Modifiers)
            {
                switch (childModifier.Type)
                {
                    case ModifierType.Leaf:
                        AddLeafModifierToDictionary(modifierMap, (LeafModifier)childModifier);
                        break;
                    case ModifierType.Node:
                        AddNodeModifier((NodeModifier)childModifier);
                        break;
                    default:
                        Log.Warn("Unknown modifier type: {Type}", childModifier.Type);
                        break;
                }
            }
        }
        else
        {
            var newModifierMap = new Dictionary<string, decimal>(modifier.Modifiers.Count);
            foreach (var childModifier in modifier.Modifiers)
            {
                if (childModifier.Type == ModifierType.Leaf)
                {
                    var leafModifier = (LeafModifier)childModifier;
                    decimal value = decimal.TryParse(leafModifier.Value, out decimal result) ? result : 0;
                    newModifierMap[leafModifier.Key] = value;
                }
                else if (childModifier.Type == ModifierType.Node)
                {
                    AddNodeModifier((NodeModifier)childModifier);
                }
                else
                {
                    Log.Warn("Unknown modifier type: {Type}", childModifier.Type);
                }
            }
            _nodeModifiers.Add(modifier.Key, newModifierMap);
        }
    }

    private static void AddLeafModifierToDictionary(
        Dictionary<string, decimal> dictionary,
        LeafModifier modifier
    )
    {
        decimal value = decimal.TryParse(modifier.Value, out decimal result) ? result : 0;
        if (value == decimal.Zero)
        {
            return;
        }

        ref decimal refValue = ref CollectionsMarshal.GetValueRefOrAddDefault(
            dictionary,
            modifier.Key,
            out bool exists
        );
        if (exists)
        {
            decimal newValue = refValue + value;
            if (newValue == decimal.Zero)
            {
                dictionary.Remove(modifier.Key);
            }
            else
            {
                refValue = newValue;
            }
        }
        else
        {
            refValue = value;
        }
    }
}

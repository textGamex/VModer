using System.Diagnostics.CodeAnalysis;
using NLog;
using VModer.Core.Models;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource.Localization;

namespace VModer.Core.Services.GameResource.Modifiers;

public sealed class ModifierService
{
    private readonly ILocalizationService _localizationService;
    private readonly ModiferLocalizationFormatService _modifierLocalizationFormatService;

    public ModifierService(
        ILocalizationService localizationService,
        ModiferLocalizationFormatService modifierLocalizationFormatService
    )
    {
        _localizationService = localizationService;
        _modifierLocalizationFormatService = modifierLocalizationFormatService;
    }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public bool TryGetLocalizationName(string modifierKey, [NotNullWhen(true)] out string? value)
    {
        if (_localizationService.TryGetValueInAll(modifierKey, out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIER_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIERS_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIER_NAVAL_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIER_UNIT_LEADER_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIER_ARMY_LEADER_{modifierKey}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"MODIFIER_{modifierKey}_LIMIT", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"{modifierKey}_tt", out value))
        {
            return true;
        }

        return false;
    }
    
    public string GetLocalizationName(string modifierKey)
    {
        if (TryGetLocalizationName(modifierKey, out string? value))
        {
            return value;
        }

        if (TryGetLocalizationNameInState(modifierKey, out value))
        {
            return value;
        }

        return modifierKey;
    }

    private bool TryGetLocalizationNameInState(string modifier, [NotNullWhen(true)] out string? value)
    {
        if (_localizationService.TryGetValue($"STAT_ARMY_{modifier}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"STAT_NAVY_{modifier}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"STAT_COMMON_{modifier}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"STAT_NAVY_HG_{modifier}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"STAT_NAVY_LG_{modifier}", out value))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"STAT_RAILWAY_{modifier}", out value))
        {
            return true;
        }

        return false;
    }

    public bool TryGetLocalizationFormat(string modifier, [NotNullWhen(true)] out string? result)
    {
        if (_modifierLocalizationFormatService.TryGetLocalizationFormat(modifier, out result))
        {
            return true;
        }

        if (_localizationService.TryGetValue($"{modifier}_tt", out result))
        {
            return true;
        }

        if (
            !modifier.StartsWith("modifier_")
            && _localizationService.TryGetValue($"modifier_{modifier}_tt", out result)
        )
        {
            return true;
        }

        return _localizationService.TryGetValue(modifier, out result);
    }

    private static ModifierEffectType GetModifierType(string modifierName, string modifierFormat)
    {
        for (int index = modifierFormat.Length - 1; index >= 0; index--)
        {
            char c = modifierFormat[index];
            switch (c)
            {
                case '+':
                    return ModifierEffectType.Positive;
                case '-':
                    return ModifierEffectType.Negative;
            }
        }

        return ModifierEffectType.Unknown;
    }

    /// <summary>
    /// 获取 Modifier 数值的显示值
    /// </summary>
    /// <param name="leafModifier">包含关键字和对应值的修饰符对象</param>
    /// <param name="modifierDisplayFormat">修饰符对应的格式化设置文本, 为空时使用百分比格式</param>
    /// <returns>应用<c>modifierDisplayFormat</c>格式的<c>LeafModifier.Value</c>的的显示值</returns>
    public string GetDisplayValue(LeafModifier leafModifier, string modifierDisplayFormat)
    {
        string result = leafModifier.Value;
        if (leafModifier.ValueType is GameValueType.Int or GameValueType.Float)
        {
            double value = double.Parse(leafModifier.Value);
            string sign = leafModifier.Value.StartsWith('-') ? string.Empty : "+";

            char displayDigits = GetDisplayDigits(modifierDisplayFormat);
            bool isPercentage =
                string.IsNullOrEmpty(modifierDisplayFormat)
                || modifierDisplayFormat.Contains('%')
                || leafModifier.Key.EndsWith("factor");

            string percentageSymbol = string.Empty;
            if (modifierDisplayFormat.Contains("%%"))
            {
                isPercentage = false;
                percentageSymbol = "%";
            }
            char format = isPercentage ? 'P' : 'F';

            result = $"{sign}{value.ToString($"{format}{displayDigits}")}{percentageSymbol}";
        }

        return result;
    }

    private static char GetDisplayDigits(string modifierDescription)
    {
        char displayDigits = '1';
        for (int i = modifierDescription.Length - 1; i >= 0; i--)
        {
            char c = modifierDescription[i];
            if (char.IsDigit(c))
            {
                displayDigits = c;
                break;
            }
        }

        return displayDigits;
    }
}

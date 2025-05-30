using MethodTimer;
using ParadoxPower.Process;
using VModer.Core.Models.Character;
using VModer.Core.Services.GameResource.Base;
using ZLinq;
using ZLinq.Linq;

namespace VModer.Core.Services.GameResource;

/// <summary>
/// 用于提供人物技能的信息, 如最大值, 修正等
/// </summary>
[method: Time("加载人物技能信息")]
public sealed class CharacterSkillService()
    : CommonResourcesService<CharacterSkillService, SkillInfo[]>(
        Path.Combine(Keywords.Common, "unit_leader"),
        WatcherFilter.Text
    )
{
    private ValueEnumerable<
        SelectMany<FromEnumerable<SkillInfo[]>, SkillInfo[], SkillInfo>,
        SkillInfo
    > Skills => Resources.Values.AsValueEnumerable().SelectMany(s => s);

    private const ushort DefaultSkillMaxValue = 1;

    public ushort GetMaxSkillValue(SkillType skillType, SkillCharacterType skillCharacterType)
    {
        foreach (var skillInfo in Skills.Where(skill => skill.SkillType == skillType))
        {
            ushort? value = skillInfo.GetMaxValue(skillCharacterType);
            if (value.HasValue)
            {
                return value.Value;
            }
        }

        return DefaultSkillMaxValue;
    }

    public SkillModifier? GetModifier(SkillType skillType, SkillCharacterType characterType, ushort level)
    {
        foreach (var skillInfo in Skills.Where(skill => skill.SkillType == skillType))
        {
            var skillModifier = skillInfo.GetModifierDescription(characterType, level);
            if (skillModifier is not null)
            {
                return skillModifier;
            }
        }

        return null;
    }

    protected override SkillInfo[]? ParseFileToContent(Node rootNode)
    {
        var skills = new List<SkillInfo>();

        foreach (var node in rootNode.Nodes)
        {
            SkillType skillType;
            if (StringComparer.OrdinalIgnoreCase.Equals(node.Key, "leader_attack_skills"))
            {
                skillType = SkillType.Attack;
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(node.Key, "leader_defense_skills"))
            {
                skillType = SkillType.Defense;
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(node.Key, "leader_skills"))
            {
                skillType = SkillType.Level;
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(node.Key, "leader_coordination_skills"))
            {
                skillType = SkillType.Coordination;
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(node.Key, "leader_logistics_skills"))
            {
                skillType = SkillType.Logistics;
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(node.Key, "leader_maneuvering_skills"))
            {
                skillType = SkillType.Maneuvering;
            }
            else if (StringComparer.OrdinalIgnoreCase.Equals(node.Key, "leader_planning_skills"))
            {
                skillType = SkillType.Planning;
            }
            else
            {
                return null;
            }

            skills.Add(ParseSkills(node, skillType));
        }

        return skills.ToArray();
    }

    private SkillInfo ParseSkills(Node node, SkillType skillType)
    {
        var skillMap = new Dictionary<SkillCharacterType, ushort>(3);
        var skillModifiers = new Dictionary<SkillCharacterType, List<SkillModifier>>(3);

        foreach (var skillInfoNode in node.Nodes)
        {
            var skillTypeLeaf = skillInfoNode.Leaves.FirstOrDefault(leaf =>
                StringComparer.OrdinalIgnoreCase.Equals(leaf.Key, "type")
            );
            if (skillTypeLeaf is null)
            {
                continue;
            }

            if (!SkillCharacterType.TryFromName(skillTypeLeaf.ValueText, true, out var type))
            {
                continue;
            }

            var currentSkillMaxValue = ushort.TryParse(skillInfoNode.Key, out var value)
                ? value
                : DefaultSkillMaxValue;
            var modifierNode = skillInfoNode.Nodes.FirstOrDefault(n =>
                StringComparer.OrdinalIgnoreCase.Equals(n.Key, "modifier")
            );
            if (skillMap.TryGetValue(type, out var maxValue))
            {
                skillMap[type] = Math.Max(maxValue, currentSkillMaxValue);
            }
            else
            {
                skillMap.Add(type, currentSkillMaxValue);
            }

            skillModifiers.TryGetValue(type, out var modifiers);
            if (modifiers is null)
            {
                modifiers = [];
                skillModifiers.Add(type, modifiers);
            }
            modifiers.Add(new SkillModifier(currentSkillMaxValue, modifierNode?.Leaves ?? []));
        }

        var skillInfo = new SkillInfo(skillType);
        foreach (var (type, maxValue) in skillMap)
        {
            skillInfo.Add(new Skill(type, maxValue, skillModifiers[type]));
        }

        return skillInfo;
    }
}

using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using ParadoxPower.CSharpExtensions;
using ParadoxPower.Process;
using ParadoxPower.ZLinq;
using VModer.Core.Extensions;
using VModer.Core.Models.Character;
using VModer.Core.Services.GameResource;
using VModer.Languages;
using ZLinq;

namespace VModer.Core.Analyzers;

public sealed class CharacterAnalyzerService
{
    private readonly CharacterSkillService _characterSkillService;

    public CharacterAnalyzerService(CharacterSkillService characterSkillService)
    {
        _characterSkillService = characterSkillService;
    }

    public List<Diagnostic> Analyze(Node rootNode)
    {
        var list = new List<Diagnostic>();
        foreach (
            var charactersNode in rootNode.NodesValue.Where(node =>
                node.Key.Equals("characters", StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            foreach (var character in charactersNode.NodesValue)
            {
                foreach (var childNode in character.NodesValue)
                {
                    if (
                        !Array.Exists(
                            Keywords.GeneralKeywords,
                            keyword => childNode.Key.Equals(keyword, StringComparison.OrdinalIgnoreCase)
                        )
                    )
                    {
                        continue;
                    }

                    AnalyzeCharacter(childNode, list);
                }
            }
        }

        return list;
    }

    private void AnalyzeCharacter(Node generalNode, List<Diagnostic> list)
    {
        var skillType = SkillCharacterType.FromCharacterType(generalNode.Key);
        foreach (var skillLeaf in generalNode.LeavesValue)
        {
            if (!skillLeaf.Value.TryGetInt(out int value))
            {
                continue;
            }

            var skill = SkillType
                .List.AsValueEnumerable()
                .FirstOrDefault(skill =>
                    skill.Value.Equals(skillLeaf.Key, StringComparison.OrdinalIgnoreCase)
                );
            if (skill is null)
            {
                continue;
            }

            ushort maxValue = _characterSkillService.GetMaxSkillValue(skill, skillType);
            if (value > maxValue)
            {
                list.Add(
                    new Diagnostic
                    {
                        Range = skillLeaf.Position.ToDocumentRange(),
                        Message = string.Format(
                            Resources.ErrorMessage_SkillExceedsMaxValue,
                            generalNode.Key,
                            skillLeaf.Key,
                            maxValue
                        ),
                        Severity = DiagnosticSeverity.Error,
                        Code = ErrorCode.VM1004
                    }
                );
            }
        }
    }
}

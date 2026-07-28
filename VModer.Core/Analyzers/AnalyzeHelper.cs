using EmmyLua.LanguageServer.Framework.Protocol.Model.Diagnostic;
using ParadoxPower.Process;
using ParadoxPower.ZLinq;
using VModer.Core.Extensions;
using VModer.Languages;
using ZLinq;

namespace VModer.Core.Analyzers;

public static class AnalyzeHelper
{
    public static void AnalyzeEmptyNode(Node node, List<Diagnostic> list)
    {
        foreach (var childNode in node.NodesValue)
        {
            if (childNode.AllArray.Length == 0)
            {
                list.Add(
                    new Diagnostic
                    {
                        Range = childNode.Position.ToDocumentRange(),
                        Code = ErrorCode.VM2000,
                        Message = Resources.Analyzer_UnusedStatement,
                        Severity = DiagnosticSeverity.Warning,
                        Tags = [DiagnosticTag.Unnecessary]
                    }
                );
            }
            else
            {
                AnalyzeEmptyNode(childNode, list);
            }
        }
    }
}
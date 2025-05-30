﻿using EmmyLua.LanguageServer.Framework.Protocol.Model;
using VModer.Core.Models;

namespace VModer.Core.Extensions;

public static class PositionExtensions
{
    /// <summary>
    /// 将 VS Code 的位置转换为 AST 中的位置(简单的加一)
    /// </summary>
    /// <param name="position"></param>
    /// <returns>AST 中的位置信息</returns>
    /// <remarks>
    /// 因为 VS Code 中的位置是以 0 开始,而解析器中的位置是以 1 开始
    /// </remarks>
    public static LocalPosition ToLocalPosition(this Position position)
    {
        return new LocalPosition(position.Line + 1, position.Character);
    }
}
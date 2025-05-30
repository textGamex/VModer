﻿using System.Collections.Concurrent;
using Ardalis.SmartEnum;
using DotNet.Globbing;
using ZLinq;

namespace VModer.Core.Models;

public sealed class GameFileType(string name, string value) : SmartEnum<GameFileType, string>(name, value)
{
    public static readonly GameFileType Unknown = new(nameof(Unknown), string.Empty);
    public static readonly GameFileType State = new(nameof(State), "**/history/states/*.txt");
    public static readonly GameFileType Character = new(nameof(Character), "**/common/characters/*.txt");
    public static readonly GameFileType Countries = new(nameof(Countries), "**/common/countries/*.txt");
    public static readonly GameFileType Modifiers = new(nameof(Modifiers), "**/common/modifiers/*.txt");
    public static readonly GameFileType CountryDefine =
        new(nameof(CountryDefine), "**/history/countries/*.txt");
    public static readonly GameFileType CoreGfx = new(nameof(CoreGfx), "**/interface/core.gfx");
    public static readonly GameFileType Ideologies = new(nameof(Ideologies), "**/common/ideologies/*.txt");
    public static readonly GameFileType Technology = new(nameof(Technology), "**/common/technologies/*.txt");

    private static readonly ConcurrentDictionary<string, Glob> Globs = new();

    static GameFileType()
    {
        GlobOptions.Default.Evaluation.CaseInsensitive = true;
    }

    public static GameFileType FromFilePath(string filePath)
    {
        foreach (var fileType in List.AsValueEnumerable().Where(type => type != Unknown))
        {
            if (IsGlobMatch(fileType, filePath))
            {
                return fileType;
            }
        }

        return Unknown;
    }

    private static bool IsGlobMatch(GameFileType fileType, string filePath)
    {
        string? key = fileType.Name;
        string? patter = fileType.Value;
        if (!Globs.TryGetValue(key, out var glob))
        {
            glob = Glob.Parse(patter);
            Globs.TryAdd(key, glob);
        }

        return glob.IsMatch(filePath);
    }
}

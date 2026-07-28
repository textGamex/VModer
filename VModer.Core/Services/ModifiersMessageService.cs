using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsvHelper;
using MethodTimer;
using VModer.Core.Dto;
using VModer.Core.Models.Modifiers;
using VModer.Core.Services.GameResource;
using VModer.Core.Services.GameResource.Localization;
using VModer.Core.Services.GameResource.Modifiers;
using ZLinq;

namespace VModer.Core.Services;

public sealed class ModifiersMessageService
{
    private readonly ModifierDto[] _modifierDto;
    private static readonly char[] TrimChars = [':', '：'];

    [Time]
    public ModifiersMessageService(
        BuildingsService buildingsService,
        OreService oreService,
        ModifierService modifierService,
        LocalizationFormatService localizationFormatService,
        IdeologiesService ideologiesService,
        UnitService unitService,
        OperationsService operationsService,
        SpecialProjectsService specialProjectsService
    )
    {
        string filePtah = Path.Combine(App.AssetsFolder, "Modifiers.csv");

        using var csv = new CsvReader(File.OpenText(filePtah), CultureInfo.InvariantCulture);

        var modifiers = new List<ModifierMessage>();
        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            string name = csv.GetField<string>("Name") ?? string.Empty;
            string[] categories = csv.GetField<string>("Categories")?.Split(';') ?? [];
            var modifierMessage = new ModifierMessage(name, categories);
            modifiers.Add(modifierMessage);
        }

        ReadDynamicModifiers(
            modifiers,
            buildingsService,
            oreService,
            ideologiesService,
            unitService,
            operationsService,
            specialProjectsService
        );

        _modifierDto = modifiers
            .AsValueEnumerable()
            .Select(message => new ModifierDto
            {
                Name = message.Name,
                Categories = message.Categories,
                LocalizedName = string.Concat(
                        localizationFormatService
                            .GetFormatTextInfo(modifierService.GetLocalizationName(message.Name))
                            .Select(info => info.DisplayText)
                    )
                    .TrimEnd(TrimChars)
            })
            .ToArray();
    }

    private static void ReadDynamicModifiers(
        List<ModifierMessage> modifiers,
        BuildingsService buildingsService,
        OreService oreService,
        IdeologiesService ideologiesService,
        UnitService unitService,
        OperationsService operationsService,
        SpecialProjectsService specialProjectsService
    )
    {
        string filePtah = Path.Combine(App.AssetsFolder, "DynamicModifiers.csv");
        using var dynamicCsv = new CsvReader(File.OpenText(filePtah), CultureInfo.InvariantCulture);
        dynamicCsv.Read();
        dynamicCsv.ReadHeader();

        while (dynamicCsv.Read())
        {
            string name = dynamicCsv.GetField<string>("Name") ?? string.Empty;
            string[] categories = dynamicCsv.GetField<string>("Categories")?.Split(';') ?? [];

            if (name.Contains("<Building>", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var building in buildingsService.All)
                {
                    modifiers.Add(
                        new ModifierMessage(
                            name.Replace("<Building>", building.Name, StringComparison.OrdinalIgnoreCase),
                            categories
                        )
                    );
                }
            }
            else if (name.Contains("<Resource>", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string oreName in oreService.All)
                {
                    modifiers.Add(
                        new ModifierMessage(
                            name.Replace("<Resource>", oreName, StringComparison.OrdinalIgnoreCase),
                            categories
                        )
                    );
                }
            }
            else if (name.Contains("<Ideology>", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string ideologyName in ideologiesService.All)
                {
                    modifiers.Add(
                        new ModifierMessage(
                            name.Replace("<Ideology>", ideologyName, StringComparison.OrdinalIgnoreCase),
                            categories
                        )
                    );
                }
            }
            else if (name.Contains("<Unit>", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string unitName in unitService.All)
                {
                    modifiers.Add(
                        new ModifierMessage(
                            name.Replace("<Unit>", unitName, StringComparison.OrdinalIgnoreCase),
                            categories
                        )
                    );
                }
            }
            else if (name.Contains("<Operation>", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string operationName in operationsService.OperationNames)
                {
                    modifiers.Add(
                        new ModifierMessage(
                            name.Replace("<Operation>", operationName, StringComparison.OrdinalIgnoreCase),
                            categories
                        )
                    );
                }
            }
            else if (name.Contains("<SpecialProject>", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string projectName in specialProjectsService.SpecialProjectNames)
                {
                    modifiers.Add(
                        new ModifierMessage(
                            name.Replace("<SpecialProject>", projectName, StringComparison.OrdinalIgnoreCase),
                            categories
                        )
                    );
                }
            }
            else
            {
                var modifierMessage = new ModifierMessage(name, categories);
                modifiers.Add(modifierMessage);
            }
        }
    }

    [Time]
    public JsonDocument GetModifierJson()
    {
        return JsonDocument.Parse(
            JsonSerializer.Serialize(_modifierDto, ModifierSerializerContext.Default.ModifierDtoArray)
        );
    }
}

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(ModifierDto[]))]
internal partial class ModifierSerializerContext : JsonSerializerContext;

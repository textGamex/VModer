using System.Globalization;
using System.Text;
using AngleSharp;
using AngleSharp.Html.Parser;
using CsvHelper;

// 根据P社的文档来生成 Modifiers.csv 和 DynamicModifiers.csv 文件, 用作修饰符查询器的数据源

var context = BrowsingContext.New(Configuration.Default);
var parser = context.GetService<IHtmlParser>() ?? new HtmlParser();

string fileText = File.ReadAllText(
    @"D:\SteamLibrary\steamapps\common\Hearts of Iron IV\documentation\modifiers_documentation.html"
);

var modifiers = new Dictionary<string, IEnumerable<string>>(1024);
var dynamicModifiers = new Dictionary<string, IEnumerable<string>>(16);

var document = parser.ParseDocument(fileText);
bool isReady = false;
foreach (var element in document.All)
{
    bool addToDynamicModifiers = false;
    if (element.Id == "modifiers-for-scope-aggressive")
    {
        isReady = true;
    }

    if (isReady && element is { TagName: "h2", Id: "_drift_from_guarantees" })
    {
        break;
    }

    if (isReady && element.TagName.Equals("li", StringComparison.OrdinalIgnoreCase))
    {
        // 有些 <li> 没有子元素, 跳过
        if (element.Children.Length == 0)
        {
            continue;
        }

        var link = element.Children[0];
        string? query = link.GetAttribute("href");
        if (string.IsNullOrWhiteSpace(query))
        {
            continue;
        }

        string innerHtml = link.InnerHtml;
        if (innerHtml.Contains('<'))
        {
            addToDynamicModifiers = true;
        }
        
        string modifierName = addToDynamicModifiers 
            ? System.Text.RegularExpressions.Regex.Replace(innerHtml, @"</[^>]+>", "") 
            : link.TextContent;

        var modifier = document.QuerySelector(query);
        var ulElement = modifier?.NextElementSibling;
        
        if (ulElement is null || !ulElement.TagName.Equals("UL", StringComparison.OrdinalIgnoreCase))
        {
            // 如果没有找到下一个元素, 可能是一个动态生成的修饰符, 嵌套在在一个 <h2> 中
            var parentNext = modifier?.ParentElement?.NextElementSibling;
            if (parentNext != null && parentNext.TagName.Equals("UL", StringComparison.OrdinalIgnoreCase))
            {
                ulElement = parentNext;
            }
        }

        if (ulElement is null)
        {
            continue;
        }

        var categoriesElement = ulElement.Children.FirstOrDefault(item => item.TextContent.Contains("Categories"));
        if (categoriesElement == null)
        {
            continue;
        }
        
        var categoriesParts = categoriesElement.TextContent.Split(':');
        if (categoriesParts.Length < 2)
        {
            continue;
        }

        string categories = categoriesParts[1];

        if (addToDynamicModifiers)
        {
            dynamicModifiers[modifierName] = categories.Split(',', StringSplitOptions.TrimEntries);
        }
        else
        {
            modifiers[modifierName] = categories.Split(',', StringSplitOptions.TrimEntries);
        }
    }
}

using var modifierCsv = new CsvWriter(
    new StreamWriter(File.Open("Modifiers.csv", FileMode.Create), new UTF8Encoding(false)),
    CultureInfo.InvariantCulture
);
modifierCsv.WriteField("Name");
modifierCsv.WriteField("Categories");
modifierCsv.NextRecord();

foreach (var element in modifiers)
{
    modifierCsv.WriteField(element.Key);
    modifierCsv.WriteField(string.Join(';', element.Value));
    modifierCsv.NextRecord();
}

using var dynamicModifierCsv = new CsvWriter(
    new StreamWriter(File.Open("DynamicModifiers.csv", FileMode.Create), new UTF8Encoding(false)),
    CultureInfo.InvariantCulture
);

dynamicModifierCsv.WriteField("Name");
dynamicModifierCsv.WriteField("Categories");
dynamicModifierCsv.NextRecord();

foreach (var element in dynamicModifiers)
{
    dynamicModifierCsv.WriteField(element.Key);
    dynamicModifierCsv.WriteField(string.Join(';', element.Value));
    dynamicModifierCsv.NextRecord();
}

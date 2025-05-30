using System.Text;

namespace VModer.Core.Extensions;

public static class StringExtensions
{
    public static string ToFilePath(this string filePath)
    {
        var builder = new StringBuilder("file:///", filePath.Length + 16);
        foreach (char c in filePath)
        {
            switch (c)
            {
                case '\\':
                    builder.Append('/');
                    break;
                case ' ':
                    builder.Append("%20");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }

    public static bool EqualsIgnoreCase(this string left, string value)
    {
        return left.Equals(value, StringComparison.OrdinalIgnoreCase);
    }
}

using System.Text;
using System.Text.RegularExpressions;

namespace Files.Api.Migrate.Input;

/// <summary>Renders a source's <c>reference</c>: a template over <c>{name}</c> placeholders — a key column or
/// <c>{path}</c>, and <c>{sha256}</c>, the lowercase hex of the file's bytes.</summary>
/// <remarks>Placeholders match case-sensitively, so a key column <c>Sha256</c> and the hash <c>{sha256}</c> stay apart;
/// a key column literally named <c>sha256</c> wins over the hash, since the product's cutover reads its own column.</remarks>
public sealed partial class ReferenceTemplate
{
    /// <summary>Holds the placeholder every kind offers: the SHA-256 of the file's bytes.</summary>
    public const string Sha256Placeholder = "sha256";

    private readonly string template;

    private ReferenceTemplate(string template, IReadOnlyList<string> placeholders)
    {
        this.template = template;
        Placeholders = placeholders;
    }

    /// <summary>Gets the placeholder names the template uses, in order of appearance.</summary>
    public IReadOnlyList<string> Placeholders { get; }

    /// <summary>Parses a template and checks every placeholder against the names its source offers.</summary>
    /// <param name="template">The template as the manifest writes it.</param>
    /// <param name="allowed">The names the source offers besides <c>{sha256}</c>.</param>
    /// <exception cref="MigrateInputException">The template uses a placeholder the source does not offer.</exception>
    public static ReferenceTemplate Parse(string template, IReadOnlyCollection<string> allowed)
    {
        var placeholders = new List<string>();
        foreach (Match match in PlaceholderPattern().Matches(template))
        {
            var name = match.Groups[1].Value;
            if (name != Sha256Placeholder && !allowed.Contains(name))
                throw new MigrateInputException($"The reference uses the placeholder '{{{name}}}', which the source does not offer.");
            placeholders.Add(name);
        }

        return new ReferenceTemplate(template, placeholders);
    }

    /// <summary>Renders the reference of one file.</summary>
    /// <param name="values">The item's placeholder values (key columns or <c>path</c>).</param>
    /// <param name="sha256">The lowercase hex SHA-256 of the item's bytes.</param>
    public string Render(IReadOnlyDictionary<string, string> values, string sha256)
    {
        var rendered = new StringBuilder(template.Length + 64);
        var last = 0;
        foreach (Match match in PlaceholderPattern().Matches(template))
        {
            rendered.Append(template, last, match.Index - last);
            var name = match.Groups[1].Value;
            rendered.Append(values.TryGetValue(name, out var value) ? value : name == Sha256Placeholder ? sha256 : match.Value);
            last = match.Index + match.Length;
        }

        return rendered.Append(template, last, template.Length - last).ToString();
    }

    /// <inheritdoc />
    public override string ToString() => template;

    [GeneratedRegex(@"\{([A-Za-z_][A-Za-z0-9_]*)\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();
}

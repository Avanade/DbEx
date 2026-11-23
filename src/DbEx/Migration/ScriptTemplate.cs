using YamlDotNet.Core;

namespace DbEx.Migration;

/// <summary>
/// A provider-specific Script template shared by discovery, help and export.
/// </summary>
internal sealed class ScriptTemplate
{
    /// <summary>
    /// Reads the template and its metadata.
    /// </summary>
    private ScriptTemplate(string name, string source, string content)
    {
        Name = name;
        Source = source;
        Content = content;
        Command = $"script {name.ToLowerInvariant()}";

        foreach (var line in content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None))
        {
            var text = line.Trim().TrimStart('\uFEFF');
            if (!text.StartsWith("{{!", StringComparison.Ordinal) || !text.EndsWith("}}", StringComparison.Ordinal))
                continue;

            var metadata = text[3..^2].Trim();
            var colon = metadata.IndexOf(':');
            if (colon < 0)
                continue;

            var key = metadata[..colon].Trim();
            var value = metadata[(colon + 1)..].Trim();
            switch (key.ToUpperInvariant())
            {
                case "COMMAND":
                    if (!Regex.IsMatch(value, $@"^script\s+(?:{Regex.Escape(name)}{(name.Equals("Default", StringComparison.OrdinalIgnoreCase) ? @"|\[default\]" : string.Empty)})(?:\s|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                        throw new CodeGenException($"Script template '{source}' has a COMMAND that does not match script '{name}'.");

                    Command = value;
                    break;

                case "HELP":
                    Help = value;
                    break;

                case "FILENAME":
                    FileName = value;
                    break;

                case "PARAM":
                    if (value.Length == 0)
                        break;

                    var equals = value.IndexOf('=');
                    var parameter = (equals < 0 ? value : value[..equals]).Trim();
                    if (parameter.Length == 0)
                        throw new CodeGenException($"Script template '{source}' has an empty PARAM name.");

                    var defaultValue = equals < 0 ? null : value[(equals + 1)..].Trim();
                    Defaults.TryAdd(parameter, string.IsNullOrEmpty(defaultValue) ? null : defaultValue);
                    break;
            }
        }
    }

    /// <summary>
    /// Gets the resource-derived script name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the source path or manifest resource name.
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// Gets the unrendered template.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets the displayed command.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Gets the displayed description, if provided.
    /// </summary>
    public string? Help { get; }

    /// <summary>
    /// Gets the filename template.
    /// </summary>
    public string FileName { get; } = "new-script";

    /// <summary>
    /// Gets the literal parameter defaults.
    /// </summary>
    public Dictionary<string, string?> Defaults { get; } = [];

    /// <summary>
    /// Discovers effective templates without constructing a migrator or accessing a database.
    /// </summary>
    /// <param name="suffix">The provider script suffix.</param>
    /// <param name="args">The configured resource assemblies.</param>
    /// <param name="providerAssembly">The provider's built-in resource assembly.</param>
    /// <param name="fallbackAssemblies">Migrator or provider assemblies used as fallbacks.</param>
    /// <returns>The effective templates in help order.</returns>
    public static IReadOnlyList<ScriptTemplate> Discover(string suffix, MigrationArgsBase args, Assembly providerAssembly, IEnumerable<Assembly> fallbackAssemblies)
    {
        var pattern = new Regex($@"^Script(?<name>[^.\\/]+)_{Regex.Escape(suffix)}(?:\.(?<extension>hb|hbs))?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var templates = new Dictionary<string, ScriptTemplate>(StringComparer.OrdinalIgnoreCase);

        void AddSource(IEnumerable<(string FileName, string Source, Func<string> Read)> candidates)
        {
            var matches = candidates.Select(x => (Candidate: x, Match: pattern.Match(x.FileName))).Where(x => x.Match.Success);
            foreach (var group in matches.GroupBy(x => x.Match.Groups["name"].Value, StringComparer.OrdinalIgnoreCase))
            {
                if (templates.ContainsKey(group.Key))
                    continue;

                var ordered = group.OrderBy(x => x.Match.Groups["extension"].Value.ToLowerInvariant() switch { "" => 0, "hb" => 1, _ => 2 }).ToArray();
                if (ordered.Length > 1 && ordered[0].Match.Groups["extension"].Value.Equals(ordered[1].Match.Groups["extension"].Value, StringComparison.OrdinalIgnoreCase))
                    throw new CodeGenException($"Script '{group.Key}' is ambiguous between '{ordered[0].Candidate.Source}' and '{ordered[1].Candidate.Source}'.");

                var candidate = ordered[0].Candidate;
                try
                {
                    templates.Add(group.Key, new ScriptTemplate(group.Key, candidate.Source, candidate.Read()));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    throw new CodeGenException($"Cannot read Script template '{candidate.Source}': {ex.Message}");
                }
            }
        }

        foreach (var directory in new[] { Environment.CurrentDirectory, Path.Combine(Environment.CurrentDirectory, "Resources") })
        {
            if (Directory.Exists(directory))
                AddSource(Directory.EnumerateFiles(directory).Select(path => (Path.GetFileName(path), path, (Func<string>)(() => File.ReadAllText(path)))));
        }

        foreach (var assembly in args.ProbeAssemblies.Select(x => x.Assembly).Where(x => x != providerAssembly)
            .Concat(fallbackAssemblies).Append(providerAssembly).Distinct())
        {
            var candidates = new List<(string, string, Func<string>)>();
            foreach (var resource in assembly.GetManifestResourceNames())
            {
                var index = resource.LastIndexOf(".Resources.", StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                    continue;

                var source = $"{assembly.GetName().Name}:{resource}";
                candidates.Add((resource[(index + 11)..], source, () =>
                {
                    using var reader = new StreamReader(assembly.GetManifestResourceStream(resource) ?? throw new CodeGenException($"Cannot read Script template '{source}'."));
                    return reader.ReadToEnd();
                }));
            }

            AddSource(candidates);
        }

        return templates.Values.OrderBy(x => x.Name.Equals("Default", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// Copies explicit inputs and fills omitted parameters from configuration or literal defaults.
    /// </summary>
    /// <param name="parameters">The explicit caller inputs.</param>
    /// <param name="outputDirectory">The directory containing dbex.yaml, when present.</param>
    /// <param name="schemaFromConfiguration">Indicates that a default was resolved from dbex.yaml.</param>
    /// <returns>A new parameter dictionary containing the resolved defaults.</returns>
    public Dictionary<string, string?> ResolveParameters(IDictionary<string, string?>? parameters, DirectoryInfo outputDirectory, out bool schemaFromConfiguration)
    {
        var resolved = parameters == null ? new Dictionary<string, string?>() : new Dictionary<string, string?>(parameters);
        schemaFromConfiguration = false;
        string? schema = null;
        foreach (var parameter in Defaults)
        {
            if (resolved.ContainsKey(parameter.Key))
                continue;

            var value = parameter.Value;
            if (value == "Schema")
            {
                schema ??= ReadSchemaDefault(outputDirectory, out schemaFromConfiguration);
                value = schema;
            }

            resolved.Add(parameter.Key, value);
        }

        return resolved;
    }

    /// <summary>
    /// Reads only the generic configuration data needed for an omitted Schema default.
    /// </summary>
    private static string ReadSchemaDefault(DirectoryInfo outputDirectory, out bool fromConfiguration)
    {
        fromConfiguration = false;
        var path = Path.Combine(outputDirectory.FullName, "dbex.yaml");
        try
        {
            using var reader = File.OpenText(path);
            var parser = new YamlDotNet.Core.Parser(reader);
            parser.Consume<StreamStart>();
            var config = new DeserializerBuilder().WithDuplicateKeyChecking().WithAttemptingUnquotedStringTypeDeserialization().Build().Deserialize<Dictionary<string, object?>>(parser);
            parser.Consume<YamlDotNet.Core.Events.StreamEnd>();
            if (config == null)
                throw new CodeGenException($"Script configuration '{path}' must contain a YAML mapping.");

            if (!config.TryGetValue("schema", out var value))
                return "Schema";

            if (value is not string text || string.IsNullOrWhiteSpace(text))
                throw new CodeGenException($"Script configuration '{path}' must have a non-blank string 'schema' value.");

            fromConfiguration = true;
            return text;
        }
        catch (FileNotFoundException)
        {
            return "Schema";
        }
        catch (DirectoryNotFoundException)
        {
            return "Schema";
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new CodeGenException($"Script configuration '{path}' is invalid: {ex.Message}");
        }
        catch (IOException ex)
        {
            throw new CodeGenException($"Cannot read Script configuration '{path}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new CodeGenException($"Cannot read Script configuration '{path}': {ex.Message}");
        }
    }
}

#nullable enable
namespace XamlColorSchemeGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    // This class has to be kept in sync with https://github.com/ControlzEx/ControlzEx/blob/develop/src/ControlzEx/Theming/ThemeGenerator.cs
    public class ColorSchemeGenerator
    {
        private const int BufferSize = 32768; // 32 Kilobytes

        public void GenerateColorSchemeFiles(string generatorParametersFile, string templateFile, string? outputPath = null)
        {
            var parameters = GetParametersFromFile(generatorParametersFile);

            outputPath ??= Path.GetDirectoryName(Path.GetFullPath(templateFile));

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new Exception("OutputPath could not be determined.");
            }

            Directory.CreateDirectory(outputPath);

            var templateContent = File.ReadAllText(templateFile, Encoding.UTF8);

            var colorSchemesWithoutVariantName = parameters.ColorSchemes
                                                        .Where(x => string.IsNullOrEmpty(x.ColorSchemeVariantName))
                                                        .ToList();
            var colorSchemesWithVariantName = parameters.ColorSchemes
                                                        .Where(x => string.IsNullOrEmpty(x.ColorSchemeVariantName) == false)
                                                        .ToList();

            foreach (var baseColorScheme in parameters.BaseColorSchemes)
            {
                foreach (var colorScheme in colorSchemesWithoutVariantName)
                {
                    var themeName = $"{baseColorScheme.Name}.{colorScheme.Name}";
                    var colorSchemeName = colorScheme.Name;
                    var alternativeColorSchemeName = colorScheme.Name;
                    var themeDisplayName = $"{colorSchemeName} ({baseColorScheme.Name})";

                    this.GenerateColorSchemeFile(outputPath, templateContent, themeName, themeDisplayName, baseColorScheme.Name, colorSchemeName, alternativeColorSchemeName, colorScheme.Values, baseColorScheme.Values, parameters.DefaultValues);
                }

                foreach (var colorSchemeVariant in parameters.AdditionalColorSchemeVariants)
                {
                    foreach (var colorScheme in colorSchemesWithoutVariantName.Concat(colorSchemesWithVariantName))
                    {
                        var themeName = $"{baseColorScheme.Name}.{colorScheme.Name}.{colorSchemeVariant.Name}";
                        var colorSchemeName = $"{colorScheme.Name}.{colorSchemeVariant.Name}";
                        var alternativeColorSchemeName = colorScheme.Name;
                        var themeDisplayName = $"{colorSchemeName} ({baseColorScheme.Name})";

                        this.GenerateColorSchemeFile(outputPath, templateContent, themeName, themeDisplayName, baseColorScheme.Name, colorSchemeName, alternativeColorSchemeName, colorScheme.Values, colorSchemeVariant.Values, baseColorScheme.Values, parameters.DefaultValues);
                    }
                }
            }
        }

        public static ThemeGeneratorParameters GetParametersFromFile(string inputFile)
        {
            return GetParametersFromString(ReadAllTextShared(inputFile));
        }

        private static string ReadAllTextShared(string file)
        {
            Stream? stream = null;
            try
            {
                stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);

                using (var textReader = new StreamReader(stream, Encoding.UTF8))
                {
                    stream = null;
                    return textReader.ReadToEnd();
                }
            }
            finally
            {
                stream?.Dispose();
            }
        }

        public void GenerateColorSchemeFile(string templateDirectory, string templateContent, string themeName, string themeDisplayName, string baseColorScheme, string colorScheme, string alternativeColorScheme, params Dictionary<string, string>[] valueSources)
        {
            var themeFilename = $"{themeName}.xaml";

            var themeFile = Path.Combine(templateDirectory, themeFilename);

            var themeTempFileContent = GenerateColorSchemeFileContent(templateContent, themeName, themeDisplayName, baseColorScheme, colorScheme, alternativeColorScheme, valueSources);

            Trace.WriteLine($"Checking \"{themeFile}\"...");

            var fileHasToBeWritten = File.Exists(themeFile) == false
                                     || ReadAllTextShared(themeFile) != themeTempFileContent;

            if (fileHasToBeWritten)
            {
                using (var sw = new StreamWriter(themeFile, false, Encoding.UTF8, BufferSize))
                {
                    sw.Write(themeTempFileContent);
                }

                Trace.WriteLine($"Resource Dictionary saved to \"{themeFile}\".");
            }
            else
            {
                Trace.WriteLine("New Resource Dictionary did not differ from existing file. No new file written.");
            }
        }

        public static ThemeGeneratorParameters GetParametersFromString(string input)
        {
#if NETCOREAPP
            return System.Text.Json.JsonSerializer.Deserialize<ThemeGeneratorParameters>(input);
#else
            return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<ThemeGeneratorParameters>(input);
#endif
        }

        // The order of the passed valueSources is important.
        // More specialized/concrete values must be passed first and more generic ones must follow.
        public static string GenerateColorSchemeFileContent(string templateContent, string themeName, string themeDisplayName, string baseColorScheme, string colorScheme, string alternativeColorScheme, params Dictionary<string, string>[] valueSources)
        {
            templateContent = templateContent.Replace("{{ThemeName}}", themeName);
            templateContent = templateContent.Replace("{{ThemeDisplayName}}", themeDisplayName);
            templateContent = templateContent.Replace("{{BaseColorScheme}}", baseColorScheme);
            templateContent = templateContent.Replace("{{ColorScheme}}", colorScheme);
            templateContent = templateContent.Replace("{{AlternativeColorScheme}}", alternativeColorScheme);

            foreach (var valueSource in valueSources)
            {
                foreach (var value in valueSource)
                {
                    templateContent = templateContent.Replace($"{{{{{value.Key}}}}}", value.Value);   
                }
            }

            return templateContent;
        }
    }

    public class ThemeGeneratorParameters
    {
        public Dictionary<string, string> DefaultValues { get; set; } = new Dictionary<string, string>();

        public ThemeGeneratorBaseColorScheme[] BaseColorSchemes { get; set; } = new ThemeGeneratorBaseColorScheme[0];

        public ThemeGeneratorColorScheme[] ColorSchemes { get; set; } = new ThemeGeneratorColorScheme[0];

        public AdditionalColorSchemeVariant[] AdditionalColorSchemeVariants { get; set; } = new AdditionalColorSchemeVariant[0];
    }

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class ThemeGeneratorBaseColorScheme
    {
        public string Name { get; set; } = string.Empty;

        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class AdditionalColorSchemeVariant
    {
        public string Name { get; set; } = string.Empty;

        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class ThemeGeneratorColorScheme
    {
        public string Name { get; set; } = string.Empty;

        public string ColorSchemeVariantName { get; set; } = string.Empty;

        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }
}
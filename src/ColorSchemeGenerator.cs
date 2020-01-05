namespace XamlColorSchemeGenerator
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class ColorSchemeGenerator
    {
        private const int BufferSize = 32768; // 32 Kilobytes

        public void GenerateColorSchemeFiles(string generatorParametersFile, string templateFile, string outputPath = null)
        {
            var parameters = GetParametersFromFile(generatorParametersFile);

            outputPath ??= Path.GetDirectoryName(Path.GetFullPath(templateFile));

            Directory.CreateDirectory(outputPath);

            var templateContent = File.ReadAllText(templateFile, Encoding.UTF8);

            var colorSchemesForBaseColors = parameters.ColorSchemes.Where(x => string.IsNullOrEmpty(x.CustomBaseColorSchemeName))
                                                      .ToList();
            var colorSchemesWithCustomBaseColor = parameters.ColorSchemes.Where(x => string.IsNullOrEmpty(x.CustomBaseColorSchemeName) == false);

            foreach (var baseColorScheme in parameters.BaseColorSchemes)
            {
                foreach (var colorScheme in colorSchemesForBaseColors)
                {
                    this.GenerateColorSchemeFile(parameters, outputPath, templateContent, baseColorScheme, colorScheme);
                }
            }

            foreach (var colorScheme in colorSchemesWithCustomBaseColor)
            {
                var baseColorScheme = string.IsNullOrEmpty(colorScheme.BaseColorSchemeReference)
                                                      ? new ThemeGeneratorBaseColorScheme()
                                                      : parameters.BaseColorSchemes.First(x => x.Name == colorScheme.BaseColorSchemeReference).Clone();

                baseColorScheme.Name = colorScheme.CustomBaseColorSchemeName;

                this.GenerateColorSchemeFile(parameters, outputPath, templateContent, baseColorScheme, colorScheme);
            }
        }

        public static ThemeGeneratorParameters GetParametersFromFile(string inputFile)
        {
            return GetParametersFromString(ReadAllTextShared(inputFile));
        }

        public static ThemeGeneratorParameters GetParametersFromString(string input)
        {
#if NETCOREAPP3_0
            return System.Text.Json.JsonSerializer.Deserialize<ThemeGeneratorParameters>(input);
#else
            return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<ThemeGeneratorParameters>(input);
#endif
        }

        public void GenerateColorSchemeFile(ThemeGeneratorParameters parameters, string templateDirectory, string templateContent, ThemeGeneratorBaseColorScheme baseColorScheme, ThemeGeneratorColorScheme colorScheme)
        {
            var themeName = $"{baseColorScheme.Name}.{colorScheme.Name}";
            var themeDisplayName = $"{colorScheme.Name} ({baseColorScheme.Name})";
            var themeFilename = $"{themeName}.xaml";

            var themeFile = Path.Combine(templateDirectory, themeFilename);

            var themeTempFileContent = this.GenerateColorSchemeFileContent(parameters, baseColorScheme, colorScheme, templateContent, themeName, themeDisplayName);

            //Trace.WriteLine($"Comparing temp file \"{themeTempFile}\" to \"{themeFile}\"");

            var fileHasToBeWritten = File.Exists(themeFile) == false
                                     || ReadAllTextShared(themeFile) != themeTempFileContent;

            if (fileHasToBeWritten)
            {
                using (var sw = new StreamWriter(themeFile, false, Encoding.UTF8, BufferSize))
                {
                    sw.Write(themeTempFileContent);
                }

                //Trace.WriteLine($"Resource Dictionary saved to \"{themeFile}\".");
            }
            else
            {
                //Trace.WriteLine("New Resource Dictionary did not differ from existing file. No new file written.");
            }
        }

        public string GenerateColorSchemeFileContent(ThemeGeneratorParameters parameters, ThemeGeneratorBaseColorScheme baseColorScheme, ThemeGeneratorColorScheme colorScheme, string templateContent, string themeName, string themeDisplayName)
        {
            templateContent = templateContent.Replace("{{ThemeName}}", themeName);
            templateContent = templateContent.Replace("{{ThemeDisplayName}}", themeDisplayName);
            templateContent = templateContent.Replace("{{BaseColorScheme}}", baseColorScheme.Name);
            templateContent = templateContent.Replace("{{ColorScheme}}", colorScheme.Name);

            foreach (var value in colorScheme.Values)
            {
                templateContent = templateContent.Replace($"{{{{{value.Key}}}}}", value.Value);
            }

            foreach (var value in baseColorScheme.Values)
            {
                templateContent = templateContent.Replace($"{{{{{value.Key}}}}}", value.Value);
            }

            foreach (var value in parameters.DefaultValues)
            {
                templateContent = templateContent.Replace($"{{{{{value.Key}}}}}", value.Value);
            }

            return templateContent;
        }

        private static string ReadAllTextShared(string file)
        {
            Stream stream = null;
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
    }

    public class ThemeGeneratorParameters
    {
        public Dictionary<string, string> DefaultValues { get; set; } = new Dictionary<string, string>();

        public ThemeGeneratorBaseColorScheme[] BaseColorSchemes { get; set; }

        public ThemeGeneratorColorScheme[] ColorSchemes { get; set; }
    }

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class ThemeGeneratorBaseColorScheme
    {
        public string Name { get; set; }

        public Dictionary<string, string> Values { get; set; }

        public ThemeGeneratorBaseColorScheme Clone()
        {
            return (ThemeGeneratorBaseColorScheme)this.MemberwiseClone();
        }
    }

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class ThemeGeneratorColorScheme
    {
        public string CustomBaseColorSchemeName { get; set; }

        public string BaseColorSchemeReference { get; set; }

        public string Name { get; set; }        

        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }
}
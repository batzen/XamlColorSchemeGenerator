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

        public void GenerateColorSchemeFiles(string inputFile)
        {
            var parameters = GetParametersFromFile(inputFile);

            var templateDirectory = Path.GetDirectoryName(Path.GetFullPath(inputFile));
            var templateFile = Path.Combine(templateDirectory, parameters.TemplateFile);
            var templateContent = File.ReadAllText(templateFile, Encoding.UTF8);

            var colorSchemesForBaseColors = parameters.ColorSchemes.Where(x => string.IsNullOrEmpty(x.CustomBaseColorSchemeName))
                                                      .ToList();
            var colorSchemesWithCustomBaseColor = parameters.ColorSchemes.Where(x => string.IsNullOrEmpty(x.CustomBaseColorSchemeName) == false);

            foreach (var baseColorScheme in parameters.BaseColorSchemes)
            {
                foreach (var colorScheme in colorSchemesForBaseColors)
                {
                    this.GenerateColorSchemeFile(parameters, templateDirectory, templateContent, baseColorScheme, colorScheme);
                }
            }

            foreach (var colorScheme in colorSchemesWithCustomBaseColor)
            {
                var baseColorScheme = string.IsNullOrEmpty(colorScheme.BaseColorSchemeReference)
                                                      ? new BaseColorScheme()
                                                      : parameters.BaseColorSchemes.First(x => x.Name == colorScheme.BaseColorSchemeReference).Clone();

                baseColorScheme.Name = colorScheme.CustomBaseColorSchemeName;

                this.GenerateColorSchemeFile(parameters, templateDirectory, templateContent, baseColorScheme, colorScheme);
            }
        }

        public static GeneratorParameters GetParametersFromFile(string inputFile)
        {
            return GetParametersFromString(ReadAllTextShared(inputFile));
        }

        public static GeneratorParameters GetParametersFromString(string input)
        {
#if NETCOREAPP3_0
            return System.Text.Json.Serialization.JsonSerializer.Parse<GeneratorParameters>(input);
#else
            return new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<GeneratorParameters>(input);
#endif
        }

        public void GenerateColorSchemeFile(GeneratorParameters parameters, string templateDirectory, string templateContent, BaseColorScheme baseColorScheme, ColorScheme colorScheme)
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

        public string GenerateColorSchemeFileContent(GeneratorParameters parameters, BaseColorScheme baseColorScheme, ColorScheme colorScheme, string templateContent, string themeName, string themeDisplayName)
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

    public class GeneratorParameters
    {
        public string TemplateFile { get; set; }

        public Dictionary<string, string> DefaultValues { get; set; } = new Dictionary<string, string>();

        public BaseColorScheme[] BaseColorSchemes { get; set; }

        public ColorScheme[] ColorSchemes { get; set; }
    }

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class BaseColorScheme
    {
        public string Name { get; set; }

        public Dictionary<string, string> Values { get; set; }

        public BaseColorScheme Clone()
        {
            return (BaseColorScheme)this.MemberwiseClone();
        }
    }

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class ColorScheme
    {
        public string CustomBaseColorSchemeName { get; set; }

        public string BaseColorSchemeReference { get; set; }

        public string Name { get; set; }        

        public Dictionary<string, string> Values { get; set; } = new Dictionary<string, string>();
    }
}
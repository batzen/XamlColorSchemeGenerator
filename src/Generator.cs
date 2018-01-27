namespace XamlColorSchemeGenerator
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;

    public class Generator
    {
        public void Generate(string inputFile)
        {
            var parameters = JsonConvert.DeserializeObject<GeneratorParameters>(File.ReadAllText(inputFile, Encoding.UTF8));

            var templateDirectory = Path.GetDirectoryName(Path.GetFullPath(inputFile));
            var templateFile = Path.Combine(templateDirectory, parameters.TemplateFile);
            var templateContent = File.ReadAllText(templateFile, Encoding.UTF8);

            foreach (var colorScheme in parameters.ColorSchemes)
            {
                this.Generate(parameters, templateDirectory, templateContent, colorScheme);
            }
        }

        private void Generate(GeneratorParameters parameters, string templateDirectory, string templateContent, ColorScheme colorScheme)
        {
            var fileContent = templateContent;
            var colorSchemeFileName = $"{colorScheme.Name}.xaml";
            var colorSchemeTempFileName = $"{colorSchemeFileName}_{Guid.NewGuid()}.xaml";

            var colorSchemeFile = Path.Combine(templateDirectory, colorSchemeFileName);
            var colorSchemeTempFile = Path.Combine(Path.GetTempPath(), colorSchemeTempFileName);

            try
            {
                foreach (var colorSchemeValue in colorScheme.Values)
                {
                    fileContent = fileContent.Replace($"{{{{{colorSchemeValue.Key}}}}}", colorSchemeValue.Value);
                }

                foreach (var defaultValue in parameters.DefaultValues)
                {
                    fileContent = fileContent.Replace($"{{{{{defaultValue.Key}}}}}", defaultValue.Value);
                }

                File.WriteAllText(colorSchemeTempFile, fileContent, Encoding.UTF8);

                Trace.WriteLine($"Comparing temp file \"{colorSchemeTempFile}\" to \"{colorSchemeFile}\"");

                if (File.Exists(colorSchemeFile) == false
                    || File.ReadAllText(colorSchemeFile) != File.ReadAllText(colorSchemeTempFile))
                {
                    File.Copy(colorSchemeTempFile, colorSchemeFile, true);

                    Trace.WriteLine($"Resource Dictionary saved to \"{colorSchemeFile}\".");
                }
                else
                {
                    Trace.WriteLine("New Resource Dictionary did not differ from existing file. No new file written.");
                }
            }
            finally
            {
                if (File.Exists(colorSchemeTempFile))
                {
                    File.Delete(colorSchemeTempFile);
                }
            }
        }
    }
}
namespace XamlColorSchemeGenerator
{
    using System.Collections.Generic;

    public class GeneratorParameters
    {
        public string TemplateFile { get; set; }

        public Dictionary<string, string> DefaultValues { get; set; }

        public ColorScheme[] ColorSchemes { get; set; }
    }
}
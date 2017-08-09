namespace XamlColorSchemeGenerator
{
    using System.Collections.Generic;
    using System.Diagnostics;

    [DebuggerDisplay("{" + nameof(Name) + "}")]
    public class ColorScheme
    {
        public string Name { get; set; }

        public Dictionary<string, string> Values { get; set; }
    }
}
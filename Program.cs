namespace XamlColorSchemeGenerator
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;

    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();

                // TODO: Add flags for some parameters.
                if (args.Any())
                {
                    var generator = new Generator();
                    generator.Generate(args[0]);
                }

                // TODO: Add help output.

                stopwatch.Stop();
                Trace.WriteLine($"Generation time: {stopwatch.Elapsed}");

                if (Debugger.IsAttached)
                {
                    Console.ReadLine();
                }

                return 0;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                Console.WriteLine(e);
                return 1;
            }
        }
    }
}
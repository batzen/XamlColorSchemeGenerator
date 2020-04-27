namespace XamlColorSchemeGenerator
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime;
    using System.Threading;

    internal class Program
    {
        private static int Main(string[] args)
        {
            const string ProfileFile = "XamlColorSchemeGenerator.profile";

            ProfileOptimization.SetProfileRoot(Environment.GetEnvironmentVariable("temp"));
            ProfileOptimization.StartProfile(ProfileFile);

            var stopwatch = Stopwatch.StartNew();

            var nologo = args.Any(x => x.Equals("-nologo", StringComparison.OrdinalIgnoreCase));
            var verbose = args.Any(x => x.Equals("-v", StringComparison.OrdinalIgnoreCase));
            var indexForGeneratorParametersFile = Array.IndexOf(args, "-g") + 1;
            var indexForTemplateFile = Array.IndexOf(args, "-t") + 1;
            var indexForOutputPath = Array.IndexOf(args, "-o") + 1;

            try
            {
                if (nologo == false)
                {
                    var attribute = (AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(typeof(Program).Assembly, typeof(AssemblyFileVersionAttribute));
                    var version = attribute.Version;
                    Console.WriteLine($"XamlColorSchemeGenerator - {version}");
                }

                if (verbose)
                {
                    Console.WriteLine("Starting file generation with args:");
                    Console.WriteLine(string.Join(" ", args));
                }

                var generatorParametersFile = indexForGeneratorParametersFile > 0 && args.Length >= indexForGeneratorParametersFile
                    ? args[indexForGeneratorParametersFile] 
                    : "GeneratorParameters.json";
                var templateFile = indexForTemplateFile > 0 && args.Length >= indexForTemplateFile
                    ? args[indexForTemplateFile] 
                    : "Theme.Template.xaml";
                var outputPath = indexForOutputPath > 0 && args.Length >= indexForOutputPath
                    ? args[indexForOutputPath]  
                    : null;

                using (var mutex = Lock(generatorParametersFile))
                {
                    try
                    {
                        var generator = new ColorSchemeGenerator();

                        generator.GenerateColorSchemeFiles(generatorParametersFile, templateFile, outputPath);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                        stopwatch.Stop();
                    }
                }

                // TODO: Add help output.

                if (verbose)
                {
                    Console.WriteLine($"Generation time: {stopwatch.Elapsed}");
                }

                return 0;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                Console.WriteLine(e);

                if (Debugger.IsAttached)
                {
                    Console.ReadLine();
                }

                return 1;
            }
        }

        private static Mutex Lock(string file)
        {           
            var mutexName = "Local\\XamlColorSchemeGenerator_" + Path.GetFileName(file);

            var mutex = new Mutex(false, mutexName);

            if (mutex.WaitOne(TimeSpan.FromSeconds(10)) == false)
            {
                throw new TimeoutException("Another instance of this application blocked the concurrent execution.");
            }
            
            return mutex;
        }
    }
}
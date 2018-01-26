namespace XamlColorSchemeGenerator
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

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
                    var inputFile = args[0];

                    using (Lock(inputFile))
                    {
                        var generator = new Generator();

                        generator.Generate(inputFile);
                    }
                }
                else
                {
                    Console.WriteLine("You have pass the generator input file as a commandline parameter.");
                    return 1;
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

        private static IDisposable Lock(string file)
        {
            var appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value;
            var mutexName = $"Local\\{appGuid}_{GetMD5Hash(file)}";

            var mutex = new Mutex(false, mutexName);

            if (mutex.WaitOne(TimeSpan.FromSeconds(10)) == false)
            {
                throw new TimeoutException("Another instance of this application blocked the concurrent execution.");
            }

            return mutex;
        }

        private static string GetMD5Hash(string textToHash)
        {
            if (string.IsNullOrEmpty(textToHash))
            {
                return string.Empty;
            }

            var md5 = new MD5CryptoServiceProvider();
            var bytesToHash = Encoding.Default.GetBytes(textToHash);
            var result = md5.ComputeHash(bytesToHash); 

            return BitConverter.ToString(result); 
        } 
    }
}
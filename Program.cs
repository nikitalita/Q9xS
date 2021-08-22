using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace Q9xS
{
    class Options
    {
        [Value(0, Required = true, MetaName = "updatePath", HelpText = "Path to updates directory.")]
        public string updatePath { get; set; }

        [Value(1, Required = true, MetaName = "isoPath", HelpText = "Path to Windows 9x/ME installation CD ISO.")]
        public string isoPath { get; set; }

        [Option('o', "output",
          HelpText = "Path of output CD-ROM")]
        public string outputPath { get; set; }

        [Option('w', "working-dir",
          HelpText = "Specifies path of working directory for updating. Otherwise uses a temporary directory")]
        public string workingDir { get; set; }

        [Option("no-clean",
          Default = false,
          HelpText = "Path of output CD-ROM")]
        public bool noClean { get; set; }
    }

    class Program
    {
        static Options options;
        static void Main(string[] args)
        {
            bool extract = true;
            var result = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => options = o)
                .WithNotParsed(errs =>
                {
                    Console.WriteLine(Parser.Default.Settings.HelpWriter.ToString());
                    Environment.Exit(-1);
                });

            if (string.IsNullOrWhiteSpace(options.workingDir))
            {
                options.workingDir = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
            }
            else
            {
                options.workingDir = Path.Join(options.workingDir, "build");
            }
            try
            {
                string extractedIsoPath = Path.Join(
                    options.workingDir,
                    Path.GetFileNameWithoutExtension(options.isoPath)
                );

                if (!Directory.Exists(options.updatePath))
                    throw new FileNotFoundException(options.updatePath + " does not exist");

                if (!File.Exists(options.isoPath))
                    throw new FileNotFoundException(options.isoPath + " does not exist");

                if (Directory.Exists(extractedIsoPath))
                {
                    Console.WriteLine(@"It looks like you've already extracted the iso. Would you like to re-extract? [y/any other key]");
                    string response = Console.ReadLine();

                    if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                        extract = false;
                }
                Q9xS.ExtractBootImage(options.isoPath, options.workingDir);
                if (extract)
                    Q9xS.ExtractISO(options.isoPath, extractedIsoPath);

                Q9xS.Update9xDir(options.updatePath, extractedIsoPath);
                Q9xS.CreateISO(extractedIsoPath, options.outputPath);

            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                if (!options.noClean && Directory.Exists(options.workingDir))
                {
                    Console.WriteLine("Cleaning working directory " + options.workingDir);
                    Directory.Delete(options.workingDir, true);
                }
                    
            }
        }
    }
}

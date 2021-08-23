using System;
using System.IO;
using CommandLine;

namespace Q9xS
{
    class Options
    {
        [Value(0, Required = true, MetaName = "updatePath", HelpText = "Path to updates directory.")]
        public string UpdatePath { get; set; }

        [Value(1, Required = true, MetaName = "isoPath", HelpText = "Path to Windows 9x/ME installation CD ISO.")]
        public string IsoPath { get; set; }

        [Option('o', "output",
          HelpText = "Path of output CD-ROM")]
        public string OutputPath { get; set; }

        [Option('w', "working-dir",
          HelpText = "Specifies path of working directory for updating. Otherwise uses a temporary directory")]
        public string WorkingDir { get; set; }

        [Option("no-clean",
          Default = false,
          HelpText = "Path of output CD-ROM")]
        public bool NoClean { get; set; }
        
        [Option('f', "force",
          Default = false,
          HelpText = "If working build dir exists, overwrite without confirmation.")]
        public bool Force { get; set; }
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

            if (string.IsNullOrWhiteSpace(options.WorkingDir))
            {
                options.WorkingDir = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
            }
            else
            {
                // add "Q9xS-build" to a user specified dir, so we don't wipe out anything while cleaning
                options.WorkingDir = Path.Join(options.WorkingDir, "Q9xS-build");
            }
            try
            {
                string extractedIsoPath = Path.Join(
                    options.WorkingDir,
                    Path.GetFileNameWithoutExtension(options.IsoPath)
                );

                if (!Directory.Exists(options.UpdatePath))
                    throw new FileNotFoundException(options.UpdatePath + " does not exist");

                if (!File.Exists(options.IsoPath))
                    throw new FileNotFoundException(options.IsoPath + " does not exist");

                if (Directory.Exists(extractedIsoPath))
                {
                    if (!options.Force)
                    {
                        Console.WriteLine(@"It looks like you've already extracted the iso. Would you like to re-extract? [y/any other key]");
                        string response = Console.ReadLine();

                        if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                            extract = false;
                    }
                    if (extract)
                    {
                        Directory.Delete(extractedIsoPath, true);
                    }
                }
                Q9xS.ExtractBootImage(options.IsoPath, options.WorkingDir);

                if (extract)
                    Q9xS.ExtractISO(options.IsoPath, extractedIsoPath);

                Q9xS.Update9xDir(options.UpdatePath, extractedIsoPath);
                Q9xS.CreateISO(extractedIsoPath, options.OutputPath);

            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                if (!options.NoClean && Directory.Exists(options.WorkingDir))
                {
                    Console.WriteLine("Cleaning working directory " + options.WorkingDir);
                    Directory.Delete(options.WorkingDir, true);
                }
                    
            }
        }
    }
}

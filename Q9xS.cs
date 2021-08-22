using System;
using System.IO;
using DiscUtils;
using DiscUtils.Iso9660;
using System.Text.RegularExpressions;

namespace Q9xS
{
    static class Q9xS
    {
        static string layoutsDir = Path.Join(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "layouts");
        static BootDeviceEmulation emu = BootDeviceEmulation.NoEmulation;
        static int bootLoadSegment = 0;
        static string bootImagePath = "";
        static long bootImageStart = 0;
        public static void Update9xDir(string updatesDir, string extracted9xDir)
        {
            bool updated = false;

            string subDir = "";
            if (!Path.IsPathRooted(updatesDir))
            {
                updatesDir = Path.GetFullPath(updatesDir);
            }
            if (Directory.Exists(Path.Join(extracted9xDir, "Win95")))
                subDir = Path.Join(extracted9xDir, "Win95");
            else if (Directory.Exists(Path.Join(extracted9xDir, "Win98")))
                subDir = Path.Join(extracted9xDir, "Win98");
            else if (Directory.Exists(Path.Join(extracted9xDir, "Win9x")))
                subDir = Path.Join(extracted9xDir, "Win9x");
            else
            {
                throw new DirectoryNotFoundException(extracted9xDir + " is not an extracted windows iso");
            }


            string subDirName = new DirectoryInfo(subDir).Name;
            string layoutPath = Path.Join(subDir, "layout.inf");
            string layout1Path = Path.Join(subDir, "layout1.inf");
            string layout2Path = Path.Join(subDir, "layout2.inf");
            Directory.Exists(layoutsDir);
            CopyFreshLayoutInf(layoutPath, subDirName);

            if (File.Exists(Path.Join(layoutsDir, subDirName, "layout1.inf")))
                CopyFreshLayoutInf(layout1Path, subDirName);

            if (File.Exists(Path.Join(layoutsDir, subDirName, "layout2.inf")))
                CopyFreshLayoutInf(layout2Path, subDirName);

            foreach (string update in Directory.GetFiles(updatesDir))
            {
                string copyTo = Path.Join(subDir, Path.GetFileName(update));
                FileInfo updateInfo = new FileInfo(update);
                FileInfo copyToInfo = new FileInfo(copyTo);
                if (!File.Exists(copyTo) || updateInfo.LastWriteTime > copyToInfo.LastWriteTime)
                {
                    updated = true;
                    updateInfo.Attributes = FileAttributes.Normal;
                    updateInfo.CopyTo(copyToInfo.FullName, true);
                    copyToInfo.Attributes = FileAttributes.Normal;
                    Console.WriteLine(updateInfo.FullName + " was copied to " + copyToInfo.FullName);
                }
            }

            // begin updating layout(s)
            if (updated)
            {
                string[] updatedSubDirFiles = Directory.GetFiles(subDir);
                UpdateLayout(updatedSubDirFiles, layoutPath);

                if (File.Exists(layout1Path))
                    UpdateLayout(updatedSubDirFiles, layout1Path);

                if (File.Exists(layout2Path))
                    UpdateLayout(updatedSubDirFiles, layout2Path);
            }
        }

        public static void CopyFreshLayoutInf(string layoutPath, string subDirName)
        {
            if (!File.Exists(layoutPath))
            {
                string layoutCopyFromPath = Path.Join(layoutsDir, subDirName, Path.GetFileName(layoutPath));
                if (!File.Exists(layoutCopyFromPath))
                {
                    throw new FileNotFoundException(layoutCopyFromPath + " does not exist.\n" +
                        "Make sure that you have the layouts folder in the same directory" +
                        "as this application.");
                }
                Console.WriteLine(layoutPath + " does not exist, copying a fresh one...");
                File.Copy(layoutCopyFromPath, layoutPath);
            }
        }

        public static void UpdateLayout(string[] updatedSubDirFiles, string layoutToUpdatePath)
        {
            string layoutText = File.ReadAllText(layoutToUpdatePath);

            foreach (string file in updatedSubDirFiles)
            {
                string fileName = Path.GetFileName(file);
                long fileSize = new FileInfo(file).Length;
                for (int i = 0; i < 29; i++)
                {
                    string regEx = fileName + @"=" + i + @",,[1-9]*";
                    Match match = Regex.Match(layoutText, regEx, RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        layoutText = Regex.Replace(layoutText, regEx, fileName + "=" + i + ",," + fileSize, RegexOptions.IgnoreCase);
                        break;
                    }
                }
                Console.WriteLine("Layout entry for " + fileName + " updated.");
            }

            File.WriteAllText(layoutToUpdatePath, layoutText);
            Console.WriteLine(layoutToUpdatePath + " successfully updated.");
        }

        public static CDBuilder AddToIso(CDBuilder builder, string dirToIso) {
            return AddToIso(builder, dirToIso, dirToIso);
        }

        private static CDBuilder AddToIso(CDBuilder builder, string rootDir, string dirToIso)
        {
            foreach (string directory in Directory.GetDirectories(dirToIso))
            {
                AddToIso(builder, rootDir, directory);
            }
            foreach (string file in Directory.GetFiles(dirToIso))
            {
                Stream file2add = File.Open(file, FileMode.Open);
                string file2addPath = Path.GetRelativePath(rootDir, file);
                builder.AddFile(file2addPath, file2add);
                Console.WriteLine(file2addPath + " was added to the iso.");
            }
            return builder;
        }

        public static void CreateISO(string dirToIso, string outputPath = "")
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                outputPath = Path.GetFileNameWithoutExtension(dirToIso) + "_slip.iso";
            }
            CDBuilder builder = new CDBuilder
            {
                UseJoliet = true,
                VolumeIdentifier = "WIN_9X"
            };

            builder = AddToIso(builder, dirToIso);

            FileStream bootImage = null;
/*            if (File.Exists(bootImagePath))
            {
                bootImage = File.OpenRead(bootImagePath);
                builder.SetBootImage(bootImage, emu, bootLoadSegment);
            }
            */
            builder.Build(outputPath);
            if (bootImage != null)
            {
                bootImage.Close();
            }
            Console.WriteLine(outputPath + " successfully created.");
        }
        public static void ExtractBootImage(string toExtract, string folderPath)
        {
            CDReader Reader = new CDReader(File.Open(toExtract, FileMode.Open), true);
            if (false && Reader.HasBootImage)
            {
                emu = Reader.BootEmulation;
                bootLoadSegment = Reader.BootLoadSegment;
                bootImageStart = Reader.BootImageStart;
                using var read = Reader.OpenBootImage();
                int imagelength = (int)read.Length;
                if (Reader.BootEmulation == BootDeviceEmulation.Diskette1440KiB)
                {
                    // Ignore what boot catalog says, it's likely lying.
                    imagelength = 1474560;
                }
                var buffer = new byte[imagelength];
                read.Read(buffer,0,imagelength);
                read.Close();
                bootImagePath = Path.Join(folderPath, "boot.img");
                ForceMkdirp(Path.GetDirectoryName(folderPath));

                using var binWriter = new BinaryWriter(File.Open(bootImagePath, FileMode.Create));

                binWriter.Write(buffer);
                binWriter.Close();
                Console.WriteLine("Boot image was succesfully extracted from ISO.");
            }
            Reader.Dispose();
        }

        public static void ExtractISO(string toExtract, string folderPath)
        {
            CDReader Reader = new CDReader(File.Open(toExtract, FileMode.Open), true);
            ExtractDirectory(Reader.Root, folderPath);
            Console.WriteLine(toExtract + " was succesfully extracted.");

            Reader.Dispose();
        }

        public static void ExtractDirectory(DiscDirectoryInfo Dinfo, string RootPath, string PathinISO = "")
        {

            if (!string.IsNullOrWhiteSpace(PathinISO))
            {
                PathinISO = Path.Join(PathinISO, Dinfo.Name);
            }
            RootPath = Path.Join(RootPath, Dinfo.Name);

            ForceMkdirp(RootPath);
            foreach (DiscDirectoryInfo dinfo in Dinfo.GetDirectories())
            {
                ExtractDirectory(dinfo, RootPath, PathinISO);
            }
            foreach (DiscFileInfo finfo in Dinfo.GetFiles())
            {
                using (Stream FileStr = finfo.OpenRead())
                {
                    char[] charsToRemove = { ';', '1' };
                    string fileName = finfo.Name.TrimEnd(charsToRemove);
                    using (FileStream Fs = File.Create(Path.Join(RootPath, fileName)))
                    {
                        FileStr.CopyTo(Fs, 4 * 1024);
                        Console.WriteLine(fileName + " was extracted.");
                    }
                }
            }
        }

        static void ForceMkdirp(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (DirectoryNotFoundException)
            {
                ForceMkdirp(Path.GetDirectoryName(path));
            }
            catch (PathTooLongException)
            {
                ForceMkdirp(Path.GetDirectoryName(path));
            }
        }
    }
}

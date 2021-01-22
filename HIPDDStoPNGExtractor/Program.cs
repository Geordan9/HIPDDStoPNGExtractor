using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using ArcSysAPI.Models;
using ArcSysAPI.Utils;
using HIPDDStoPNGExtractor.Models;
using HIPDDStoPNGExtractor.Utils;
using HIPDDStoPNGExtractor.Utils.Extensions;
using static HIPDDStoPNGExtractor.Utils.Dialogs;

namespace HIPDDStoPNGExtractor
{
    internal class Program
    {
        [Flags]
        public enum Options
        {
            OutputPath = 0x1,
            Palette = 0x2,
            Transparent = 0x4,
            ConvertPalettes = 0x8
        }

        public static ConsoleOption[] ConsoleOptions =
        {
            new ConsoleOption
            {
                Name = "Palette",
                ShortOp = "-p",
                LongOp = "--palette",
                Description = "Apply a palette to palette based images.",
                HasArg = true,
                Flag = Options.Palette
            },
            new ConsoleOption
            {
                Name = "Transparent",
                ShortOp = "-t",
                LongOp = "--transparent",
                Description = "Make the first color in the image palette to transparent.",
                Flag = Options.Transparent
            },
            new ConsoleOption
            {
                Name = "ConvertPalettes",
                ShortOp = "-cp",
                LongOp = "--convertpalettes",
                Description = "Extract and convert HPL palettes files as ACT files.",
                Flag = Options.ConvertPalettes
            },
            new ConsoleOption
            {
                Name = "OutputPath",
                ShortOp = "-o",
                LongOp = "--output",
                Description = "Define the location to save the output.",
                HasArg = true,
                Flag = Options.OutputPath
            }
        };

        public static string currentFile = string.Empty;

        public static Bitmap bitmap = null;

        [STAThread]
        private static void Main(string[] args)
        {
            Console.WriteLine("\nHIP and DDS to PNG Extractor\nprogrammed by: Geo\n\n");

            try
            {
                if (args.Length > 0)
                {
                    if (args[0] == "-h" || args[0] == "--help")
                    {
                        ShowUsage();
                        return;
                    }

                    var newArgsList = new List<string>();

                    var filePath = args[0].Replace("\"", "\\");

                    var saveFolder = string.Empty;

                    var options = new Options();

                    newArgsList.Add(args[0]);

                    if (args.Length > 1)
                        for (var i = 0; i < args.Length; i++)
                        {
                            var arg = args[i];
                            if (arg.First() != '-')
                                continue;

                            newArgsList.Add(arg);

                            foreach (var co in ConsoleOptions)
                                if (arg == co.ShortOp || arg == co.LongOp)
                                {
                                    options |= (Options) co.Flag;
                                    if (co.HasArg)
                                    {
                                        var subArgsList = new List<string>();
                                        for (var j = i; j < args.Length - 1; j++)
                                        {
                                            var subArg = args[j + 1];

                                            if (subArg.First() == '-')
                                                break;

                                            subArgsList.Add(subArg);
                                            i++;
                                        }

                                        co.SpecialObject = subArgsList.ToArray();
                                    }
                                }
                        }

                    args = newArgsList.ToArray();

                    Color[] colorPalette = null;

                    foreach (var co in ConsoleOptions)
                    {
                        if (co.Flag == null || co.SpecialObject == null)
                            continue;

                        if (co.SpecialObject != null)
                        {
                            if ((Options) co.Flag == Options.Palette &&
                                options.HasFlag(Options.Palette))
                            {
                                var colorRange = 256;
                                var subArgs = (string[]) co.SpecialObject;
                                var path = subArgs[0];
                                var ext = Path.GetExtension(path);
                                VirtualFileSystemInfo vfsi = null;
                                byte[] bytes = null;
                                if (ext == ".pac" ||
                                    ext == ".paccs" ||
                                    ext == ".pacgz" ||
                                    ext == ".fontpac")
                                {
                                    vfsi = new PACFileInfo(path);
                                    for (var i = 1; i < subArgs.Length && vfsi is PACFileInfo; i++)
                                    {
                                        var pfi = (PACFileInfo) vfsi;
                                        int index;
                                        if (!int.TryParse(subArgs[i], out index))
                                            throw new Exception(
                                                $"Palette's sub argument index is invalid: {subArgs[i]}");
                                        var files = pfi.GetFiles();
                                        if (index >= files.Length)
                                            throw new Exception(
                                                $"Palette's sub argument index exceeds PAC file count: {subArgs[i]}");

                                        vfsi = files[index];
                                    }

                                    bytes = vfsi.GetBytes();
                                    ext = vfsi.Extension;
                                }
                                else if (ext == ".hpl")
                                {
                                    vfsi = new HPLFileInfo(path);
                                    bytes = vfsi.GetBytes();
                                    ext = vfsi.Extension;
                                }
                                else if (ext == ".hip")
                                {
                                    vfsi = new HIPFileInfo(path);
                                    if (((HIPFileInfo) vfsi).Palette == null)
                                        return;
                                    bytes = vfsi.GetBytes();
                                    ext = vfsi.Extension;
                                }
                                else
                                {
                                    bytes = File.ReadAllBytes(path);
                                    if (subArgs.Length > 1)
                                        if (!int.TryParse(subArgs[1], out colorRange))
                                            throw new Exception(
                                                $"Palette's sub argument color range is invalid: {subArgs[1]}");
                                }

                                if (bytes == null)
                                    return;
                                switch (ext)
                                {
                                    case ".hpl":
                                        co.SpecialObject = ((HPLFileInfo) vfsi).Palette;
                                        break;
                                    case ".hip":
                                        co.SpecialObject = ((HIPFileInfo) vfsi).Palette;
                                        break;
                                    case ".act":
                                        co.SpecialObject = PaletteTools.ReadACTPalette(bytes, colorRange);
                                        break;
                                    case ".pal":
                                        co.SpecialObject = PaletteTools.ReadPALPalette(bytes, colorRange);
                                        break;
                                    case ".aco":
                                        co.SpecialObject = PaletteTools.ReadACOPalette(bytes, colorRange);
                                        break;
                                    case ".ase":
                                        co.SpecialObject = PaletteTools.ReadASEPalette(bytes, colorRange);
                                        break;
                                }

                                colorPalette = (Color[]) co.SpecialObject;
                            }

                            if ((Options) co.Flag == Options.OutputPath &&
                                options.HasFlag(Options.OutputPath))
                                saveFolder = ((string[]) co.SpecialObject)[0];
                        }
                    }

                    if (string.IsNullOrWhiteSpace(saveFolder))
                        saveFolder = Path.Combine(OpenFolderDialog("Export files in..."), "Converted\\");
                    else
                        saveFolder = Path.Combine(saveFolder, "Converted\\");

                    var attr = File.GetAttributes(filePath);

                    var vfiles = new List<VirtualFileSystemInfo>();

                    var baseDirectory = string.Empty;

                    var convertPalettes = options.HasFlag(Options.ConvertPalettes);

                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        Console.WriteLine(
                            $"Scanning for all {(convertPalettes ? "hip, dds, and hpl" : "hip and dds")} files...");
                        baseDirectory = Directory.GetParent(filePath).FullName;
                        var files = DirSearch(filePath);
                        foreach (var file in files)
                            vfiles.Add(new VirtualFileSystemInfo(file));
                    }
                    else
                    {
                        baseDirectory = Directory.GetParent(filePath).FullName;
                        vfiles.Add(new VirtualFileSystemInfo(filePath));
                    }

                    var len = vfiles.Count;

                    for (var i = 0; i < len; i++)
                    {
                        currentFile = vfiles[i].FullName;
                        var pacFileInfo = vfiles[i] as PACFileInfo;
                        if (pacFileInfo != null)
                        {
                            vfiles.AddRange(RecursivePACExplore(pacFileInfo));
                        }
                        else if (vfiles[i].Extension == ".pac" ||
                                 vfiles[i].Extension == ".paccs" ||
                                 vfiles[i].Extension == ".pacgz" ||
                                 vfiles[i].Extension == ".fontpac")
                        {
                            vfiles.AddRange(RecursivePACExplore(new PACFileInfo(vfiles[i].GetPrimaryPath())));
                        }
                        else if (string.IsNullOrWhiteSpace(vfiles[i].Extension))
                        {
                            if (MD5Tools.IsMD5(vfiles[i].Name))
                            {
                                pacFileInfo = new PACFileInfo(vfiles[i].GetPrimaryPath());
                                vfiles.AddRange(RecursivePACExplore(pacFileInfo));
                            }
                            else
                            {
                                var length = vfiles[i].Name.LastIndexOf('_');
                                if (length >= 32)
                                {
                                    var name = vfiles[i].Name.Substring(0, length);
                                    if (MD5Tools.IsMD5(name))
                                    {
                                        pacFileInfo = new PACFileInfo(vfiles[i].GetPrimaryPath());
                                        vfiles.AddRange(RecursivePACExplore(pacFileInfo));
                                    }
                                }
                            }
                        }
                    }

                    var hipFiles = new List<HIPFileInfo>();
                    var ddsFiles = new List<DDSFileInfo>();
                    var hplFiles = new List<HPLFileInfo>();

                    foreach (var vfile in vfiles)
                    {
                        var hipFileInfo = vfile as HIPFileInfo;
                        if (hipFileInfo != null)
                            hipFiles.Add(hipFileInfo);
                        else if (vfile.Extension.ToLower() == ".hip")
                            hipFiles.Add(new HIPFileInfo(vfile.GetPrimaryPath()));
                        var ddsFileInfo = vfile as DDSFileInfo;
                        if (ddsFileInfo != null)
                            ddsFiles.Add(ddsFileInfo);
                        else if (vfile.Extension.ToLower() == ".dds")
                            ddsFiles.Add(new DDSFileInfo(vfile.GetPrimaryPath()));

                        if (convertPalettes)
                        {
                            var hplFileInfo = vfile as HPLFileInfo;
                            if (hplFileInfo != null)
                                hplFiles.Add(hplFileInfo);
                            else if (vfile.Extension.ToLower() == ".hpl")
                                hplFiles.Add(new HPLFileInfo(vfile.GetPrimaryPath()));
                        }
                    }

                    vfiles.Clear();

                    Directory.CreateDirectory(saveFolder);

                    VirtualFileSystemInfo prevVFSI = null;

                    len = hipFiles.Count;

                    for (var i = 0; i < len; i++)
                    {
                        var curVFSI = hipFiles[0].VirtualRoot;

                        curVFSI.Active = true;

                        if (prevVFSI != null && prevVFSI.FullName != curVFSI.FullName)
                            prevVFSI.Active = false;

                        ProcessFile(hipFiles[0], baseDirectory, saveFolder, true, colorPalette,
                            options.HasFlag(Options.Transparent));

                        prevVFSI = curVFSI;

                        hipFiles.RemoveAt(0);
                    }

                    len = ddsFiles.Count;

                    for (var i = 0; i < len; i++)
                    {
                        var curVFSI = ddsFiles[0].VirtualRoot;

                        curVFSI.Active = true;

                        if (prevVFSI.FullName != curVFSI.FullName)
                            prevVFSI.Active = false;

                        ProcessFile(ddsFiles[0], baseDirectory, saveFolder, false);

                        prevVFSI = curVFSI;

                        ddsFiles.RemoveAt(0);
                    }

                    len = hplFiles.Count;

                    for (var i = 0; i < len; i++)
                    {
                        var curVFSI = hplFiles[0].VirtualRoot;

                        curVFSI.Active = true;

                        if (prevVFSI.FullName != curVFSI.FullName)
                            prevVFSI.Active = false;

                        currentFile = hplFiles[0].FullName;
                        var ep = hplFiles[0].GetExtendedPaths();
                        var extPaths = new string[ep.Length + 1];
                        Array.Copy(ep, 0, extPaths, 1, ep.Length);
                        extPaths[0] = hplFiles[0].GetPrimaryPath();

                        if (extPaths.Length > 0)
                            extPaths[0] = extPaths[0].Replace(baseDirectory, string.Empty);
                        var extPath = string.Join("\\", extPaths.Length > 0 ? extPaths : new[] {hplFiles[0].Name});
                        var savePath = Path.GetFullPath((saveFolder + extPath).Replace('?', '_'));

                        if (hplFiles[0].VirtualRoot != hplFiles[0] &&
                            !string.IsNullOrWhiteSpace(hplFiles[0].VirtualRoot.Extension))
                            savePath = savePath.Replace(hplFiles[0].VirtualRoot.Extension, string.Empty);

                        savePath = savePath.Replace(hplFiles[0].Extension, ".act").Replace("?", "_");

                        if (File.Exists(savePath))
                            if (new FileInfo(savePath).Length > 0)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine($"'{savePath}' already exists. Skipping...");
                                Console.ForegroundColor = ConsoleColor.White;
                                continue;
                            }

                        Console.Write("Converting ");
                        Console.ForegroundColor = hplFiles[0].GetTextColor();
                        Console.Write($"{hplFiles[0].Name} ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("to act...");
                        Directory.CreateDirectory(Path.GetDirectoryName(savePath));

                        File.WriteAllBytes(savePath, PaletteTools.CreateACTByteArray(hplFiles[0].Palette));

                        Console.Write("Finished converting ");
                        Console.ForegroundColor = hplFiles[0].GetTextColor();
                        Console.Write($"{hplFiles[0].Name} ");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("to act.");

                        prevVFSI = curVFSI;

                        hplFiles.RemoveAt(0);
                    }

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Done!");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Please input the path of a file or folder.\n");
                    ShowUsage();
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Current File: {currentFile}");
                Console.WriteLine(ex);
                Console.WriteLine("Something went wrong!");
                Console.ForegroundColor = ConsoleColor.White;
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        public static string[] DirSearch(string sDir)
        {
            var stringList = new List<string>();
            foreach (var f in Directory.GetFiles(sDir)) stringList.Add(f);
            foreach (var d in Directory.GetDirectories(sDir)) stringList.AddRange(DirSearch(d));

            return stringList.ToArray();
        }

        public static VirtualFileSystemInfo[] RecursivePACExplore(PACFileInfo pfi, int level = 0)
        {
            Console.Write(new string(' ', level * 4) + "Scanning ");
            Console.ForegroundColor = pfi.GetTextColor();
            Console.Write($"{pfi.Name}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("...");

            currentFile = pfi.FullName;

            var vfiles = new List<VirtualFileSystemInfo>();
            vfiles.AddRange(pfi.GetFiles());

            var len = vfiles.Count;

            for (var i = 0; i < len; i++)
            {
                var pacFileInfo = vfiles[i] as PACFileInfo;
                if (pacFileInfo != null)
                    vfiles.AddRange(RecursivePACExplore(pacFileInfo, level + 1));
            }

            return vfiles.ToArray();
        }

        public static void ProcessFile(VirtualFileSystemInfo vfsi, string baseDirectory, string saveFolder,
            bool isHIP, Color[] cp = null, bool transparent = false)
        {
            currentFile = vfsi.FullName;
            var ep = vfsi.GetExtendedPaths();
            var extPaths = new string[ep.Length + 1];
            Array.Copy(ep, 0, extPaths, 1, ep.Length);
            extPaths[0] = vfsi.GetPrimaryPath();

            if (extPaths.Length > 0)
                extPaths[0] = extPaths[0].Replace(baseDirectory, string.Empty);
            var extPath = string.Join("\\", extPaths.Length > 0 ? extPaths : new[] {vfsi.Name});
            var savePath = Path.GetFullPath((saveFolder + extPath).Replace('?', '_'));

            if (vfsi.VirtualRoot != vfsi && !string.IsNullOrWhiteSpace(vfsi.VirtualRoot.Extension))
                savePath = savePath.Replace(vfsi.VirtualRoot.Extension, string.Empty);

            savePath = savePath.Replace(vfsi.Extension, ".png").Replace("?", "_");
            if (File.Exists(savePath))
                if (new FileInfo(savePath).Length > 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"'{savePath}' already exists. Skipping...");
                    Console.ForegroundColor = ConsoleColor.White;
                    return;
                }

            Console.Write("Converting ");
            Console.ForegroundColor = vfsi.GetTextColor();
            Console.Write($"{vfsi.Name} ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("to png...");
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));

            using (var bitmap = isHIP ? ((HIPFileInfo) vfsi).GetImage(cp) : ((DDSFileInfo) vfsi).GetImage())
            {
                if (bitmap == null || bitmap.Width == 0 && bitmap.Height == 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(
                        $"{vfsi.Name}'s format is not supported, there's no image data, or there's a memory leak.");
                    Console.ForegroundColor = ConsoleColor.White;
                    return;
                }

                if (transparent)
                {
                    var palette = bitmap.Palette;
                    if (palette.Entries.Length > 0)
                    {
                        palette.Entries[0] = Color.Transparent;
                        bitmap.Palette = palette;
                    }
                }

                bitmap.Save(savePath, ImageFormat.Png);

                Console.Write("Finished converting ");
                Console.ForegroundColor = vfsi.GetTextColor();
                Console.Write($"{vfsi.Name} ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("to png.");
            }
        }

        private static void ShowUsage()
        {
            var shortOpMaxLength =
                ConsoleOptions.Select(co => co.ShortOp).OrderByDescending(s => s.Length).First().Length;
            var longOpMaxLength =
                ConsoleOptions.Select(co => co.LongOp).OrderByDescending(s => s.Length).First().Length;

            Console.WriteLine("Usage: <file/folder path> [options...]");

            Console.WriteLine("Options:");
            foreach (var co in ConsoleOptions)
                Console.WriteLine(
                    $"{co.ShortOp.PadRight(shortOpMaxLength)}\t{co.LongOp.PadRight(longOpMaxLength)}\t{co.Description}");

            Console.WriteLine("\rPress Any Key to exit...");
            Console.ReadKey();
        }
    }
}
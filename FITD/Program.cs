using System;
using System.Collections.Generic;
using System.Linq;
using SALT.Moveset.AnimCMD;
using System.IO;
using SALT.Moveset.MSC;
using SALT.PARAMS;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Threading;
using System.Globalization;

namespace FITD
{
    public class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            Console.WriteLine($"\n> FITD v0.77 - Smash 4 Fighter Decompiler\n" +
                               "> Licensed under the MIT License\n" +
                               "> Copyright(c) 2016-2017 Sammi Husky\n");

            string target = "";
            string motion = "";
            string output = "output";
            string events = "";
            bool decompRaw = false;

            if (args.Length == 0)
            {
                print_help();
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                var str = args[i];
                if (str.StartsWith("-"))
                {
                    switch (str)
                    {
                        case "-m":
                            if (i + 1 < args.Length)
                            {
                                motion = args[++i];
                            }
                            break;
                        case "-o":
                            if (i + 1 < args.Length)
                            {
                                output = args[++i];
                            }
                            break;
                        case "-h":
                        case "--help":
                            print_help();
                            return;
                        case "-e":
                            if (i + 1 < args.Length)
                            {
                                events = args[++i];
                            }
                            break;
                        case "--raw":
                            decompRaw = true;
                            break;
                    }
                }
                else if (str.EndsWith(".mtable"))
                {
                    target = str;
                }
                else if (str.EndsWith(".mscsb"))
                {
                    target = str;
                }
            }

            if (!string.IsNullOrWhiteSpace(events))
            {
                ACMD_INFO.OverrideInfo(events);
            }

            if (!string.IsNullOrEmpty(target) && target.EndsWith(".mtable"))
            {
                decompile_acmd(target, motion, output);
            }
            else if (!string.IsNullOrEmpty(target) && target.EndsWith(".mscsb"))
            {
                decompile_msc(target, output, decompRaw);
            }

            Console.WriteLine("> All tasks finished");
        }

        public static void print_help()
        {
            Console.WriteLine("> S4FC [options] [.mtable file / .mscsb file]");
            Console.WriteLine("> Options:\n" +
                              "> \t-o: Sets the aplication output directory\n" +
                              "> \t-e: Overrides the internal event dictionary with specified events file\n" +
                              "> \t-m: Sets animation folder for parsing animation names\n" +
                              "> \t-h --help: Displays this help message\n" +
                              "> \t--raw: Also decompile MSC to raw commands in addition to intelligent decompilation");
        }

        public static void decompile_acmd(string mtable, string motionFolder, string output)
        {

            string script_dir = Path.Combine(output, "animcmd");
            Directory.CreateDirectory(script_dir);

            Console.WriteLine($">\tDecompile ACMD.. -> \"{script_dir}\"");

            Endianness endian = Endianness.Big;
            SortedList<string, ACMDFile> files = new SortedList<string, ACMDFile>();

            Dictionary<uint, string> animations = new Dictionary<uint, string>();
            if (!string.IsNullOrEmpty(motionFolder))
                animations = Animations.ParseAnimations(motionFolder);

            //string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //if (mtable.Contains(Path.DirectorySeparatorChar))
            //    dir = Path.GetDirectoryName(mtable);
            string dir = Path.GetDirectoryName(mtable);

            foreach (string path in Directory.EnumerateFiles(dir, "*.bin"))
            {
                var file = new ACMDFile(path);
                files.Add(Path.GetFileNameWithoutExtension(path), file);
                endian = file.Endian;
            }
            var table = new MTable(mtable, endian);
            var hashes = new List<uint>(table.ToList());

            write_mlist(table, animations, Path.Combine(output, "fighter.mlist"));

            // workaround for unlisted moves
            foreach (var f in files)
                foreach (var s in f.Value.Scripts)
                    if (!hashes.Contains(s.Key))
                        hashes.Add(s.Key);


            foreach (uint hash in hashes)
            {
                string animName = $"0x{hash:X8}";
                if (animations.ContainsKey(hash))
                    animName = animations[hash];

#if DEBUG
                Console.WriteLine($">\tDecompiling {animName}..");
#endif

                ACMDScript game = null, effect = null, sound = null, expression = null;
                if (files.ContainsKey("game") && files["game"].Scripts.ContainsKey(hash))
                {
                    game = (ACMDScript)files["game"].Scripts[hash];
                }
                if (files.ContainsKey("effect") && files["effect"].Scripts.ContainsKey(hash))
                {
                    effect = (ACMDScript)files["effect"].Scripts[hash];
                }
                if (files.ContainsKey("sound") && files["sound"].Scripts.ContainsKey(hash))
                {
                    sound = (ACMDScript)files["sound"].Scripts[hash];
                }
                if (files.ContainsKey("expression") && files["expression"].Scripts.ContainsKey(hash))
                {
                    expression = (ACMDScript)files["expression"].Scripts[hash];
                }

                write_movedef(game, effect, sound, expression, animName, !table.Contains(hash), script_dir);
            }
            Console.WriteLine(">\tFinished\n");
        }
        public static void decompile_msc(string file, string output, bool includeRaw)
        {
            MSCFile f = new MSCFile(file);
            if (!Directory.Exists(output))
                Directory.CreateDirectory(output);

            foreach (var script in f.Scripts)
            {
#if DEBUG
                Console.WriteLine($"Decompiling script {f.Scripts.IndexOfKey(script.Key)} at offset  0x{script.Key:X8}");
#endif
                string path = output + $"/{f.Scripts.IndexOfKey(script.Key)}.mscript";
                if (f.EntryPoint == script.Key)
                    path = output + "/entrypoint.mscript";

                using (var writer = File.CreateText(path))
                {
                    writer.Write(((MSCScript)script.Value).Decompile());
                }

                if (includeRaw)
                {
                    path = output + $"/{f.Scripts.IndexOfKey(script.Key)}_raw.mscript";
                    if (f.EntryPoint == script.Key)
                        path = output + "/entrypoint_raw.mscript";

                    using (var writer = File.CreateText(path))
                    {
                        writer.Write(((MSCScript)script.Value).Deserialize());
                    }
                }
            }
        }

        private static void write_movedef(ACMDScript game, ACMDScript effect, ACMDScript sound, ACMDScript expression, string animname, bool unlisted, string outdir)
        {
            using (StreamWriter writer = new StreamWriter(Path.Combine(outdir, $"{animname}.acm")))
            {
                writer.Write($"MoveDef {animname}");
                if (unlisted)
                    writer.Write(" : Unlisted\n");
                else
                    writer.Write("\n");
                writer.WriteLine("{");

                writer.WriteLine("\tMain()\n\t{");
                if (game != null)
                {
                    var commands = game.Deserialize().Split('\n');
                    foreach (string cmd in commands)
                        writer.WriteLine("\t\t" + cmd.TrimEnd());
                }
                writer.WriteLine("\t}\n");
                writer.WriteLine("\tEffect()\n\t{");
                if (effect != null)
                {
                    var commands = effect.Deserialize().Split('\n');
                    foreach (string cmd in commands)
                        writer.WriteLine("\t\t" + cmd.TrimEnd());
                }
                writer.WriteLine("\t}\n");
                writer.WriteLine("\tSound()\n\t{");
                if (sound != null)
                {
                    var commands = sound.Deserialize().Split('\n');
                    foreach (string cmd in commands)
                        writer.WriteLine("\t\t" + cmd.TrimEnd());
                }
                writer.WriteLine("\t}\n");
                writer.WriteLine("\tExpression()\n\t{");
                if (expression != null)
                {
                    var commands = expression.Deserialize().Split('\n');
                    foreach (string cmd in commands)
                        writer.WriteLine("\t\t" + cmd.TrimEnd());
                }
                writer.WriteLine("\t}\n");
                writer.WriteLine("}");
            }
        }
        private static void write_mlist(MTable table, Dictionary<uint, string> anims, string filename)
        {
            using (var writer = File.CreateText(filename))
            {
                foreach (var u in table)
                {
                    if (anims.ContainsKey(u))
                        writer.WriteLine(anims[u]);
                    else
                        writer.WriteLine($"0x{u:X8}");
                }
            }
        }
    }
}

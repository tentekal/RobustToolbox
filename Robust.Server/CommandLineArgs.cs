﻿using System.Collections.Generic;
using Robust.Shared.Utility;
using C = System.Console;

namespace Robust.Server
{
    internal sealed class CommandLineArgs
    {
        public string ConfigFile { get; }
        public string DataDir { get; }
        public IReadOnlyCollection<(string key, string value)> CVars { get; }

        // Manual parser because C# has no good command line parsing libraries. Also dependencies bad.
        // Also I don't like spending 100ms parsing command line args. Do you?
        public static bool TryParse(IReadOnlyList<string> args, out CommandLineArgs parsed)
        {
            parsed = null;
            string configFile = null;
            string dataDir = null;
            var cvars = new List<(string, string)>();

            using var enumerator = args.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var arg = enumerator.Current;
                if (arg == "--config-file")
                {
                    if (!enumerator.MoveNext())
                    {
                        C.WriteLine("Missing config file.");
                        return false;
                    }

                    configFile = enumerator.Current;
                }
                else if (arg == "--data-dir")
                {
                    if (!enumerator.MoveNext())
                    {
                        C.WriteLine("Missing data directory.");
                        return false;
                    }

                    dataDir = enumerator.Current;
                }
                else if (arg == "--cvar")
                {
                    if (!enumerator.MoveNext())
                    {
                        C.WriteLine("Missing cvar value.");
                        return false;
                    }

                    var cvar = enumerator.Current;
                    DebugTools.AssertNotNull(cvar);
                    var split = cvar.Split("=");

                    if (split.Length < 2)
                    {
                        C.WriteLine("Expected = in cvar.");
                        return false;
                    }

                    cvars.Add((split[0], split[1]));
                }
                else if (arg == "--help")
                {
                    PrintHelp();
                    return false;
                }
                else
                {
                    C.WriteLine("Unknown argument: {0}", arg);
                }
            }

            parsed = new CommandLineArgs(configFile, dataDir, cvars);
            return true;
        }

        private static void PrintHelp()
        {
            C.WriteLine(@"
Options:
  --config-file     Path to the config file to read from.
  --data-dir        Path to the data directory to read/write from/to.
  --cvar            Specifies an additional cvar overriding the config file. Syntax is <key>=<value>
  --help            Display this help text and exit.
");
        }

        private CommandLineArgs(string configFile, string dataDir, IReadOnlyCollection<(string, string)> cVars)
        {
            ConfigFile = configFile;
            DataDir = dataDir;
            CVars = cVars;
        }
    }
}

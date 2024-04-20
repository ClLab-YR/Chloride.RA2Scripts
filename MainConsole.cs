﻿using Chloride.RA2Scripts.Formats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chloride.RA2Scripts
{
    public static class MainConsole
    {
        private static readonly IniDoc Config = new();
        internal static IniSection Arguments = Config.Default; 

        static MainConsole()
        {
            // = =
            var path = Path.Join(Environment.CurrentDirectory, "config.ini");

            Config.Deserialize(new FileInfo(path));
        }

        public static IniValue GetArg(string key)
        {
            _ = Arguments.Contains(key, out IniValue ret);
            return ret;
        }

        public static void Main(string[] args)
        {
            var file = new FileInfo(GetArg("FilePath").ToString());
            var doc = ReadIni(file);
            new OwnerMapScript(Config).TransferOwnerReference(
                doc,
                GetArg("Old").ToString(),
                GetArg("New").ToString());
            doc.Serialize(file);
        }

        public static IniDoc ReadIni(FileInfo file, bool include = false)
        {
            var paths = include ? IniSerializer.TryGetIncludes(file.FullName) : new() { file };
            var ret = new IniDoc();
            ret.Deserialize(paths.Where(i => i.Exists).ToArray());
            return ret;
        }

        //public static IniSection? Get(string section)
        //{
        //    _ = Config.Contains(section, out IniSection? ret);
        //    return ret;
        //}
    }
}

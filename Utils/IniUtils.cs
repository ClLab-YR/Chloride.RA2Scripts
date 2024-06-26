﻿using Chloride.RA2Scripts.Formats;

namespace Chloride.RA2Scripts.Utils
{
    public static class IniUtils
    {
        /// <summary>
        /// Hint: Dictionary keys are readonly and not able to override,
        /// and you could update <c>IniValue.Value</c> to override Dictionary values.
        /// </summary>
        public static void IteratePairs(IniDoc doc, string section, Action<string, IniValue> action)
        {
            if (!doc.Contains(section, out IniSection? sect))
                return;
            foreach (var i in sect!)
            {
                if (i.Key.StartsWith(';'))  // comments
                    continue;
                IniValue val = i.Value;
                action.Invoke(i.Key, val);
            }
        }
        public static void ReplaceValue(IniDoc doc, string section, Action<string[]> action)
            => IteratePairs(doc, section, (key, val) =>
            {
                var seq = val.Split();
                action.Invoke(seq);
                val.Value = string.Join(',', seq);
            });

        public static IniDoc ReadIni(FileInfo file, bool include = false)
        {
            var paths = include ? IniSerializer.TryGetIncludes(file.FullName) : new() { file };
            var ret = new IniDoc();
            ret.Deserialize(paths.Where(i => i.Exists).ToArray());
            return ret;
        }

        public static void ResortTypeList(this IniDoc doc, string typeName)
        {
            var tlist = doc.GetTypeList(typeName);
            if (!doc.Contains(typeName, out IniSection? sect))
                return;
            sect!.Clear();  // lazy to filter comments, just clean all.
            for (int i = 0; i < tlist.Length; i++)
                sect.Add(i.ToString(), tlist[i]);
        }
    }
}

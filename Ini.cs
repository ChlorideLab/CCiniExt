﻿using System.Text;

namespace Chloride.CCINIExt
{
    public class Ini
    {
        private List<IniItem> Header = new();

        /*// no need to make anything linear, sections were just like trees.
        private List<string> sections = new();*/
        // fxxking dictionary just linear !!!!!!!
        private List<IniSection> Raw = new();

        public Ini() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // To support GB2312.

        public IniSection this[string sec]
        {
            get => Raw[GetSectionIndex(sec)];
            set
            {
                value.Name = sec; // this["ssks"] = new("ddtms")? NO FXXKING WAY

                var hs = GetSectionIndex(sec);
                if (hs != -1)
                    Raw[hs] = value;
                else
                    Raw.Add(value);
            }
        }

        public int GetSectionIndex(string section)
        {
            foreach (var i in Raw)
            {
                if (i.Name == section)
                    return Raw.IndexOf(i);
            }
            return -1;
        }
        public bool HasKey(string section, string key)
        {
            var hs = GetSectionIndex(section);
            return hs != -1 && Raw[hs].ContainsKey(key, out _);
        }

        // wouldn't replace the old one.
        public void AddNew(string sect)
        {
            if (GetSectionIndex(sect) != -1)
                return;
            Raw.Add(new(sect));
        }
        public void Remove(string sect)
        {
            var hs = GetSectionIndex(sect);
            if (hs == -1)
                return;
            Raw.RemoveAt(hs);
        }
        public void Rename(string _old, string _new)
        {
            if (_old == _new || GetSectionIndex(_new) != -1)
                throw new ArgumentException($"Section {_new} already exists");
            Raw[GetSectionIndex(_old)].Name = _new;
        }

        public IniValue GetValue(string sect, string key) 
            => HasKey(sect, key) ? Raw[GetSectionIndex(sect)][key] : null;
        public string[] GetTypeList(string sect)
        {
            var hs = GetSectionIndex(sect);
            return hs != -1 ? Raw[hs].Values().Select(i => i.ToString()).ToArray() : Array.Empty<string>();
        }
        public void SetValue<T>(string sect, string key, T value) where T : notnull
        {
            AddNew(sect);
            Raw[GetSectionIndex(sect)][key] = value.ToString();
        }

        public void Clear() => Raw.Clear();
        public void Sort() => Raw.Sort((x, y) => x.CompareTo(y));
        public void Sort(IComparer<IniSection>? condExpr) => Raw.Sort(condExpr);

        public void Load(FileInfo[] paths)
        {
            foreach (var i in paths)
            {
                if (i.Exists)
                {
                    using var fs = i.OpenRead();
                    ParseStream(new StreamReader(fs));
                }
            }
        }
        public void Load(FileInfo[] paths, string encoding = "utf-8")
        {
            foreach (var i in paths)
            {
                if (i.Exists)
                {
                    using var fs = i.OpenRead();
                    ParseStream(new StreamReader(fs, Encoding.GetEncoding(encoding)));
                }
            }
        }
        public void Save(string dest, string codec = "utf-8", bool space = false)
        {
            using var fs = new FileInfo(dest).Open(FileMode.Create, FileAccess.Write, FileShare.Read);
            {
                var sw = new StreamWriter(fs, Encoding.GetEncoding(codec));
                foreach (var i in Header)
                    sw.WriteLine(i);
                foreach (var i in Raw)
                {
                    sw.WriteLine(i.ToString());
                    foreach (var j in i)
                        sw.WriteLine(j.ToString(space));
                }
                sw.Flush();
            }
        }
        private void ParseStream(StreamReader stream)
        // too Python. needs rewritten.
        {
            int cur, max, diff = 0;
            var sections = new List<string>();
            if (Raw.Count == 0)
                cur = max = -1;
            else
                cur = max = Raw.Count - 1;

            while (!stream.EndOfStream)
            {
                var i = stream.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(i))
                {
                    if (cur == -1)
                        Header.Add(new(null, null, null));
                    else
                        Raw[cur].Add(new(null, null, null));
                    continue;
                }

                switch (i[0])
                {
                    case '[':
                        var sect = i.Split(';', 2);
                        var curSect = sect[0].Split(':', 2).Select(i => i.Trim()[1..^1]).ToArray();
                        string? curDesc = sect.Length == 2 ? sect[1].TrimEnd() : null;

                        if (sections.Contains(curSect[0]))
                            cur = sections.IndexOf(curSect[0]);
                        else
                        {
                            sections.Add(curSect[0]);
                            Raw.Add(new(
                                curSect[0],
                                (curSect.Length > 1) ? (sections.Contains(curSect[1]) ? Raw[sections.IndexOf(curSect[1])] : new(curSect[1])) : null,
                                curDesc
                            ));
                            cur = ++max;
                        }
                        break;
                    case ';':
                        if (cur == -1)
                            Header.Add(new(null, null, i[(i.IndexOf(';') + 1)..].TrimEnd()));
                        else
                            Raw[cur].Add(new(null, null, i[(i.IndexOf(';') + 1)..].TrimEnd()));
                        break;
                    default:
                        var spDesc = i.Split(';', 2);
                        var spPair = spDesc[0].Split('=', 2).Select(i => i.Trim()).ToArray();
                        if (spPair[0] == "+")
                            spPair[0] = $"+{diff++}";
                        Raw[cur].Add(spPair[0], spPair[1], spDesc.Length > 1 ? spDesc[1] : null);
                        break;
                }
            }

            stream.Close();
        }
    }
}
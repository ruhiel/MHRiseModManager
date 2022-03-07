using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MHRiseModManager.Utils
{
    public static class Utility
    {
        public static void ExtractFile(string path, string dir)
        {
            ZipFile.ExtractToDirectory(path, dir);
        }

        public static void CompressionFile(string dir, string path)
        {
            if(File.Exists(path))
            {
                File.Delete(path);
            }

            ZipFile.CreateFromDirectory(dir, path);
        }

        public static Dictionary<string, Dictionary<string, string>> ReadIni(string file)
        {
            using (var reader = new StreamReader(file))
            {
                var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
                var regexSection = new Regex(@"^\s*\[(?<section>[^\]]+)\].*$", RegexOptions.Singleline | RegexOptions.CultureInvariant);
                var regexNameValue = new Regex(@"^\s*(?<name>[^=]+)=(?<value>.*?)(\s+;(?<comment>.*))?$", RegexOptions.Singleline | RegexOptions.CultureInvariant);
                var currentSection = string.Empty;

                // セクション名が明示されていない先頭部分のセクション名を""として扱う
                sections[string.Empty] = new Dictionary<string, string>();

                for (; ; )
                {
                    var line = reader.ReadLine();

                    if (line == null)
                        break;

                    // 空行は読み飛ばす
                    if (line.Length == 0)
                        continue;

                    // コメント行は読み飛ばす
                    if (line.StartsWith(";", StringComparison.Ordinal))
                        continue;
                    else if (line.StartsWith("#", StringComparison.Ordinal))
                        continue;

                    var matchNameValue = regexNameValue.Match(line);

                    if (matchNameValue.Success)
                    {
                        // name=valueの行
                        sections[currentSection][matchNameValue.Groups["name"].Value.Trim()] = matchNameValue.Groups["value"].Value.Trim();
                        continue;
                    }

                    var matchSection = regexSection.Match(line);

                    if (matchSection.Success)
                    {
                        // [section]の行
                        currentSection = matchSection.Groups["section"].Value;

                        if (!sections.ContainsKey(currentSection))
                            sections[currentSection] = new Dictionary<string, string>();

                        continue;
                    }
                }

                return sections;
            }
        }
    }
}

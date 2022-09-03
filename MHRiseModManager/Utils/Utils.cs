using SevenZip;
using SharpCompress.Readers;
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
        /// ディレクトリをコピーする。
        /// </summary>
        /// <param name="sourceDirName">コピーするディレクトリ</param>
        /// <param name="destDirName">コピー先のディレクトリ</param>
        public static void CopyDirectory(string sourceDirName, string destDirName)
        {
            // コピー先のディレクトリがないかどうか判定する
            if (!Directory.Exists(destDirName))
            {
                // コピー先のディレクトリを作成する
                Directory.CreateDirectory(destDirName);
            }

            // コピー元のディレクトリの属性をコピー先のディレクトリに反映する
            File.SetAttributes(destDirName, File.GetAttributes(sourceDirName));

            // ディレクトリパスの末尾が「\」でないかどうかを判定する
            if (!destDirName.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                // コピー先のディレクトリ名の末尾に「\」を付加する
                destDirName = destDirName + Path.DirectorySeparatorChar;
            }

            // コピー元のディレクトリ内のファイルを取得する
            var files = Directory.GetFiles(sourceDirName);
            foreach (string file in files)
            {
                // コピー元のディレクトリにあるファイルをコピー先のディレクトリにコピーする
                File.Copy(file, destDirName + Path.GetFileName(file), true);
            }

            // コピー元のディレクトリのサブディレクトリを取得する
            var dirs = Directory.GetDirectories(sourceDirName);
            foreach (string dir in dirs)
            {
                // コピー元のディレクトリのサブディレクトリで自メソッド（CopyDirectory）を再帰的に呼び出す
                CopyDirectory(dir, destDirName + Path.GetFileName(dir));
            }
        }

        public static string GetOrCreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        public static void CleanDirectory(string path)
        {
            if (!Directory.Exists(path)) return;

            var di = new DirectoryInfo(path);
            foreach (var f in di.GetFiles())
            {
                f.Delete();
            }

            foreach (var d in di.GetDirectories())
            {
                d.Delete(true);
            }
        }

        public static void CleanDirectoryOnlyDirectory(string path)
        {
            if (!Directory.Exists(path)) return;

            var di = new DirectoryInfo(path);

            foreach (var d in di.GetDirectories())
            {
                d.Delete(true);
            }
        }

        public static bool IsEmptyDirectory(string path)
        {
            if (!Directory.Exists(path)) return false;

            try
            {
                var entries = Directory.GetFileSystemEntries(path);
                return entries == null || entries.Length == 0;
            }
            catch
            {
                return false;
            }
        }

        public static IEnumerable<List<T>> Re<T>(List<T> arg)
        {
            for(int i = arg.Count; i > 0; i--)
            {
                yield return arg.GetRange(0, i);
            }
        }

        public static void ExtractFile(string path, string dir)
        {
            if(!File.Exists(path))
            {
                return;
            }
            if(Path.GetExtension(path) == ".zip")
            {
                ZipFile.ExtractToDirectory(path, dir);
            }
            else if(Path.GetExtension(path) == ".7z")
            {
                SevenZipBase.SetLibraryPath("7z.dll");
                var extractor = new SevenZipExtractor(path);
                extractor.ExtractArchive(dir);
            }
            else
            {
                using (Stream stream = File.OpenRead(path))
                {
                    var reader = ReaderFactory.Open(stream);
                    while (reader.MoveToNextEntry())
                    {
                        if (!reader.Entry.IsDirectory)
                        {
                            reader.WriteEntryToDirectory(dir);
                        }
                    }
                }
            }
        }

        public static void CompressionFile(string dir, string path)
        {
            FileSafeDelete(path);

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
        public static void DirectorySafeDelete(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path);
            }
        }

        public static void DirectorySafeDelete(string path, bool recursive)
        {
            if(Directory.Exists(path))
            {
                Directory.Delete(path, recursive);
            }
        }

        public static void FileSafeDelete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static string GetPathWithoutExtension(string path)
        {
            var extension = Path.GetExtension(path);
            if (string.IsNullOrEmpty(extension))
            {
                return path;
            }
            return path.Replace(extension, string.Empty);
        }

        /// <summary>
        /// 引数のリスト（何らかの名称のリスト）から、重複する要素を抽出する。
        /// </summary>
        /// <param name="list">何らかの名称のリスト。</param>
        /// <returns>重複している要素のリスト。</returns>
        public static List<T> FindDuplication<T>(IEnumerable<T> list)
        {
            // 要素名でGroupByした後、グループ内の件数が2以上（※重複あり）に絞り込み、
            // 最後にIGrouping.Keyからグループ化に使ったキーを抽出している。
            var duplicates = list.GroupBy(name => name).Where(name => name.Count() > 1)
                .Select(group => group.Key).ToList();

            return duplicates;
        }
    }
}

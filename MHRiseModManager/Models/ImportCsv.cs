using CsvHelper.Configuration.Attributes;

namespace MHRiseModManager.Models
{
    public class ImportCsv
    {
        [Index(0), Name("名前")]
        public string Name { get; set; }
        [Index(1), Name("URL")]
        public string Url { get; set; }
        [Index(2), Name("メモ")]
        public string Memo { get; set; }
        [Index(3), Name("バージョン")]
        public string Version { get; set; }
        [Index(4), Name("ファイルフルパス")]
        public string FullFilePath { get; set; }
    }
}

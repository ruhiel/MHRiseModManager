using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHRiseModManager.Models
{
    public class ImportCsv
    {
        [Index(0)]
        public string Name { get; set; }
        [Index(1)]
        public string Url { get; set; }
        [Index(2)]
        public string Memo { get; set; }
        [Index(3)]
        public string Version { get; set; }
        [Index(4)]
        public string FullFilePath { get; set; }
    }
}

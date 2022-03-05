using MHRiseModManager.Properties;
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHRiseModManager.Models
{
    public class ModListManager
    {
        public ModListManager()
        {
            if (!System.IO.File.Exists(Settings.Default.DataBaseFileName))
            {
                // コネクションを開いてテーブル作成して閉じる  
                using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
                {
                    con.Open();
                    using (SQLiteCommand command = con.CreateCommand())
                    {
                        command.CommandText = "CREATE TABLE IF NOT EXISTS modinfo(" +
                        "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                        "name TEXT NOT NULL," +
                        "status INTEGER NOT NULL," +
                        "filesize INTEGER NOT NULL," +
                        "datecreated TEXT NOT NULL," +
                        "category INTEGER NOT NULL," +
                        "archivefilepath TEXT NOT NULL," +
                        "url TEXT NOT NULL)";

                        command.ExecuteNonQuery();
                    }
                    con.Close();
                }
            }

        }

        public void Insert(string name, long fileSize, Category category, string archiveFilePath, string url, DateTime? dateCreated = null, Status status = Status.未インストール)
        {
            var dt = dateCreated ?? DateTime.Now;

            // コネクションを開いてテーブル作成して閉じる  
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                string sql = $"insert into modinfo (name, status, filesize, datecreated, category, archivefilepath, url) values ('{name}', {(int)status}, {fileSize}, '{dt.ToString("yyyy-MM-dd HH:mm:ss")}', {(int)category}, '{archiveFilePath}', '{url}');";
                SQLiteCommand com = new SQLiteCommand(sql, con);
                com.ExecuteNonQuery();

                con.Close();
            }


        }

        public List<ModInfo> SelectAll()
        {
            List<ModInfo> list = null;
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                using (var context = new DataContext(con))
                {
                    var table = context.GetTable<ModInfo>();
                    list = table.ToList();
                }
                con.Close();
            }

            return list;
        }
    }
}

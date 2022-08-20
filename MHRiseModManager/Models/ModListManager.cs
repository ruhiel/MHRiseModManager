using MHRiseModManager.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.IO;
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
                        "imagefilepath TEXT," +
                        "url TEXT," +
                        "memo TEXT," +
                        "modname TEXT)";

                        command.ExecuteNonQuery();


                    }
                    using (SQLiteCommand command = con.CreateCommand())
                    {
                        command.CommandText = "CREATE TABLE IF NOT EXISTS modinfodetail(" +
                            "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "modinfoid INTEGER NOT NULL," +
                            "path TEXT," +
                            "pakpath TEXT)";

                        command.ExecuteNonQuery();
                    }
                    con.Close();
                }
            }

        }

        public void Insert(string name, long fileSize, string archiveFilePath, string url, string modName = null, string imagefilepath = null, string memo = null, DateTime? dateCreated = null, Status status = Status.未インストール)
        {
            var dt = dateCreated ?? DateTime.Now;

            var mod = new ModInfo(id: 1, name: name, status: status, fileSize: fileSize, dateCreated: dt, category: Category.Lua, archiveFilePath: archiveFilePath, url: url, memo:memo);

            // コネクションを開いてテーブル作成して閉じる  
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                string sql = $"insert into modinfo (name, status, filesize, datecreated, category, archivefilepath, url{(imagefilepath == null ? "" : ", imagefilepath")}{(memo == null ? "" : ", memo")}{(modName == null ? "" : ", modname")}) values ('{name.Replace("'", "''")}', {(int)status}, {fileSize}, '{dt.ToString("yyyy-MM-dd HH:mm:ss")}', {(int)mod.GetNewCategory()}, '{archiveFilePath.Replace("'", "''")}', '{url}'{(imagefilepath == null ? "" : ",'" + imagefilepath.Replace("'", "''") + "'")}{(memo == null ? "" : ", '" + memo + "'")}{(modName == null ? "" : ", '" + modName + "'")});";
                SQLiteCommand com = new SQLiteCommand(sql, con);
                com.ExecuteNonQuery();

                con.Close();
            }

        }

        public List<ModInfo> SelectAll()
        {
            List<ModInfo> list;
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

        public void Install(int id, IEnumerable<string> files, Category? category = null)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                string sql = $"update modinfo set status = {(int)Status.インストール済} where id = {id}";
                SQLiteCommand com = new SQLiteCommand(sql, con);
                com.ExecuteNonQuery();

                foreach (var file in files)
                {
                    sql = $"select count(*) from modinfodetail where modinfoid = {id} and path = '{file}'";
                    com = new SQLiteCommand(sql, con);
                    var record = (long)com.ExecuteScalar();

                    if(record == 0)
                    {
                        sql = $"insert into modinfodetail (modinfoid, path) values ({id}, '{file}')";
                        com = new SQLiteCommand(sql, con);
                        com.ExecuteNonQuery();
                    }
                }

                con.Close();
            }
        }

        public List<ModInfoDetail> SelectModFile(int modInfoId)
        {
            List<ModInfoDetail> list;
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                using (var context = new DataContext(con))
                {
                    var table = context.GetTable<ModInfoDetail>();
                    list = table.ToList().Where(x => x.ModInfoId == modInfoId).ToList();
                }
                con.Close();
            }

            return list;
        }

        public void UpdateStatus(int id, Status status, Category? category = null)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                string sql = $"update modinfo set status = {(int)status} where id = {id}";
                SQLiteCommand com = new SQLiteCommand(sql, con);
                com.ExecuteNonQuery();

                con.Close();
            }
        }

        public void Delete(int id)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                var sql = $"delete from modinfodetail where modinfoid = {id}";
                var com = new SQLiteCommand(sql, con);
                com.ExecuteNonQuery();

                sql = $"delete from modinfo where id = {id}";
                com = new SQLiteCommand(sql, con);
                com.ExecuteNonQuery();

                con.Close();
            }
        }
    }
}

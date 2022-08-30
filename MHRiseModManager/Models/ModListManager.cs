using MHRiseModManager.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace MHRiseModManager.Models
{
    public class ModListManager
    {
        public ModListManager()
        {
            if (!File.Exists(Settings.Default.DataBaseFileName))
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
                        "modname TEXT," +
                        "version TEXT," +
                        "latestversion TEXT," +
                        "modfilebinary BLOB)";

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

        public void Insert(string name, string targetFile, string url, string version, string modName = null, string memo = null, DateTime? dateCreated = null, Status status = Status.未インストール)
        {
            var dt = dateCreated ?? DateTime.Now;

            string archiveFilePath = targetFile.Substring(Environment.CurrentDirectory.Length + 1);

            var fileBinaryFrom = File.ReadAllBytes(targetFile);

            var fileSize = fileBinaryFrom.Length;

            var mod = new ModInfo(id: 1, name: name, status: status, fileSize: fileSize, dateCreated: dt, category: Category.Lua, archiveFilePath: archiveFilePath, url: url, memo: memo, modFileBinary:fileBinaryFrom);

            // コネクションを開いてテーブル作成して閉じる  
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                string sql = $"insert into modinfo (name, status, filesize, datecreated, category, archivefilepath, url{(memo == null ? "" : ", memo")}{(modName == null ? "" : ", modname")}{(version == null ? "" : ", version")}, modfilebinary) values ('{name.Replace("'", "''")}', {(int)status}, {fileSize}, '{dt.ToString("yyyy-MM-dd HH:mm:ss")}', {(int)mod.GetNewCategory()}, '{archiveFilePath.Replace("'", "''")}', '{url}'{(memo == null ? "" : ", '" + memo + "'")}{(modName == null ? "" : ", '" + modName + "'")}{(version == null ? "" : ", '" + version + "'")}, @file_binary);";
                SQLiteCommand com = new SQLiteCommand(sql, con);
                com.Parameters.Add("@file_binary", DbType.Binary).Value = fileBinaryFrom;
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
        public void DeleteDetail(int id)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                var sql = $"delete from modinfodetail where modinfoid = {id}";
                var com = new SQLiteCommand(sql, con);
                com.ExecuteNonQuery();

                con.Close();
            }
        }

        public ModInfo Update(int id, string targetFile)
        {
            string archiveFilePath = targetFile.Substring(Environment.CurrentDirectory.Length + 1);

            var fileBinaryFrom = File.ReadAllBytes(targetFile);

            var fileSize = fileBinaryFrom.Length;

            // コネクションを開いてテーブル作成して閉じる  
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                string sql = $"update modinfo set name = '{Path.GetFileName(archiveFilePath)}', filesize = {(int)fileSize}, archivefilepath = '{archiveFilePath}', modfilebinary = @file_binary where id = {id}";
                var com = new SQLiteCommand(sql, con);
                com.Parameters.Add("@file_binary", DbType.Binary).Value = fileBinaryFrom;
                com.ExecuteNonQuery();

                con.Close();
            }

            return SelectAll().Where(x => x.Id == id).First();
        }

        public ModInfo Update(int id, string name, string url, string memo, string version)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                string sql = $"update modinfo set modname = '{name}', url = '{url}', memo = '{memo}', version = '{version}' where id = {id}";
                var com = new SQLiteCommand(sql, con);
                com.ExecuteNonQuery();

                con.Close();
            }

            return SelectAll().Where(x => x.Id == id).First();
        }

        public ModInfo UpdateLatestVersion(int id, string latestversion)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                string sql = $"update modinfo set latestversion = '{latestversion}' where id = {id}";
                var com = new SQLiteCommand(sql, con);
                com.ExecuteNonQuery();

                con.Close();
            }

            return SelectAll().Where(x => x.Id == id).First();
        }
    }
}

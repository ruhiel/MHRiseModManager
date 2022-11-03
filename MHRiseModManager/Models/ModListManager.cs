using MHRiseModManager.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;

namespace MHRiseModManager.Models
{
    public class ModListManager
    {
        private Compiler _Compiler = new SqliteCompiler();
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

        public int Insert(string name, string targetFile, string url, string version, string modName = null, string memo = null, DateTime? dateCreated = null, Status status = Status.未インストール)
        {
            var dt = dateCreated ?? DateTime.Now;

            string archiveFilePath = targetFile.Substring(Environment.CurrentDirectory.Length + 1);

            var fileBinaryFrom = File.ReadAllBytes(targetFile);

            var fileSize = fileBinaryFrom.Length;

            var mod = new ModInfo(id: 1, name: name, status: status, fileSize: fileSize, dateCreated: dt, category: Category.Lua, archiveFilePath: archiveFilePath, url: url, memo: memo, modFileBinary:fileBinaryFrom);

            // コネクションを開いてテーブル作成して閉じる  
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);

                return db.Query(nameof(ModInfo).ToLower()).Insert(new
                {
                    name = name.Replace("'", "''"),
                    status = (int)status,
                    filesize = fileSize,
                    datecreated = dt.ToString("yyyy-MM-dd HH:mm:ss"),
                    category = (int)mod.GetNewCategory(),
                    archivefilepath = archiveFilePath.Replace("'", "''"),
                    url = url,
                    memo = (memo == null ? "" : memo),
                    modname = (modName == null ? "" : modName),
                    version = (version == null ? "" : version),
                    modfilebinary = fileBinaryFrom,
                });
            }
        }

        private ModInfo Find(int id)
        {
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);
                return db.Query(nameof(ModInfo).ToLower()).Where("id", id).First().Get<ModInfo>();
            }
        }

        public List<ModInfo> SelectAll()
        {
            List<ModInfo> list;
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);
                list = db.Query(nameof(ModInfo).ToLower()).Get<ModInfo>().ToList();
            }

            return list;
        }

        public void Install(int id, IEnumerable<string> files, Category? category = null)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);
                
                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new
                {
                    status = (int)Status.インストール済
                });

                foreach (var file in files)
                {
                    var sql = $"select count(*) from modinfodetail where modinfoid = {id} and path = '{file}'";
                    var com = new SQLiteCommand(sql, connection);
                    var record = (long)com.ExecuteScalar();

                    if(record == 0)
                    {
                        db.Query(nameof(ModInfoDetail).ToLower()).Insert(new
                        {
                            modinfoid = id,
                            path = file,
                        });
                    }
                }
            }
        }

        public List<ModInfoDetail> SelectUninstallModFile(int modInfoId)
        {
            var list = new List<ModInfoDetail>();
            using (var con = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                con.Open();
                var format = @"with checktable as (
                    select detail.path
                    from modinfodetail as detail
                    left join modinfo as info on detail.modinfoid = info.id
                    where info.status = 1 and detail.modinfoid <> {0}
                ) select d.* from modinfodetail as d where d.modinfoid = {0} and not exists (select checktable.path from checktable where checktable.path = d.path)";

                var sql = string.Format(format, modInfoId);
                var com = new SQLiteCommand(sql, con);
                using (var reader = com.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var i = new ModInfoDetail();
                        i.Id = int.Parse(reader["id"].ToString());
                        i.ModInfoId = int.Parse(reader["modinfoid"].ToString());
                        i.Path = DBNull.Value.Equals(reader["path"]) ? null : (string)reader["path"];
                        i.PakPath = DBNull.Value.Equals(reader["pakpath"]) ? null : (string)reader["pakpath"];
                        list.Add(i);
                    }
                }
            }

            return list;
        }

        public List<ModInfoDetail> SelectModFile(int modInfoId)
        {
            List<ModInfoDetail> list;
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);
                list = db.Query(nameof(ModInfoDetail).ToLower()).Where("modinfoid", modInfoId).Get<ModInfoDetail>().ToList();
            }

            return list;
        }

        public int UpdateStatus(int id, Status status, Category? category = null)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);

                return db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new
                {
                    status = (int)status
                });
            }
        }

        public void Delete(int id)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                var db = new QueryFactory(connection, _Compiler);
 
                db.Query(nameof(ModInfoDetail).ToLower()).Where("modinfoid", id).Delete();

                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Delete();
            }
        }
        public int DeleteDetail(int id)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);
                return db.Query(nameof(ModInfoDetail).ToLower()).Where("modinfoid", id).Delete();
            }
        }

        public ModInfo Update(int id, string targetFile)
        {
            string archiveFilePath = targetFile.Substring(Environment.CurrentDirectory.Length + 1);

            var fileBinaryFrom = File.ReadAllBytes(targetFile);

            var fileSize = fileBinaryFrom.Length;

            // コネクションを開いてテーブル作成して閉じる  
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);
                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new
                {
                    name = Path.GetFileName(archiveFilePath),
                    filesize = fileSize,
                    archivefilepath = archiveFilePath,
                    modfilebinary = fileBinaryFrom,
                });

            }

            return Find(id);
        }

        public ModInfo Update(int id, string name, string url, string memo, string version)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);
                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new
                {
                    modname = name,
                    url = url,
                    memo = memo,
                    version = version,
                });
            }

            return Find(id);
        }

        public ModInfo UpdateLatestVersion(int id, string latestversion)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);
                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new
                {
                    latestversion = latestversion,
                });
            }

            return Find(id);
        }
        public void UpdateArchivePath(int id, string archivefilepath)
        {
            // コネクションを開いてテーブル作成して閉じる  
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);
                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new
                {
                    archivefilepath = archivefilepath,
                });
            }
        }

        public void UpdateDetailPath(int id, string path)
        {
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);
                db.Query(nameof(ModInfoDetail).ToLower()).Where("id", id).Update(new
                {
                    path = path,
                });
            }
        }
    }
}

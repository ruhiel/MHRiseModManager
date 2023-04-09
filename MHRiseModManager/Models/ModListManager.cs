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
                using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
                {
                    connection.Open();
                    using (SQLiteCommand command = connection.CreateCommand())
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
                    using (SQLiteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "CREATE TABLE IF NOT EXISTS modinfodetail(" +
                            "id INTEGER PRIMARY KEY AUTOINCREMENT," +
                            "modinfoid INTEGER NOT NULL," +
                            "path TEXT," +
                            "pakpath TEXT)";

                        command.ExecuteNonQuery();
                    }
                }
            }
        }
        public int Insert(string name, string targetFile, string url, string version, string modName = null, string memo = null, DateTime? dateCreated = null, Status status = Status.未インストール)
        {
            Func<QueryFactory, int> action = (db) =>
            {
                var dt = dateCreated ?? DateTime.Now;

                string archiveFilePath = targetFile.Substring(Environment.CurrentDirectory.Length + 1);

                var fileBinaryFrom = File.ReadAllBytes(targetFile);

                var fileSize = fileBinaryFrom.Length;

                var mod = new ModInfo(id: 1, name: name, status: status, fileSize: fileSize, dateCreated: dt, category: Category.Lua, archiveFilePath: archiveFilePath, url: url, memo: memo, modFileBinary: fileBinaryFrom);

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
            };

            return Execute(action);
        }
        private ModInfo Find(int id)
        {
            Func<QueryFactory, ModInfo> action = (db) => db.Query(nameof(ModInfo).ToLower()).Where("id", id).First<ModInfo>();

            return Execute(action);
        }
        public List<ModInfo> SelectAll()
        {
            Func<QueryFactory, List<ModInfo>> action = (db) => db.Query(nameof(ModInfo).ToLower()).Get<ModInfo>().ToList();

            return Execute(action);
        }
        public void Install(int id, IEnumerable<string> files, Category? category = null)
        {
            Action<QueryFactory, SQLiteConnection> action = (db, connection) =>
            {
                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new
                {
                    status = (int)Status.インストール済
                });

                foreach (var file in files)
                {
                    var sql = $"select count(*) from modinfodetail where modinfoid = {id} and path = '{file}'";
                    var com = new SQLiteCommand(sql, connection);
                    var record = (long)com.ExecuteScalar();

                    if (record == 0)
                    {
                        db.Query(nameof(ModInfoDetail).ToLower()).Insert(new
                        {
                            modinfoid = id,
                            path = file,
                        });
                    }
                }
            };

            Execute(action);
        }
        public List<ModInfoDetail> SelectUninstallModFile(int modInfoId)
        {
            var list = new List<ModInfoDetail>();
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var format = @"with checktable as (
                    select detail.path
                    from modinfodetail as detail
                    left join modinfo as info on detail.modinfoid = info.id
                    where info.status = 1 and detail.modinfoid <> {0}
                ) select d.* from modinfodetail as d where d.modinfoid = {0} and not exists (select checktable.path from checktable where checktable.path = d.path)";

                var sql = string.Format(format, modInfoId);
                var com = new SQLiteCommand(sql, connection);
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
            Func<QueryFactory, List<ModInfoDetail>> action = (db) => db.Query(nameof(ModInfoDetail).ToLower()).Where("modinfoid", modInfoId).Get<ModInfoDetail>().ToList();

            return Execute(action);
        }
        public int UpdateStatus(int id, Status status, Category? category = null)
        {
            Func<QueryFactory, int> action = (db) => db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new { status = (int)status });

            return Execute(action);
        }
        public void Delete(int id)
        {
            Action<QueryFactory> action = (db) =>
            {
                db.Query(nameof(ModInfoDetail).ToLower()).Where("modinfoid", id).Delete();

                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Delete();
            };

            Execute(action);
        }
        public int DeleteDetail(int id)
        {
            Func<QueryFactory, int> action = (db) => db.Query(nameof(ModInfoDetail).ToLower()).Where("modinfoid", id).Delete();

            return Execute(action);
        }

        public ModInfo Update(int id, string targetFile)
        {
            Func<QueryFactory, ModInfo> action = (db) =>
            {
                string archiveFilePath = targetFile.Substring(Environment.CurrentDirectory.Length + 1);

                var fileBinaryFrom = File.ReadAllBytes(targetFile);

                var fileSize = fileBinaryFrom.Length;

                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new
                {
                    name = Path.GetFileName(archiveFilePath),
                    filesize = fileSize,
                    archivefilepath = archiveFilePath,
                    modfilebinary = fileBinaryFrom,
                });

                return Find(id);
            };

            return Execute(action);
        }

        public ModInfo Update(int id, string name, string url, string memo, string version)
        {
            Func<QueryFactory, ModInfo> action = (db) =>
            {
                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new
                {
                    modname = name,
                    url = url,
                    memo = memo,
                    version = version,
                });

                return Find(id);
            };

            return Execute(action);
        }

        public ModInfo UpdateLatestVersion(int id, string latestversion)
        {
            Func<QueryFactory, ModInfo> action = (db) =>
            {
                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new
                {
                    latestversion = latestversion,
                });

                return Find(id);
            };

            return Execute(action);
        }

        public void UpdateArchivePath(int id, string archivefilepath)
        {
            Action<QueryFactory> action = (db) =>
            {
                db.Query(nameof(ModInfo).ToLower()).Where("id", id).Update(new
                {
                    archivefilepath = archivefilepath,
                });
            };

            Execute(action);
        }

        public void UpdateDetailPath(int id, string path)
        {
            Action<QueryFactory> action = (db) =>
            {
                db.Query(nameof(ModInfoDetail).ToLower()).Where("id", id).Update(new
                {
                    path = path,
                });
            };

            Execute(action);
        }
        private void Execute(Action<QueryFactory> action)
        {
            // コネクションを開いて実行
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);

                action.Invoke(db);
            }
        }
        private Type Execute<Type>(Func<QueryFactory, Type> action)
        {
            // コネクションを開いて実行
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);

                return action.Invoke(db);
            }
        }
        private void Execute(Action<QueryFactory, SQLiteConnection> action)
        {
            // コネクションを開いて実行
            using (var connection = new SQLiteConnection($"Data Source={Settings.Default.DataBaseFileName}"))
            {
                connection.Open();
                var db = new QueryFactory(connection, _Compiler);

                action.Invoke(db, connection);
            }
        }
    }
}

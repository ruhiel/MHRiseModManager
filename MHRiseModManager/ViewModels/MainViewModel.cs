﻿using MHRiseModManager.Models;
using MHRiseModManager.Properties;
using MHRiseModManager.Utils;
using MHRiseModManager.Views;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.Threading.Tasks;
using System.Reflection;
using CsvHelper;
using System.Globalization;
using System.Text;
using CsvHelper.Configuration;
using System.Net.Http;
using ControlzEx.Theming;

namespace MHRiseModManager.ViewModels
{
    public class MainViewModel
    {
        private CompositeDisposable Disposable { get; } = new CompositeDisposable();
        public ReactiveCommand ShownCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CloseCommand { get; } = new ReactiveCommand();
        public ReactiveCommand InstallCommand { get; } = new ReactiveCommand();
        public ReactiveCommand UnInstallCommand { get; } = new ReactiveCommand();
        public ObservableCollection<ModFileTree> ModFileTree { get; set; } = new ObservableCollection<ModFileTree>();
        public ReactiveProperty<string> GameDirectoryPath { get; } = new ReactiveProperty<string>(Settings.Default.GameDirectoryPath);
        public ReactiveProperty<string> NowModPath { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<long?> NowModSize { get; } = new ReactiveProperty<long?>();
        public ReactiveProperty<string> ModImagePath { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<bool> Installable { get; } = new ReactiveProperty<bool>();
        public ReactiveProperty<bool> UnInstallable { get; } = new ReactiveProperty<bool>();
        public ReactiveProperty<string> NowMemo { get; } = new ReactiveProperty<string>();
        public ReactiveCommand<DragEventArgs> FileDropCommand { get; private set; }
        public ReactiveCommand<(object sender, EventArgs args)> SelectionChanged { get; private set; } = new ReactiveCommand<(object sender, EventArgs args)>();
        private ModListManager _ModListManager = new ModListManager();
        private ModInfo _NowSelectModInfo;
        public ObservableCollection<ModInfo> ModInfoList { get; set; } = new ObservableCollection<ModInfo>();
        public ReactiveProperty<string> NowModURL { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> NowVersion { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> NowLatestVersion { get; } = new ReactiveProperty<string>();
        public ReactiveCommand<object> NavigateCommand { get; } = new ReactiveCommand<Object>();
        public ReactiveCommand<object> SelectGameFolderCommand { get; } = new ReactiveCommand<Object>();
        public ReactiveCommand<object> OpenGameFolderCommand { get; } = new ReactiveCommand<Object>();
        public IDialogCoordinator MahAppsDialogCoordinator { get; set; }
        public AsyncReactiveCommand DeleteCommand { get; } = new AsyncReactiveCommand();
        public AsyncReactiveCommand BackUpCommand { get; } = new AsyncReactiveCommand();
        public AsyncReactiveCommand RestoreCommand { get; } = new AsyncReactiveCommand();
        public ReactiveCommand CSVTemplateOutPutCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CSVImportCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CSVExportCommand { get; } = new ReactiveCommand();
        public ReactiveCommand AllClearCommand { get; } = new ReactiveCommand();
        public ReactiveCommand AllInstallCommand { get; } = new ReactiveCommand();
        public ReactiveCommand AllUnInstallCommand { get; } = new ReactiveCommand();
        public ReactiveCommand MenuCloseCommand { get; } = new ReactiveCommand();
        public ReactiveCommand SettingCommand { get; } = new ReactiveCommand();

        public ReactiveCommand VersionCheckCommand { get; } = new ReactiveCommand();
        public MainViewModel()
        {
            ThemeManager.Current.ChangeThemeBaseColor(Application.Current, Settings.Default.BaseColor);
            ThemeManager.Current.ChangeThemeColorScheme(Application.Current, Settings.Default.ColorScheme);

            FileDropCommand = new ReactiveCommand<DragEventArgs>().AddTo(Disposable);
            FileDropCommand.Subscribe(e =>
            {
                if (e != null)
                {
                    OnFileDrop(e);
                }
            });

            SelectionChanged.Subscribe(x =>
            {
                var datagrid = (DataGrid)x.sender;

                var modInfo = (ModInfo)datagrid.SelectedItem;

                if (modInfo != null)
                {
                    _NowSelectModInfo = modInfo;

                    NowModPath.Value = modInfo.ArchiveFilePath;

                    NowModSize.Value = modInfo.FileSize;

                    Installable.Value = modInfo.Status == Status.未インストール;

                    UnInstallable.Value = modInfo.Status == Status.インストール済;

                    NowMemo.Value = modInfo.Memo;

                    NowModURL.Value = modInfo.URL;

                    NowVersion.Value = modInfo.Version;

                    NowLatestVersion.Value = modInfo.LatestVersion;

                    if (string.IsNullOrEmpty(modInfo.ImageFilePath))
                    {
                        ModImagePath.Value = Path.Combine(Environment.CurrentDirectory, Settings.Default.ResourceDirectoryName, "no_image_yoko.jpg");
                    }
                    else
                    {
                        ModImagePath.Value = Path.Combine(Environment.CurrentDirectory, Settings.Default.ImageCacheDirectoryName, modInfo.ImageFilePath);
                    }

                    ModFileTree.Clear();

                    foreach (var item in modInfo.GetFileTree())
                    {
                        ModFileTree.Add(item);
                    }
                }
            });

            InstallCommand.Subscribe(e =>
            {
                Install(_NowSelectModInfo);

                ModFileListReflesh();
            });

            UnInstallCommand.Subscribe(e =>
            {
                Uninstall(_NowSelectModInfo);

                ModFileListReflesh();
            });

            CloseCommand.Subscribe(e =>
            {
                CleanCache();

                Utility.CleanDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));

                Settings.Default.GameDirectoryPath = GameDirectoryPath.Value;
                Settings.Default.Save();

                Disposable.Dispose();

            });

            NavigateCommand.Subscribe(e =>
            {
                System.Diagnostics.Process.Start(NowModURL.Value);
            });

            SelectGameFolderCommand.Subscribe(e =>
            {
                using (var ofd = new System.Windows.Forms.OpenFileDialog() { FileName = "MonsterHunterRise.exe", Filter = "Folder|*.exe", CheckFileExists = false })
                {
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        GameDirectoryPath.Value = Path.GetDirectoryName(ofd.FileName);
                        Settings.Default.Save();
                    }
                }
            });

            OpenGameFolderCommand.Subscribe(e =>
            {
                if (!string.IsNullOrEmpty(GameDirectoryPath.Value))
                {
                    System.Diagnostics.Process.Start(GameDirectoryPath.Value);
                }
            });

            BackUpCommand.Subscribe(async e =>
            {
                ProgressDialogController objController = await MahAppsDialogCoordinator.ShowProgressAsync(this, Assembly.GetEntryAssembly().GetName().Name, "バックアップ中");

                objController.Minimum = 0;

                await Task.Run(() =>
                {
                    var path = Utility.GetOrCreateDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName, Path.GetRandomFileName()));

                    var di = new DirectoryInfo(Settings.Default.GameDirectoryPath);

                    foreach (var f in di.GetFiles())
                    {
                        File.Copy(f.FullName, Path.Combine(path, f.Name));
                    }

                    foreach (var d in di.GetDirectories())
                    {
                        Utility.CopyDirectory(d.FullName, Path.Combine(path, d.Name));
                    }

                    var archiveName = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip";

                    var backUpDir = Utility.GetOrCreateDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.BackUpDirectoryName));

                    Utility.CompressionFile(path, Path.Combine(backUpDir, archiveName));

                });

                await objController.CloseAsync();

                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "バックアップを完了しました。");
            });

            RestoreCommand.Subscribe(async e =>
            {
                var backUpDir = Utility.GetOrCreateDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.BackUpDirectoryName));

                if (!Directory.EnumerateFileSystemEntries(backUpDir).Any())
                {
                    await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "バックアップファイルがありません。先にバックアップしてください。");
                    return;
                }

                var objController = await MahAppsDialogCoordinator.ShowProgressAsync(this, Assembly.GetEntryAssembly().GetName().Name, "リストア中");

                await Task.Run(() =>
                {
                    Utility.CleanDirectory(Settings.Default.GameDirectoryPath);

                    var di = new DirectoryInfo(backUpDir);
                    var fi = di.GetFiles().OrderByDescending(x => x.LastWriteTime).Last();

                    var targetDir = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(fi.Name));

                    Utility.ExtractFile(fi.FullName, targetDir);

                    Utility.CopyDirectory(targetDir, Settings.Default.GameDirectoryPath);

                    Utility.DirectorySafeDelete(targetDir, true);
                });

                await objController.CloseAsync();

                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "リストアを完了しました。");
            });
            SettingCommand.Subscribe(e =>
            {
                var dialog = new SettingWindow();
                dialog.ShowDialog();
            });

            DeleteCommand.Subscribe(async e =>
            {
                var metroDialogSettings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "はい",
                    NegativeButtonText = "いいえ",
                    AnimateHide = true,
                    AnimateShow = true,
                    ColorScheme = MetroDialogColorScheme.Theme,
                };

                var diagResult = await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modの登録を削除します。よろしいですか？", MessageDialogStyle.AffirmativeAndNegative, metroDialogSettings);

                if (MessageDialogResult.Negative == diagResult)
                {
                    return;
                }

                Delete(_NowSelectModInfo);

                ModFileListReflesh();

                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modの登録を削除しました。");
            });
            CSVTemplateOutPutCommand.Subscribe(e =>
            {
                var filePath = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), "csv"));

                var config = new CsvConfiguration(new CultureInfo("ja-JP", false))
                {
                    HasHeaderRecord = true,
                    Encoding = Encoding.GetEncoding("Shift_JIS")
                };

                var records = new List<ImportCsv>();
                using (var writer = new StreamWriter(filePath, false, Encoding.GetEncoding("Shift_JIS")))
                {
                    using (var csv = new CsvWriter(writer, config))
                    {
                        csv.WriteRecords(records);
                    }
                }

                System.Diagnostics.Process.Start(filePath);
            });
            CSVImportCommand.Subscribe(async e =>
            {
                if (string.IsNullOrEmpty(GameDirectoryPath.Value))
                {
                    await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "ゲームフォルダを設定してください。処理を終了します。");
                    return;
                }

                var dropFile = string.Empty;

                using (var ofd = new System.Windows.Forms.OpenFileDialog() { FileName = "", Filter = "CSVファイル|*.csv" })
                {
                    if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    {
                        return;
                    }

                    dropFile = ofd.FileName;
                }

                await CsvImportProc(dropFile);

            });

            CSVExportCommand.Subscribe(e =>
            {
                var filePath = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), "csv"));

                var config = new CsvConfiguration(new CultureInfo("ja-JP", false))
                {
                    HasHeaderRecord = true,
                    Encoding = Encoding.GetEncoding("Shift_JIS")
                };

                var records = _ModListManager.SelectAll().Select(x => new CSVRecord() { Name = x.ModName, Url = x.URL, Memo = x.Memo, Version = x.Version });
                using (var writer = new StreamWriter(filePath, false, Encoding.GetEncoding("Shift_JIS")))
                {
                    using (var csv = new CsvWriter(writer, config))
                    {
                        csv.WriteRecords(records);
                    }
                }

                System.Diagnostics.Process.Start(filePath);
            });

            AllClearCommand.Subscribe(async e =>
            {
                var metroDialogSettings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "はい",
                    NegativeButtonText = "いいえ",
                    AnimateHide = true,
                    AnimateShow = true,
                    ColorScheme = MetroDialogColorScheme.Theme,
                };

                var diagResult = await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modの登録を一括削除します。よろしいですか？", MessageDialogStyle.AffirmativeAndNegative, metroDialogSettings);

                if (MessageDialogResult.Negative == diagResult)
                {
                    return;
                }

                var records = _ModListManager.SelectAll().Where(x => x.Status == Status.未インストール).ToList();
                var controller = await MahAppsDialogCoordinator.ShowProgressAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modの一括削除中...");
                var i = 1;
                controller.Minimum = i;
                controller.Maximum = records.Count;

                await Task.Run(() =>
                {
                    foreach (var modInfo in records)
                    {
                        Delete(modInfo);
                        controller.SetProgress(i);
                        var rate = 100 * i / records.Count;
                        controller.SetMessage($"Modの一括削除中 {i} / {records.Count} ({rate}%)...");
                        i++;
                    }
                });

                ModFileListReflesh();

                await controller.CloseAsync();

                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modの登録を削除しました。");
            });

            AllInstallCommand.Subscribe(async e =>
            {
                var records = _ModListManager.SelectAll().Where(x => x.Status == Status.未インストール).ToList();
                var controller = await MahAppsDialogCoordinator.ShowProgressAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modの一括インストール中...");
                var i = 1;
                controller.Minimum = i;
                controller.Maximum = records.Count;

                await Task.Run(() =>
                {
                    foreach (var modInfo in records)
                    {
                        Install(modInfo);
                        controller.SetProgress(i);
                        var rate = 100 * i / records.Count;
                        controller.SetMessage($"Modの一括インストール中 {i} / {records.Count} ({rate}%)...");
                        i++;
                    }
                });

                ModFileListReflesh();

                await controller.CloseAsync();

                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modを一括インストールしました。");
            });

            AllUnInstallCommand.Subscribe(async e =>
            {
                var records = _ModListManager.SelectAll().Where(x => x.Status == Status.インストール済).ToList();
                var controller = await MahAppsDialogCoordinator.ShowProgressAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modの一括アンインストール中...");
                var i = 1;
                controller.Minimum = i;
                controller.Maximum = records.Count;

                await Task.Run(() =>
                {
                    foreach (var modInfo in records)
                    {
                        Uninstall(modInfo);
                        controller.SetProgress(i);
                        var rate = 100 * i / records.Count;
                        controller.SetMessage($"Modの一括アンインストール中 {i} / {records.Count} ({rate}%)...");
                        i++;
                    }
                });

                ModFileListReflesh();

                await controller.CloseAsync();

                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modを一括アンインストールしました。");
            });
            ShownCommand.Subscribe(async e =>
            {
                if (Settings.Default.StartUpVersionCheck)
                {
                    await CheckVersion();
                }
            });
            VersionCheckCommand.Subscribe(async e =>
            {
                await CheckVersion();
                ModFileListReflesh();
            });
            MenuCloseCommand.Subscribe(x => ((Window)x).Close());

            ModFileListReflesh();
        }
        private async Task CsvImportProc(string dropFile)
        {
            var records = await GetCSVRecord(dropFile);
            if (records == null)
            {
                return;
            }
            var controller = await MahAppsDialogCoordinator.ShowProgressAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modの新規登録中...");

            var i = 1;
            controller.Minimum = i;
            controller.Maximum = records.Count;

            await Task.Run(() =>
            {
                foreach (var record in records)
                {
                    ModRegistSync(modName: record.Name, url: record.Url, memo: record.Memo, version: record.Version, dropFile: record.FullFilePath);
                    controller.SetProgress(i);
                    var rate = 100 * i / records.Count;
                    controller.SetMessage($"Modの新規登録中 {i} / {records.Count} ({rate}%)...");
                    i++;
                }

                Utility.CleanDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));
            });

            ModFileListReflesh();

            await controller.CloseAsync();

            await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modを新規登録しました。");
        }

        private async Task<List<ImportCsv>> GetCSVRecord(string dropFile)
        {
            List<ImportCsv> records = null;
            var config = new CsvConfiguration(new CultureInfo("ja-JP", false))
            {
                HasHeaderRecord = true,
                Encoding = Encoding.GetEncoding("Shift_JIS")
            };
            try
            {
                using (var reader = new StreamReader(dropFile, Encoding.GetEncoding("Shift_JIS")))
                {
                    using (var csv = new CsvReader(reader, config))
                    {
                        records = csv.GetRecords<ImportCsv>().ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, ex.Message + "\n処理を終了します。");

                return null;
            }

            var noexists = records.Where(x => !File.Exists(x.FullFilePath)).Select(x => x.FullFilePath).OrderBy(x => x);

            if (noexists.Any())
            {
                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "存在しないファイルが含まれています。処理を終了します。\n" + string.Join("\n", noexists));

                return null;
            }

            var duplicate = Utility.FindDuplication(records.Select(x => x.FullFilePath)).OrderBy(x => x);

            if (duplicate.Any())
            {
                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "重複したファイルが含まれています。処理を終了します。\n" + string.Join("\n", duplicate));

                return null;
            }

            return records;
        }

        private async Task CheckVersion()
        {
            var checkList = await VersionCheckListAsync();

            if (checkList.Count > 0)
            {
                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, $"以下の新しいバージョンのModが公開されています。\n {string.Join("\n", checkList.Select(x => $"{x.Item1} ({x.Item2}) → ({x.Item3})"))}");
            }
        }
        private async Task<List<Tuple<string, string, string>>> VersionCheckListAsync()
        {
            var controller = await MahAppsDialogCoordinator.ShowProgressAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modバージョンチェック中...");

            var checkList = new List<Tuple<string, string, string>>();
            var list = _ModListManager.SelectAll();

            var i = 1;
            controller.Minimum = i;
            controller.Maximum = list.Count;


            foreach (var modInfo in list)
            {
                var version = await GetVersion(modInfo.URL);

                _ModListManager.UpdateLatestVersion(modInfo.Id, version);

                if (!string.IsNullOrEmpty(version) && string.Compare(version, modInfo.Version) > 0)
                {
                    checkList.Add(Tuple.Create(modInfo.ModName, modInfo.Version, version));
                }
                controller.SetProgress(i);
                var rate = 100 * i / list.Count;
                controller.SetMessage($"Modバージョンチェック中 {i} / {list.Count} ({rate}%)...");
                i++;
            }

            await controller.CloseAsync();

            return checkList;
        }

        private async Task<string> GetVersion(string url)
        {
            var list = new List<string>();
            var client = new HttpClient();
            var res = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

            using (var httpStream = await res.Content.ReadAsStreamAsync())
            {
                var parser = new AngleSharp.Html.Parser.HtmlParser();
                var doc = parser.ParseDocument(httpStream);

                var h2Nodes = doc.QuerySelectorAll("div.titlestat");

                foreach (var h2Node in h2Nodes)
                {
                    if (h2Node.TextContent == "Version")
                    {
                        list.Add(h2Node.NextElementSibling.TextContent.Replace("\r", "").Replace("\n", "").Replace(" ", ""));
                    }
                }
            }

            return list.Any() ? list.Max() : string.Empty;
        }

        private void Delete(ModInfo modinfo)
        {
            var file = Path.Combine(Environment.CurrentDirectory, modinfo.ArchiveFilePath);

            var dir = Path.Combine(Environment.CurrentDirectory, Path.GetDirectoryName(modinfo.ArchiveFilePath), Path.GetFileNameWithoutExtension(modinfo.ArchiveFilePath));

            Utility.FileSafeDelete(file);

            Utility.DirectorySafeDelete(dir, true);

            _ModListManager.Delete(modinfo.Id);
        }

        private static void CleanCache()
        {
            var cacheDir = Path.Combine(Environment.CurrentDirectory, Settings.Default.ModsCacheDirectoryName);

            Utility.CleanDirectory(cacheDir);

        }

        private async void OnFileDrop(DragEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(GameDirectoryPath.Value))
                {
                    await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "ゲームフォルダを設定してください。処理を終了します。");
                    return;
                }

                if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    return;
                }

                var dropFiles = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (dropFiles == null)
                {
                    return;
                }

                var dropFile = dropFiles[0];

                var dialog = new InstallDialog();

                var returnModel = dialog.DataContext as InstallDialogViewModel;

                returnModel.PakMode.Value = dropFile.EndsWith("pak");

                returnModel.PakFileName.Value = Path.GetFileName(dropFile);

                dialog.ShowDialog();

                if(!returnModel.Result)
                {
                    return;
                }

                var controller = await MahAppsDialogCoordinator.ShowProgressAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modの新規登録中...");

                var modName = string.IsNullOrEmpty(returnModel.Name.Value) ? null : returnModel.Name.Value;

                await ModRegist(modName: modName, url: returnModel.URL.Value, memo: returnModel.Memo.Value, version: returnModel.Version.Value, dropFile: dropFile, pakFileName:returnModel.PakFileName.Value);

                Utility.CleanDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));

                ModFileListReflesh();

                await controller.CloseAsync();

                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modを新規登録しました。");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        private void ModRegistSync(string modName, string url, string memo, string version, string dropFile)
        {
            dropFile = PreProcess(dropFile);

            if (dropFile == null)
            {
                return;
            }

            var cacheDir = Utility.GetOrCreateDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.ModsCacheDirectoryName));

            var targetFileName = Path.GetFileName(dropFile);
            var targetFile = Path.Combine(cacheDir, targetFileName);

            File.Copy(dropFile, targetFile, true);

            _ModListManager.Insert(name: targetFileName, targetFile: targetFile, url: url, memo: memo, modName: modName, version: version);
        }

        private async Task ModRegist(string modName, string url, string memo, string version, string dropFile, string pakFileName = null)
        {
            dropFile = PreProcess(dropFile, pakFileName);

            if (dropFile == null)
            {
                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "REFramework対応MOD以外のファイル形式です。\n処理を終了します。");

                return;
            }

            var cacheDir = Utility.GetOrCreateDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.ModsCacheDirectoryName));

            var targetFileName = Path.GetFileName(dropFile);
            var targetFile = Path.Combine(cacheDir, targetFileName);

            File.Copy(dropFile, targetFile, true);

            _ModListManager.Insert(name: targetFileName, targetFile: targetFile, url: url, memo: memo, modName: modName, version: version);
        }

        private string PreProcess(string dropFile, string pakFileName = null)
        {
            var resultFile = dropFile;

            // pakファイル対応
            if (!string.IsNullOrEmpty(pakFileName))
            {
                var dir = Path.GetDirectoryName(dropFile);
                resultFile = Path.Combine(dir, pakFileName);
                File.Move(dropFile, resultFile);

                return resultFile;
            }

            var tempDir = Utility.GetOrCreateDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));

            var targetFile = Path.Combine(tempDir, Path.GetFileName(dropFile));
            File.Copy(dropFile, targetFile, true);

            var mod = new ModInfo(id: 1, name: "", status: Status.未インストール, fileSize: new FileInfo(targetFile).Length, dateCreated: DateTime.Now, category: Category.Lua, archiveFilePath: targetFile, modFileBinary: null, url: "", memo: "");

            if (mod.GetNewCategory() == Category.Lua && !mod.GetFileTree().Any(x => !x.IsFile && x.Name == "reframework"))
            {
                var tempFileName = Path.GetRandomFileName();
                // Luaかつreframeworkがない
                var reframeworkDir = Utility.GetOrCreateDirectory(Path.Combine(tempDir, tempFileName, "reframework"));

                var srcDir = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(dropFile));

                var di = new DirectoryInfo(srcDir);

                if (di.GetFiles().Any())
                {
                    foreach (var f in di.GetFiles())
                    {
                        File.Move(f.FullName, Path.Combine(reframeworkDir, Path.GetFileName(f.FullName)));
                    }
                }

                if (di.GetDirectories().Any())
                {
                    foreach (var d in di.GetDirectories())
                    {
                        Directory.Move(d.FullName, Path.Combine(reframeworkDir, d.Name));
                    }
                }

                resultFile = Path.Combine(Path.GetDirectoryName(targetFile), $"{Path.GetFileNameWithoutExtension(targetFile)}.zip");

                Utility.CompressionFile(Path.Combine(tempDir, tempFileName), resultFile);

                Utility.DirectorySafeDelete(Path.Combine(tempDir, tempFileName), true);

            }
            else if (mod.GetNewCategory() == Category.その他)
            {
                return null;
            }

            return resultFile;
        }

        private void ModFileListReflesh()
        {
            ModInfoList.Clear();

            _ModListManager.SelectAll().ForEach(x =>
            {
                ModInfoList.Add(new ModInfo(id: x.Id, name: x.Name, status: x.Status, fileSize: x.FileSize, dateCreated: x.DateCreated, category: x.Category, archiveFilePath: x.ArchiveFilePath, url: x.URL, imageFilePath: x.ImageFilePath, memo: x.Memo, modName: x.ModName, modFileBinary: x.ModFileBinary, version: x.Version, latestversion:x.LatestVersion, mainViewModel: this));
            });

            NowModPath.Value = string.Empty;

            NowModSize.Value = null;

            ModImagePath.Value = string.Empty;

            Installable.Value = false;

            UnInstallable.Value = false;

            NowMemo.Value = string.Empty;

            NowModURL.Value = string.Empty;

            NowVersion.Value = string.Empty;

            NowLatestVersion.Value = string.Empty;

            ModFileTree.Clear();
        }
        private void Install(ModInfo modInfo)
        {
            var files = new List<string>();

            foreach (var item in modInfo.GetAllTree().Where(x => x.IsFile))
            {
                var itemPath = item.Path;
                var dir = Path.GetDirectoryName(itemPath);

                var targetDir = Utility.GetOrCreateDirectory(Path.Combine(Settings.Default.GameDirectoryPath, dir));

                var srcFile = modInfo.Category == Category.Pak ? modInfo.ExtractArchivePath : Path.Combine(modInfo.ExtractArchivePath, itemPath);

                var targetFile = string.Empty;

                targetFile = Path.Combine(Settings.Default.GameDirectoryPath, itemPath);

                File.Copy(srcFile, targetFile, true);

                files.Add(itemPath);
            }

            _ModListManager.Install(modInfo.Id, files, modInfo.Category);
        }

        private void Uninstall(ModInfo modInfo)
        {
            var set = new HashSet<string>();
            var list = _ModListManager.SelectUninstallModFile(modInfo.Id);
            foreach (var item in list)
            {
                set.Add(Path.GetDirectoryName(item.Path));

                var path = Path.Combine(Settings.Default.GameDirectoryPath, item.Path);

                Utility.FileSafeDelete(path);
            }

            var setDir = new HashSet<string>();

            foreach (var item in set)
            {
                var dirList = item.Split(Path.DirectorySeparatorChar).ToList();
                foreach (var a in Utility.Re(dirList))
                {
                    setDir.Add(Path.Combine(a.ToArray()));
                }
            }

            setDir.OrderByDescending(a => a.Length).ToList().ForEach(x =>
            {
                var dir = Path.Combine(Settings.Default.GameDirectoryPath, x);
                if (Utility.IsEmptyDirectory(dir) && !dir.Equals(Settings.Default.GameDirectoryPath))
                {
                    Utility.DirectorySafeDelete(dir);
                }
            });

            _ModListManager.UpdateStatus(modInfo.Id, Status.未インストール, modInfo.Category);
        }

        public async void OnRowUpdate(ModInfo modInfo)
        {
            var newModInfo = modInfo;

            var dropFile = string.Empty;

            // アーカイブファイル更新
            using (var ofd = new System.Windows.Forms.OpenFileDialog() { FileName = "" })
            {
                if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                dropFile = ofd.FileName;
            }

            var dialog = new InstallDialog();

            var returnModel = dialog.DataContext as InstallDialogViewModel;

            returnModel.URL.Value = modInfo.URL;
            returnModel.Name.Value = modInfo.ModName;
            returnModel.Memo.Value = modInfo.Memo;
            returnModel.Version.Value = modInfo.Version;

            dialog.ShowDialog();

            if (!returnModel.Result)
            {
                return;
            }

            // アンインストール
            Uninstall(modInfo);

            _ModListManager.DeleteDetail(modInfo.Id);

            var archiveFilePath = Path.Combine(Path.Combine(Environment.CurrentDirectory, modInfo.ArchiveFilePath));

            Utility.FileSafeDelete(archiveFilePath);

            Utility.DirectorySafeDelete(Utility.GetPathWithoutExtension(archiveFilePath), true);

            dropFile = PreProcess(dropFile);

            if (dropFile == null)
            {
                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "REFramework対応MOD以外のファイル形式です。\n処理を終了します。");

                return;
            }

            await Task.Run(() =>
            {
                var cacheDir = Utility.GetOrCreateDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.ModsCacheDirectoryName));

                var targetFileName = Path.GetFileName(dropFile);
                var targetFile = Path.Combine(cacheDir, targetFileName);

                File.Copy(dropFile, targetFile, true);

                newModInfo = _ModListManager.Update(id: modInfo.Id, targetFile: targetFile);

                Utility.CleanDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));
            });

            // インストール
            Install(newModInfo);

            _ModListManager.Update(id: modInfo.Id, name: returnModel.Name.Value, url: returnModel.URL.Value, memo: returnModel.Memo.Value, version: returnModel.Version.Value);

            ModFileListReflesh();
        }
        public async void OnRowEdit(ModInfo modInfo)
        {
            var dialog = new InstallDialog();

            var returnModel = dialog.DataContext as InstallDialogViewModel;

            returnModel.URL.Value = modInfo.URL;
            returnModel.Name.Value = modInfo.ModName;
            returnModel.Memo.Value = modInfo.Memo;
            returnModel.Version.Value = modInfo.Version;
            returnModel.PakMode.Value = modInfo.Category == Category.Pak;
            returnModel.PakFileName.Value = Path.GetFileName(modInfo.ArchiveFilePath);

            var oldFilePakName = Path.GetFileName(modInfo.ArchiveFilePath);

            dialog.ShowDialog();

            if (!returnModel.Result)
            {
                return;
            }

            _ModListManager.Update(id: modInfo.Id, name: returnModel.Name.Value, url: returnModel.URL.Value, memo: returnModel.Memo.Value, version: returnModel.Version.Value);

            if(modInfo.Category == Category.Pak && !oldFilePakName.Equals(returnModel.PakFileName.Value))
            {
                RenamePakFile(modInfo, returnModel.PakFileName.Value);
            }

            ModFileListReflesh();

            await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modを更新しました。");
        }

        private void RenamePakFile(ModInfo modInfo, string newFileName)
        {
            var oldArchiveFilePath = modInfo.ArchiveFilePath;

            var detail = _ModListManager.SelectModFile(modInfo.Id).First();

            var oldFileName = detail.Path;

            var gameDir = GameDirectoryPath.Value;

            var newArchiveFilePath = Path.Combine(Path.GetDirectoryName(oldArchiveFilePath), newFileName);

            var newArchiveFullPath = Path.Combine(Environment.CurrentDirectory, newArchiveFilePath);

            var oldArchiveFullPath = Path.Combine(Environment.CurrentDirectory, oldArchiveFilePath);

            File.Move(Path.Combine(gameDir, oldFileName), Path.Combine(gameDir, newFileName));

            File.Move(oldArchiveFullPath, newArchiveFullPath);

            _ModListManager.UpdateDetailPath(detail.Id, newFileName);

            _ModListManager.UpdateArchivePath(modInfo.Id, newArchiveFilePath);
        }
    }
}

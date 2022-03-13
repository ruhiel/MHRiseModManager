using MHRiseModManager.Models;
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
using MahApps.Metro.Controls;
using System.Threading.Tasks;
using System.Reflection;

namespace MHRiseModManager.ViewModels
{
    public class MainViewModel
    {
        private CompositeDisposable Disposable { get; } = new CompositeDisposable();
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
        public ReactiveCommand<(object sender, EventArgs args)> SelectionChanged { get; private set;} = new ReactiveCommand<(object sender, EventArgs args)>();
        private ModListManager _ModListManager = new ModListManager();
        private ModInfo _NowSelectModInfo;
        public ObservableCollection<ModInfo> ModInfoList { get; set; } = new ObservableCollection<ModInfo>();
        public ReactiveProperty<string> NowModURL { get; } = new ReactiveProperty<string>();
        public ReactiveCommand<Object> NavigateCommand { get; } = new ReactiveCommand<Object>();
        public ReactiveCommand<object> OpenGameFolderCommand { get; } = new ReactiveCommand<Object>();
        public IDialogCoordinator MahAppsDialogCoordinator { get; set; }
        public AsyncReactiveCommand DeleteCommand { get; } = new AsyncReactiveCommand();
        public AsyncReactiveCommand BackUpCommand { get; } = new AsyncReactiveCommand();
        public AsyncReactiveCommand RestoreCommand { get; } = new AsyncReactiveCommand();
        public ReactiveCommand MenuCloseCommand { get; } = new ReactiveCommand();
        public AsyncReactiveCommand SettingResetCommand { get; } = new AsyncReactiveCommand();
        public MainViewModel()
        {
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

                if(modInfo != null)
                {
                    _NowSelectModInfo = modInfo;

                    NowModPath.Value = modInfo.Name;

                    NowModSize.Value = modInfo.FileSize;

                    Installable.Value = modInfo.Status == Status.未インストール;

                    UnInstallable.Value = modInfo.Status == Status.インストール済;

                    NowMemo.Value = modInfo.Memo;

                    NowModURL.Value = modInfo.URL;

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
                var files = new List<string>();

                foreach (var item in _NowSelectModInfo.GetAllTree().Where(x => x.IsFile))
                {
                    var itemPath = item.Path;
                    var dir = Path.GetDirectoryName(itemPath);

                    var targetDir = Utility.GetOrCreateDirectory(Path.Combine(Settings.Default.GameDirectoryPath, dir));

                    var srcFile = Path.Combine(_NowSelectModInfo.ExtractArchivePath, itemPath);

                    var targetFile = string.Empty;

                    if(_NowSelectModInfo.Category == Category.Pak)
                    {
                        var num = _ModListManager.SelectLastPakNo();

                        var fileName = $"re_chunk_000.pak.patch_{_NowSelectModInfo.Id:000000}.pak";

                        targetFile = Path.Combine(Settings.Default.GameDirectoryPath, fileName);

                        itemPath = Path.Combine(Path.GetDirectoryName(itemPath), fileName);
                    }
                    else
                    {
                        targetFile = Path.Combine(Settings.Default.GameDirectoryPath, itemPath);
                    }

                    File.Copy(srcFile, targetFile, true);

                    files.Add(itemPath);
                }

                _ModListManager.Install(_NowSelectModInfo.Id, files, _NowSelectModInfo.Category);
                
                if (_NowSelectModInfo.Category == Category.Pak)
                {
                    // Pakファイル名洗い替え
                    _ModListManager.RefleshPakFileName();
                }

                ModFileListReflesh();
            });

            UnInstallCommand.Subscribe(e =>
            {
                var set = new HashSet<string>();
                var list = _ModListManager.SelectModFile(_NowSelectModInfo.Id);
                foreach(var item in list)
                {
                    set.Add(Path.GetDirectoryName(item.Path));

                    File.Delete(Path.Combine(Settings.Default.GameDirectoryPath, item.Path));
                }

                var setDir = new HashSet<string>();

                foreach(var item in set)
                {
                    var dirList = item.Split(Path.DirectorySeparatorChar).ToList();
                    foreach( var a in Utility.Re(dirList) )
                    {
                        setDir.Add(Path.Combine(a.ToArray()));
                    }
                }

                setDir.OrderByDescending(a => a.Length).ToList().ForEach(x =>
                {
                    var dir = Path.Combine(Settings.Default.GameDirectoryPath, x);
                    if(Utility.IsEmptyDirectory(dir) && !dir.Equals(Settings.Default.GameDirectoryPath))
                    {
                        Directory.Delete(dir);
                    }
                });

                _ModListManager.UpdateStatus(_NowSelectModInfo.Id, Status.未インストール, _NowSelectModInfo.Category);

                if(_NowSelectModInfo.Category == Category.Pak)
                {
                    // Pakファイル名洗い替え
                    _ModListManager.RefleshPakFileName();
                }

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

            OpenGameFolderCommand.Subscribe(e =>
            {
                var browser = new System.Windows.Forms.FolderBrowserDialog();

                browser.Description = "フォルダーを選択してください";

                if (browser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    GameDirectoryPath.Value = browser.SelectedPath;
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
                    var max = di.GetFiles().Length + di.GetDirectories().Length;
                    objController.Maximum = max;
                    var i = 1;
                    foreach (var f in di.GetFiles())
                    {
                        File.Copy(f.FullName, Path.Combine(path, f.Name));
                        objController.SetProgress(i++);
                        objController.SetMessage($"バックアップ中:{i} / {max}");
                    }

                    foreach (var d in di.GetDirectories())
                    {
                        Utility.CopyDirectory(d.FullName, Path.Combine(path, d.Name));
                        objController.SetProgress(i++);
                        objController.SetMessage($"バックアップ中:{i} / {max}");
                    }

                    var archiveName = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}.zip";

                    var backUpDir = Utility.GetOrCreateDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.BackUpDirectoryName));

                    objController.SetMessage($"圧縮中");
                    Utility.CompressionFile(path, Path.Combine(backUpDir, archiveName));

                    objController.CloseAsync();
                });
            
                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "バックアップを完了しました。");
            });

            RestoreCommand.Subscribe(async e =>
            {
                var backUpDir = Utility.GetOrCreateDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.BackUpDirectoryName));

                if(!Directory.EnumerateFileSystemEntries(backUpDir).Any())
                {
                    await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "バックアップファイルがありません。先にバックアップしてください。");
                    return;
                }

                ProgressDialogController objController = await MahAppsDialogCoordinator.ShowProgressAsync(this, Assembly.GetEntryAssembly().GetName().Name, "リストア中");

                await Task.Run(() =>
                {
                    Utility.CleanDirectory(Settings.Default.GameDirectoryPath);

                    var di = new DirectoryInfo(backUpDir);
                    var fi = di.GetFiles().OrderByDescending(x => x.LastWriteTime).Last();

                    var targetDir = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(fi.Name));

                    Utility.ExtractFile(fi.FullName, targetDir);

                    Utility.CopyDirectory(targetDir, Settings.Default.GameDirectoryPath);

                    Directory.Delete(targetDir, true);

                    objController.CloseAsync();
                });


                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "リストアを完了しました。");
            });

            SettingResetCommand.Subscribe(async e =>
            {
                var metroDialogSettings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "はい",
                    NegativeButtonText = "いいえ",
                    AnimateHide = true,
                    AnimateShow = true,
                    ColorScheme = MetroDialogColorScheme.Theme,
                };

                var diagResult = await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "設定の初期化は全てのModのアンインストール後をおすすめします。\r\nよろしいですか？", MessageDialogStyle.AffirmativeAndNegative, metroDialogSettings);

                if(MessageDialogResult.Negative == diagResult)
                {
                    return;
                }

                File.Delete(Path.Combine(Environment.CurrentDirectory, Settings.Default.DataBaseFileName));

                Utility.CleanDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.ModsCacheDirectoryName));

                Utility.CleanDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.ImageCacheDirectoryName));

                Utility.CleanDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));

                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "設定を初期化しました。アプリケーションを再起動します。");

                Application.Current.Shutdown();
                System.Windows.Forms.Application.Restart();
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

                var file = Path.Combine(Environment.CurrentDirectory, _NowSelectModInfo.ArchiveFilePath);

                var dir = Path.Combine(Environment.CurrentDirectory, Path.GetDirectoryName(_NowSelectModInfo.ArchiveFilePath), Path.GetFileNameWithoutExtension(_NowSelectModInfo.ArchiveFilePath));

                File.Delete(file);

                Directory.Delete(dir, true);

                _ModListManager.Delete(_NowSelectModInfo.Id);

                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modの登録を削除しました。");

                ModFileListReflesh();
            });

            MenuCloseCommand.Subscribe(x => ((Window)x).Close());

            ModFileListReflesh();
        }

        private static void CleanCache()
        {
            var dir = Environment.CurrentDirectory;
            var cacheDir = Path.Combine(dir, Settings.Default.ModsCacheDirectoryName);

            var di = new DirectoryInfo(cacheDir);
            foreach (var d in di.GetDirectories())
            {
                d.Delete(true);
            }
        }

        private async void OnFileDrop(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var dropFiles = e.Data.GetData(DataFormats.FileDrop) as string[];

            if (dropFiles == null)
            {
                return;
            }

            var dialog = new InstallDialog();

            dialog.ShowDialog();

            var controller = await MahAppsDialogCoordinator.ShowProgressAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modの新規登録中...");

            await Task.Run(() =>
            {
                var returnModel = dialog.DataContext as InstallDialogViewModel;

                var dropFile = dropFiles[0];
                string imagefile = null;

                (dropFile, imagefile) = PreProcess(dropFile);

                var cacheDir = Utility.GetOrCreateDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.ModsCacheDirectoryName));

                var targetFileName = Path.GetFileName(dropFile);
                var targetFile = Path.Combine(cacheDir, targetFileName);
                var modName = string.IsNullOrEmpty(returnModel.Name.Value) ? null : returnModel.Name.Value;

                File.Copy(dropFile, targetFile, true);

                Utility.CleanDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));

                _ModListManager.Insert(name: targetFileName, fileSize: new FileInfo(targetFile).Length, archiveFilePath: targetFile.Substring(Environment.CurrentDirectory.Length + 1), url: returnModel.URL.Value, memo: returnModel.Memo.Value, imagefilepath: imagefile, modName: modName);

                ModFileListReflesh();

                Utility.CleanDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));

                controller.CloseAsync();
            });

            await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modを新規登録しました。");
        }

        private (string, string) PreProcess(string dropFile)
        {
            var resultFile = dropFile;
            var imageFile = string.Empty;

            var tempDir = Utility.GetOrCreateDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));

            var targetFile = Path.Combine(tempDir, Path.GetFileName(dropFile));
            File.Copy(dropFile, targetFile, true);

            var mod = new ModInfo(id: 1, name: "", status: Status.未インストール, fileSize: new FileInfo(targetFile).Length, dateCreated: DateTime.Now, category: Category.Lua, archiveFilePath: targetFile, url: "", memo:"");

            if(mod.GetNewCategory() == Category.Lua && !mod.GetFileTree().Any(x => !x.IsFile && x.Name == "reframework"))
            {
                var tempFileName = Path.GetRandomFileName();
                // Luaかつreframeworkがない
                var reframeworkDir = Utility.GetOrCreateDirectory(Path.Combine(tempDir, tempFileName, "reframework"));

                var srcDir = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(dropFile));

                var di = new DirectoryInfo(srcDir);

                if(di.GetFiles().Any())
                {
                    foreach (var f in di.GetFiles())
                    {
                        File.Move(f.FullName, Path.Combine(reframeworkDir, Path.GetFileName(f.FullName)));
                    }
                }

                if(di.GetDirectories().Any())
                {
                    foreach (var d in di.GetDirectories())
                    {
                        Directory.Move(d.FullName, Path.Combine(reframeworkDir, d.Name));
                    }
                }
                
                resultFile = Path.Combine(Path.GetDirectoryName(targetFile), $"{Path.GetFileNameWithoutExtension(targetFile)}.zip");
                
                Utility.CompressionFile(Path.Combine(tempDir, tempFileName), resultFile);

                Directory.Delete(Path.Combine(tempDir, tempFileName), true);

            }
            else if (mod.GetAllTree().Any(x => x.IsFile && x.Name == "modinfo.ini"))
            {
                // 画像ファイル処理
                var iniFile = mod.GetAllTree().First(x => x.IsFile && x.Name == "modinfo.ini");
                var iniFilePath = Path.Combine(Path.GetDirectoryName(targetFile), Path.GetFileNameWithoutExtension(targetFile), iniFile.Path);

                var dic = Utility.ReadIni(iniFilePath);
                var ssFileName = dic.First().Value["screenshot"];

                var srcSSPath = Path.Combine(Path.GetDirectoryName(iniFilePath), ssFileName);

                var cacheDir = Utility.GetOrCreateDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.ImageCacheDirectoryName));

                var dstSSPath = Path.Combine(cacheDir, $"{Path.GetFileNameWithoutExtension(targetFile)}{Path.GetExtension(ssFileName)}");

                File.Copy(srcSSPath, dstSSPath, true);

                imageFile = Path.GetFileName(dstSSPath);

                // アーカイブ処理
                var srcDir = Path.GetDirectoryName(iniFilePath);

                var di = new DirectoryInfo(srcDir);
                
                var tempFileName = Path.GetRandomFileName();

                var targetDir = Utility.GetOrCreateDirectory(Path.Combine(tempDir, tempFileName));

                if (di.GetFiles().Any())
                {
                    foreach (var f in di.GetFiles().Where(x => x.Name != ssFileName && x.Name != "modinfo.ini"))
                    {
                        File.Move(f.FullName, Path.Combine(targetDir, Path.GetFileName(f.FullName)));
                    }
                }

                if (di.GetDirectories().Any())
                {
                    foreach (var d in di.GetDirectories())
                    {
                        Directory.Move(d.FullName, Path.Combine(targetDir, d.Name));
                    }
                }

                resultFile = Path.Combine(Path.GetDirectoryName(targetFile), $"{Path.GetFileNameWithoutExtension(targetFile)}.zip");

                Utility.CompressionFile(targetDir, resultFile);
            }
            else if (Category.Pak == mod.GetNewCategory() && mod.GetAllTree().Where(x => x.IsFile).Count() > 1)
            {
                var tempFileName = Path.GetRandomFileName();

                var fileInfo = mod.GetFileTree().Find(x => Path.GetExtension(x.Path) == ".pak");

                var targetDir = Utility.GetOrCreateDirectory(Path.Combine(tempDir, tempFileName));

                var srcFile = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(dropFile), fileInfo.Path);

                File.Move(srcFile, Path.Combine(targetDir, Path.GetFileName(fileInfo.Path)));

                resultFile = Path.Combine(Path.GetDirectoryName(targetFile), $"{Path.GetFileNameWithoutExtension(targetFile)}.zip");

                Utility.CompressionFile(targetDir, resultFile);

            }

            return (resultFile, imageFile);
        }

        private void ModFileListReflesh()
        {
            ModInfoList.Clear();

            _ModListManager.SelectAll().ForEach(x =>
            {
                ModInfoList.Add(new ModInfo(id:x.Id, name:x.Name, status:x.Status, fileSize:x.FileSize, dateCreated:x.DateCreated, category:x.Category, archiveFilePath:x.ArchiveFilePath, url:x.URL, imageFilePath:x.ImageFilePath, memo:x.Memo, modName:x.ModName, mainViewModel:this));
            });

            NowModPath.Value = string.Empty;

            NowModSize.Value = null;

            ModImagePath.Value = string.Empty;

            Installable.Value = false;

            UnInstallable.Value = false;

            NowMemo.Value = string.Empty;

            NowModURL.Value = string.Empty;

            ModFileTree.Clear();
        }

        public void OnRowUpdate(ModInfo modInfo)
        {
            // TODO: MODの更新
        }
    }
}

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
        public ReactiveCommand<object> NavigateCommand { get; } = new ReactiveCommand<Object>();
        public ReactiveCommand<object> SelectGameFolderCommand { get; } = new ReactiveCommand<Object>();

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
                Install(_NowSelectModInfo);
            });

            UnInstallCommand.Subscribe(e =>
            {
                Uninstall(_NowSelectModInfo);
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
                using (var ofd = new System.Windows.Forms.OpenFileDialog() { FileName = "SelectFolder", Filter = "Folder|.", CheckFileExists = false })
                {
                    if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        GameDirectoryPath.Value = Path.GetDirectoryName(ofd.FileName);
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

                if(!Directory.EnumerateFileSystemEntries(backUpDir).Any())
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

                    Directory.Delete(targetDir, true);
                });

                await objController.CloseAsync();

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
            try
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

                var returnModel = dialog.DataContext as InstallDialogViewModel;

                var dropFile = dropFiles[0];
                string imagefile = null;

                (dropFile, imagefile) = PreProcess(dropFile);

                if(dropFile == null)
                {
                    await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "REFramework対応MOD以外のファイル形式です。\n処理を終了します。");

                    return;
                }

                await Task.Run(() =>
                {
                    var cacheDir = Utility.GetOrCreateDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.ModsCacheDirectoryName));

                    var targetFileName = Path.GetFileName(dropFile);
                    var targetFile = Path.Combine(cacheDir, targetFileName);
                    var modName = string.IsNullOrEmpty(returnModel.Name.Value) ? null : returnModel.Name.Value;

                    File.Copy(dropFile, targetFile, true);

                    _ModListManager.Insert(name: targetFileName, fileSize: new FileInfo(targetFile).Length, archiveFilePath: targetFile.Substring(Environment.CurrentDirectory.Length + 1), url: returnModel.URL.Value, memo: returnModel.Memo.Value, imagefilepath: imagefile, modName: modName);

                    Utility.CleanDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));
                });

                ModFileListReflesh();

                await controller.CloseAsync();

                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modを新規登録しました。");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
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
            else if (mod.GetNewCategory() == Category.その他)
            {
                return (null, null);
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
        private void Install(ModInfo modInfo)
        {
            var files = new List<string>();

            foreach (var item in modInfo.GetAllTree().Where(x => x.IsFile))
            {
                var itemPath = item.Path;
                var dir = Path.GetDirectoryName(itemPath);

                var targetDir = Utility.GetOrCreateDirectory(Path.Combine(Settings.Default.GameDirectoryPath, dir));

                var srcFile = Path.Combine(modInfo.ExtractArchivePath, itemPath);

                var targetFile = string.Empty;

                targetFile = Path.Combine(Settings.Default.GameDirectoryPath, itemPath);

                File.Copy(srcFile, targetFile, true);

                files.Add(itemPath);
            }

            _ModListManager.Install(modInfo.Id, files, modInfo.Category);

            ModFileListReflesh();
        }

        private void Uninstall(ModInfo modInfo, bool detailDelete = false)
        {
            var set = new HashSet<string>();
            var list = _ModListManager.SelectModFile(modInfo.Id);
            foreach (var item in list)
            {
                set.Add(Path.GetDirectoryName(item.Path));

                File.Delete(Path.Combine(Settings.Default.GameDirectoryPath, item.Path));
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
                    Directory.Delete(dir);
                }
            });

            _ModListManager.UpdateStatus(modInfo.Id, Status.未インストール, modInfo.Category);

            if (detailDelete)
            {
                _ModListManager.DeleteDetail(modInfo.Id);
            }
            else
            {
                ModFileListReflesh();
            }
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

            // アンインストール
            Uninstall(modInfo, true);

            dropFile = PreProcess(dropFile).Item1;

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

                newModInfo = _ModListManager.Update(id: modInfo.Id, name: targetFileName, fileSize: new FileInfo(targetFile).Length, archiveFilePath: targetFile.Substring(Environment.CurrentDirectory.Length + 1));

                Utility.CleanDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));
            });

            // インストール
            Install(newModInfo);

            await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "Modを更新しました。");
        }
    }
}

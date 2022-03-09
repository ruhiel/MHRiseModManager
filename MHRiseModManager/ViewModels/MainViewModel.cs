using MHRiseModManager.Models;
using MHRiseModManager.Properties;
using MHRiseModManager.Utils;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MHRiseModManager.ViewModels
{
    public class MainViewModel
    {
        private CompositeDisposable Disposable { get; } = new CompositeDisposable();
        public ReactiveCommand CloseCommand { get; } = new ReactiveCommand();
        public ReactiveCommand InstallCommand { get; } = new ReactiveCommand();

        public ReactiveCommand UnstallCommand { get; } = new ReactiveCommand();

        public ObservableCollection<ModFileTree> ModFileTree { get; set; } = new ObservableCollection<ModFileTree>();

        public ReactiveProperty<string> GameDirectoryPath { get; } = new ReactiveProperty<string>(Settings.Default.GameDirectoryPath);

        public ReactiveProperty<string> NowModPath { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> NowModSize { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> ModImagePath { get; } = new ReactiveProperty<string>();

        public ReactiveProperty<bool> Installable { get; } = new ReactiveProperty<bool>();

        public ReactiveProperty<bool> Unstallable { get; } = new ReactiveProperty<bool>();

        public ReactiveCommand<DragEventArgs> FileDropCommand { get; private set; }

        public ReactiveCommand<(object sender, EventArgs args)> SelectionChanged { get; private set;} = new ReactiveCommand<(object sender, EventArgs args)>();

        private ModListManager _ModListManager = new ModListManager();

        private ModInfo _NowSelectModInfo;

        public ObservableCollection<ModInfo> ModInfoList { get; set; } = new ObservableCollection<ModInfo>();
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

                    var archive = modInfo.ExtractArchivePath;

                    NowModPath.Value = modInfo.Name;

                    NowModSize.Value = modInfo.FileSize.ToString();

                    Installable.Value = modInfo.Status == Status.未インストール;

                    Unstallable.Value = modInfo.Status == Status.インストール済;

                    if(string.IsNullOrEmpty(modInfo.ImageFilePath))
                    {
                        ModImagePath.Value = Path.Combine(Environment.CurrentDirectory, Settings.Default.ImageCacheDirectoryName, "no_image_yoko.jpg"); 
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

                    var targetDir = Path.Combine(Settings.Default.GameDirectoryPath, dir);

                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    var srcFile = Path.Combine(_NowSelectModInfo.ExtractArchivePath, itemPath);

                    var targetFile = string.Empty;

                    if(_NowSelectModInfo.Category == Category.Pak)
                    {
                        var num = _ModListManager.SelectLastPakNo();

                        var fileName = $"re_chunk_000.pak.patch_{num:000}.pak";

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

                _ModListManager.Install(_NowSelectModInfo.Id, files);

                ModFileListReflesh();
            });

            CloseCommand.Subscribe(e =>
            {
                CleanCache();

                CleanTemp();

                Settings.Default.GameDirectoryPath = GameDirectoryPath.Value;
                Settings.Default.Save();

                Disposable.Dispose();

            });

            ModFileListReflesh();

        }

        private void CleanTemp()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName);
            var di = new DirectoryInfo(tempDir);
            foreach (var f in di.GetFiles())
            {
                f.Delete();
            }

            foreach (var d in di.GetDirectories())
            {
                d.Delete(true);
            }
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

        private void OnFileDrop(DragEventArgs e)
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

            var dropFile = dropFiles[0];
            string imagefile = null;

            (dropFile, imagefile) = PreProcess(dropFile);

            var dir = Environment.CurrentDirectory;
            var cacheDir = Path.Combine(dir, Settings.Default.ModsCacheDirectoryName);

            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            var targetFileName = Path.GetFileName(dropFile);
            var targetFile = Path.Combine(cacheDir, targetFileName);

            File.Copy(dropFile, targetFile, true);

            _ModListManager.Insert(name: targetFileName, fileSize: new FileInfo(targetFile).Length, archiveFilePath: targetFile, url:"https://", imagefilepath: imagefile);

            ModFileListReflesh();

            CleanDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));
        }

        private void CleanDirectory(string path)
        {
            var di = new DirectoryInfo(path);

            foreach (var f in di.GetFiles())
            {
                File.Delete(f.FullName);
            }

            foreach (var d in di.GetDirectories())
            {
                Directory.Delete(d.FullName, true);
            }
        }

        private (string, string) PreProcess(string dropFile)
        {
            var resultFile = dropFile;
            var imageFile = string.Empty;

            var tempDir = Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName);
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            var targetFile = Path.Combine(tempDir, Path.GetFileName(dropFile));
            File.Copy(dropFile, targetFile, true);

            var mod = new ModInfo(id: 1, name: "", status: Status.未インストール, fileSize: new FileInfo(targetFile).Length, dateCreated: DateTime.Now, category: Category.Lua, archiveFilePath: targetFile, url: "");


            if(mod.GetNewCategory() == Category.Lua && !mod.GetFileTree().Any(x => !x.IsFile && x.Name == "reframework"))
            {
                var tempFileName = Path.GetRandomFileName();
                // Luaかつreframeworkがない
                var reframeworkDir = Path.Combine(tempDir, tempFileName, "reframework");
                Directory.CreateDirectory(reframeworkDir);

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

                var cacheDir = Path.Combine(Environment.CurrentDirectory, Settings.Default.ImageCacheDirectoryName);
                
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                var dstSSPath = Path.Combine(cacheDir, $"{Path.GetFileNameWithoutExtension(targetFile)}{Path.GetExtension(ssFileName)}");

                File.Copy(srcSSPath, dstSSPath, true);

                imageFile = Path.GetFileName(dstSSPath);

                // アーカイブ処理
                var srcDir = Path.GetDirectoryName(iniFilePath);

                var di = new DirectoryInfo(srcDir);
                
                var tempFileName = Path.GetRandomFileName();

                var targetDir = Path.Combine(tempDir, tempFileName);
                Directory.CreateDirectory(targetDir);

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

                var targetDir = Path.Combine(tempDir, tempFileName);
                Directory.CreateDirectory(targetDir);

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
                ModInfoList.Add(new ModInfo(id:x.Id, name:x.Name, status:x.Status, fileSize:x.FileSize, dateCreated:x.DateCreated, category:x.Category, archiveFilePath:x.ArchiveFilePath, url:x.URL, imageFilePath:x.ImageFilePath, mainViewModel:this));
            });

            NowModPath.Value = string.Empty;

            NowModSize.Value = string.Empty;

            ModImagePath.Value = string.Empty;

            Installable.Value = false;

            Unstallable.Value = false;

            ModFileTree.Clear();
        }

        public void OnRowUpdate(ModInfo modInfo)
        {
            // TODO: MODの更新
        }
    }
}

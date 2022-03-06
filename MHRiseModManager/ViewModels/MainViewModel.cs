using MHRiseModManager.Models;
using MHRiseModManager.Properties;
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

                foreach(var item in _NowSelectModInfo.GetAllTree().Where(x => x.IsFile))
                {
                    var dir = Path.GetDirectoryName(item.Path);

                    var targetDir = Path.Combine(Settings.Default.GameDirectoryPath, dir);

                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    var srcFile = Path.Combine(_NowSelectModInfo.ExtractArchivePath, item.Path);

                    var targetFile = Path.Combine(Settings.Default.GameDirectoryPath, item.Path);

                    File.Copy(srcFile, targetFile, true);

                    files.Add(item.Path);
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

            _ModListManager.Insert(name: targetFileName, fileSize: new FileInfo(targetFile).Length, archiveFilePath: targetFile, url:"https://");

            ModFileListReflesh();
        }

        private (string, string) PreProcess(string dropFile)
        {
            var resultFile = dropFile;

            var tempDir = Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName);
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            var targetFile = Path.Combine(tempDir, Path.GetFileName(dropFile));
            File.Copy(dropFile, targetFile, true);
            var mod = new ModInfo(id: 1, name: "", status: Status.未インストール, fileSize: new FileInfo(targetFile).Length, dateCreated: DateTime.Now, archiveFilePath: targetFile, url: "");


            if(mod.Category == Category.Lua && !mod.GetFileTree().Any(x => !x.IsFile && x.Name == "reframework"))
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
                SevenZipBase.SetLibraryPath("7z.dll");

                File.Delete(targetFile);

                var compressor = new SevenZipCompressor();
                resultFile = Path.Combine(Path.GetDirectoryName(targetFile), $"{Path.GetFileNameWithoutExtension(targetFile)}.7z");
                
                compressor.CompressDirectory(Path.Combine(tempDir, tempFileName), resultFile);

                Directory.Delete(Path.Combine(tempDir, tempFileName), true);
            }

            return (resultFile, string.Empty);
        }

        private void ModFileListReflesh()
        {
            ModInfoList.Clear();

            _ModListManager.SelectAll().ForEach(x =>
            {
                ModInfoList.Add(new ModInfo(x.Id, x.Name, x.Status, x.FileSize, x.DateCreated, x.ArchiveFilePath, x.URL, this));
            });

            NowModPath.Value = string.Empty;

            NowModSize.Value = string.Empty;

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

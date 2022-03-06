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

        public ObservableCollection<ModFileTree> ModFileTree { get; set; } = new ObservableCollection<ModFileTree>();

        public ReactiveProperty<string> GameDirectoryPath { get; } = new ReactiveProperty<string>(Settings.Default.GameDirectoryPath);
        public ReactiveCommand<DragEventArgs> FileDropCommand { get; private set; }

        public ReactiveCommand<(object sender, EventArgs args)> SelectionChanged { get; private set;} = new ReactiveCommand<(object sender, EventArgs args)>();

        private ModListManager _ModListManager = new ModListManager();

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

                var archive = modInfo.ExtractArchivePath;

                ModFileTree.Clear();

                foreach (var item in modInfo.GetFileTree())
                {
                    ModFileTree.Add(item);
                }
               
            });


            CloseCommand.Subscribe(e =>
            {
                CleanCache();

                Settings.Default.GameDirectoryPath = GameDirectoryPath.Value;
                Settings.Default.Save();




                Disposable.Dispose();

            });

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

            var dir = Environment.CurrentDirectory;
            var cacheDir = Path.Combine(dir, Settings.Default.ModsCacheDirectoryName);

            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            var targetFileName = Path.GetFileName(dropFile);
            var targetFile = Path.Combine(cacheDir, targetFileName);

            File.Copy(dropFile, targetFile);

            _ModListManager.Insert(name: targetFileName, fileSize: new FileInfo(targetFile).Length, archiveFilePath: targetFile, url:"https://");

            ModFileListReflesh();
        }

        private void ModFileListReflesh()
        {
            ModInfoList.Clear();

            _ModListManager.SelectAll().ForEach(x =>
            {
                ModInfoList.Add(new ModInfo(x.Id, x.Name, x.Status, x.FileSize, x.DateCreated, x.ArchiveFilePath, x.URL, this));
            });
        }

        public void OnRowUpdate(ModInfo modInfo)
        {
            // Modの更新
        }
    }
}

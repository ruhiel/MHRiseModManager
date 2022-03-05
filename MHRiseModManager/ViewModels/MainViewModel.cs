using MHRiseModManager.Models;
using MHRiseModManager.Properties;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MHRiseModManager.ViewModels
{
    public class MainViewModel
    {
        private CompositeDisposable Disposable { get; } = new CompositeDisposable();
        public ReactiveCommand CloseCommand { get; } = new ReactiveCommand();

        private ModFileTreeViewModel ModTreeViewModel = new ModFileTreeViewModel();
        public ObservableCollection<ModFileTree> ModFileTree { get; set; }

        public ReactiveProperty<string> GameDirectoryPath { get; } = new ReactiveProperty<string>(Settings.Default.GameDirectoryPath);
        public ReactiveCommand<DragEventArgs> FileDropCommand { get; private set; }

        private ModListManager _ModListManager = new ModListManager();

        public ObservableCollection<ModInfo> ModInfoList { get; set; } = new ObservableCollection<ModInfo>();
        public MainViewModel()
        {
            ModFileTree = new ObservableCollection<ModFileTree>(ModTreeViewModel.ModTree);

            FileDropCommand = new ReactiveCommand<DragEventArgs>().AddTo(Disposable);
            FileDropCommand.Subscribe(e =>
            {
                if (e != null)
                {
                    OnFileDrop(e);
                }
            });

            CloseCommand.Subscribe(e =>
            {
                Settings.Default.GameDirectoryPath = GameDirectoryPath.Value;
                Settings.Default.Save();

                Disposable.Dispose();

            });

            _ModListManager.SelectAll().ForEach(x =>
            {
                ModInfoList.Add(new ModInfo(x.Id, x.Name, x.Status, x.FileSize, x.DateCreated, x.Category, x.ArchiveFilePath, x.URL, this));
            });

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
            var cacheDir = Path.Combine(dir, "ModsCache");

            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            var targetFileName = Path.GetFileName(dropFile);
            var targetFile = Path.Combine(cacheDir, targetFileName);

            File.Copy(dropFile, targetFile);


            _ModListManager.Insert(name: targetFileName, fileSize: new FileInfo(targetFile).Length, category: Category.Pak, archiveFilePath: targetFile, url:"https://");

            ModInfoList.Clear();

            _ModListManager.SelectAll().ForEach(x =>
            {
                ModInfoList.Add(new ModInfo(x.Id, x.Name, x.Status, x.FileSize, x.DateCreated, x.Category, x.ArchiveFilePath, x.URL, this));
            });
        }

        public void OnRowUpdate(ModInfo modInfo)
        {
            // Modの更新
        }
    }
}

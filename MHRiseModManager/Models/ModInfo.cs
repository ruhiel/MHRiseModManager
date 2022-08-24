using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq.Mapping;
using Reactive.Bindings;
using MHRiseModManager.ViewModels;
using System.IO;
using MHRiseModManager.Utils;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;

namespace MHRiseModManager.Models
{
    [Table(Name = "modinfo")]
    public class ModInfo : BindableBase
    {
        [Column(Name = "id", CanBeNull = false, DbType = "INT", IsPrimaryKey = true)]
        private int _Id;
        public int Id
        {
            get => _Id;
            set => SetProperty(ref _Id, value);
        }

        [Column(Name = "name", CanBeNull = false, DbType = "TEXT")]
        private string _Name;
        public string Name
        {
            get => _Name;
            set => SetProperty(ref _Name, value);
        }
        [Column(Name = "status", CanBeNull = false, DbType = "INT")]
        private int _Status;
        public Status Status
        {
            get => (Status)_Status;
            set => SetProperty(ref _Status, (int)value);
        }
        [Column(Name = "filesize", CanBeNull = false, DbType = "INT")]
        private long _Filesize;
        public long FileSize
        {
            get => _Filesize;
            set => SetProperty(ref _Filesize, value);
        }
        [Column(Name = "datecreated", CanBeNull = false, DbType = "TEXT")]
        private string _DateCreated = string.Empty;
        public DateTime DateCreated
        {
            get => DateTime.ParseExact(_DateCreated, "yyyy-MM-dd HH:mm:ss", null);
            set => SetProperty(ref _DateCreated, value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        [Column(Name = "category", CanBeNull = false, DbType = "INT")]
        private int _Category;
        public Category Category
        {
            get => (Category)_Category;
            set => SetProperty(ref _Category, (int)value);
        }
        [Column(Name = "archivefilepath", CanBeNull = false, DbType = "TEXT")]
        private string _ArchiveFilePath;
        public string ArchiveFilePath
        {
            get => _ArchiveFilePath;
            set => SetProperty(ref _ArchiveFilePath, value);
        }
        [Column(Name = "url", CanBeNull = false, DbType = "TEXT")]
        private string _URL;
        public string URL
        {
            get => _URL;
            set => SetProperty(ref _URL, value);
        }
        [Column(Name = "imagefilepath", CanBeNull = true, DbType = "TEXT")]
        private string _ImageFilePath;
        public string ImageFilePath
        {
            get => _ImageFilePath;
            set => SetProperty(ref _ImageFilePath, value);
        }
        [Column(Name = "memo", CanBeNull = true, DbType = "TEXT")]
        private string _Memo;
        public string Memo
        {
            get => _Memo;
            set => SetProperty(ref _Memo, value);
        }
        [Column(Name = "modname", CanBeNull = true, DbType = "TEXT")]
        private string _ModName;
        public string ModName
        {
            get => _ModName;
            set => SetProperty(ref _ModName, value);
        }
        [Column(Name = "version", CanBeNull = true, DbType = "TEXT")]
        private string _Version;
        public string Version
        {
            get => _Version;
            set => SetProperty(ref _Version, value);
        }
        [Column(Name = "modfilebinary", CanBeNull = true)]
        private byte[] _ModFileBinary;
        public byte[] ModFileBinary
        {
            get => _ModFileBinary;
            set => SetProperty(ref _ModFileBinary, value);
        }

        public ModInfo() { }

        public string ExtractArchivePath
        {
            get
            {
                var archiveFile = Path.Combine(Environment.CurrentDirectory, ArchiveFilePath);

                if(!File.Exists(archiveFile))
                {
                    File.WriteAllBytes(archiveFile, ModFileBinary);
                }

                var targetDir = Path.Combine(Path.GetDirectoryName(archiveFile), Path.GetFileNameWithoutExtension(archiveFile));

                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);

                    Utility.ExtractFile(archiveFile, targetDir);
                }
                return targetDir;
            }
        }

        public Category GetNewCategory()
        {
            var category = Category.その他;
            foreach (var item in GetAllTree())
            {
                if (item.Name.EndsWith("lua"))
                {
                    category = Category.Lua;
                    break;
                }
                if (item.Name.Equals("dinput8.dll"))
                {
                    category = Category.REFramework;
                    break;
                }
            }

            return category;
        }

        public ReadOnlyReactiveProperty<bool> Updatable { get; }

        public ReadOnlyReactiveProperty<string> ModNameView { get; }

        public ReactiveCommand<EventArgs> ModUpdateCommand { get; set; } = new ReactiveCommand<EventArgs>();

        public ReactiveCommand<EventArgs> ModEditCommand { get; set; } = new ReactiveCommand<EventArgs>();

        private MainViewModel _MainViewModel;

        public ModInfo(int id, string name, Status status, long fileSize, DateTime dateCreated, Category category, string archiveFilePath, string url, string memo, string version = null, string modName = null, string imageFilePath = null, byte[] modFileBinary = null, MainViewModel mainViewModel = null)
        {
            Id = id;
            Name = name;
            Status = status;
            FileSize = fileSize;
            DateCreated = dateCreated;
            Category = category;
            ArchiveFilePath = archiveFilePath;
            URL = url;
            ImageFilePath = imageFilePath;
            Memo = memo;
            ModName = modName;
            ModFileBinary = modFileBinary;
            Version = version;
            _MainViewModel = mainViewModel;

            ModUpdateCommand.Subscribe(_ =>
            {
                _MainViewModel.OnRowUpdate(this);
            });

            ModEditCommand.Subscribe(_ =>
            {
                _MainViewModel.OnRowEdit(this);
            });

            Updatable = this.ObserveProperty(x => x.Status).Select(y => y == Status.インストール済).ToReadOnlyReactiveProperty();

            ModNameView = this.ObserveProperty(x => x.ModName).Select(y => string.IsNullOrEmpty(y) ? Name : y).ToReadOnlyReactiveProperty();
        }

        public List<ModFileTree> GetFileTree() => Search(ExtractArchivePath);
        public IEnumerable<ModFileTree> GetAllTree()
        {
            foreach(var item in GetFileTree())
            {
                if(item.IsFile)
                {
                    yield return item;
                }
                else
                {
                    foreach (var child in item.Child)
                    {
                        var subElements = GetElements(child); // 再帰呼び出し
                        foreach (var subElement in subElements)
                        {
                            yield return subElement; // 子ノードと含まれる要素を返す
                        }
                    }
                }
            }
        }
        private IEnumerable<ModFileTree> GetElements(ModFileTree node)
        {
            yield return node; // とりあえず自分を返す
            if (!node.HasChild) yield break; // 子ノードがなければ抜ける
            foreach (var child in node.Child)
            {
                var subElements = GetElements(child); // 再帰呼び出し
                foreach (var subElement in subElements)
                {
                    yield return subElement; // 子ノードと含まれる要素を返す
                }
            }
        }

        private List<ModFileTree> Search(string path)
        {
            var chiled = new List<ModFileTree>();
            var di = new DirectoryInfo(path);
            foreach (var f in di.GetFiles())
            {
                ModFileTree tree = new ModFileTree();
                tree.Name = f.Name;
                tree.Path = f.FullName.Substring(ExtractArchivePath.Length + 1);
                tree.IsFile = true;
                chiled.Add(tree);
            }

            foreach (var d in di.GetDirectories())
            {
                ModFileTree tree = new ModFileTree();
                tree.Name = d.Name;
                tree.Path = d.FullName.Substring(ExtractArchivePath.Length + 1);
                tree.Child = Search(d.FullName);
                tree.IsFile = false;
                chiled.Add(tree);
            }

            return chiled;
        }


    }
}

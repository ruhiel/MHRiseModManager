using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using Reactive.Bindings;
using System.Windows;
using MHRiseModManager.ViewModels;

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
        private int _Filesize;
        public int FileSize
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
#pragma warning disable CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        public ModInfo() { }
#pragma warning restore CS8618 // null 非許容のフィールドには、コンストラクターの終了時に null 以外の値が入っていなければなりません。Null 許容として宣言することをご検討ください。
        

        public ReactiveCommand<EventArgs> SomeCommand { get; set; } = new ReactiveCommand<EventArgs>();

        private MainViewModel _MainViewModel;

        public ModInfo(int id, string name, Status status, int fileSize, DateTime dateCreated, Category category, string archiveFilePath, string url, MainViewModel mainViewModel)
        {
            Id = id;
            Name = name;
            Status = status;
            FileSize = fileSize;
            DateCreated = dateCreated;
            Category = category;
            ArchiveFilePath = archiveFilePath;
            URL = url;
            _MainViewModel = mainViewModel;

            SomeCommand.Subscribe(_ =>
            {
                _MainViewModel.OnRowUpdate(this);
            });

        }
    }
}

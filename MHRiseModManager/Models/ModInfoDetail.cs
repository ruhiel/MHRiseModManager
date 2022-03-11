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
    [Table(Name = "modinfodetail")]
    public class ModInfoDetail : BindableBase
    {
        [Column(Name = "id", CanBeNull = false, DbType = "INT", IsPrimaryKey = true)]
        private int _Id;
        public int Id
        {
            get => _Id;
            set => SetProperty(ref _Id, value);
        }
        [Column(Name = "modinfoid", CanBeNull = false, DbType = "INT", IsPrimaryKey = false)]
        private int _ModInfoId;
        public int ModInfoId
        {
            get => _ModInfoId;
            set => SetProperty(ref _ModInfoId, value);
        }
        [Column(Name = "path", CanBeNull = false, DbType = "TEXT", IsPrimaryKey = false)]
        private string _Path;
        public string Path
        {
            get => _Path;
            set => SetProperty(ref _Path, value);
        }
        [Column(Name = "delflg", CanBeNull = false, DbType = "INT", IsPrimaryKey = false)]
        private int _Delflg;
        public int Delflg
        {
            get => _Delflg;
            set => SetProperty(ref _Delflg, value);
        }
        public ModInfoDetail() { }
    }
}

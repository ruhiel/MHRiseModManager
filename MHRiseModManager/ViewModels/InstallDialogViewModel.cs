using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHRiseModManager.ViewModels
{
    public class InstallDialogViewModel
    {
        public ReactiveCommand CloseWindow { get; } = new ReactiveCommand();
        public ReactiveCommand CloseWindowCancel { get; } = new ReactiveCommand();
        public ReactiveProperty<string> Name { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> URL { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> Version { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> Memo { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<bool> PakMode { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<string> PakFileName { get; } = new ReactiveProperty<string>();
        public bool Result { get; set; }

        public InstallDialogViewModel()
        {
            CloseWindow.Subscribe(x =>
            {
                Result = true;
                ((System.Windows.Window)x).Close();
            });
            CloseWindowCancel.Subscribe(x =>
            {
                Result = false;
                ((System.Windows.Window)x).Close();
            });
        }
    }
}

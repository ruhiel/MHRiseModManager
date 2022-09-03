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
        public ReactiveProperty<string> Name { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> URL { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> Version { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> Memo { get; } = new ReactiveProperty<string>();
        public InstallDialogViewModel()
        {
            CloseWindow.Subscribe(x => ((System.Windows.Window)x).Close());
        }
    }
}

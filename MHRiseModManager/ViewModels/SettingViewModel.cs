using Reactive.Bindings;
using System;

namespace MHRiseModManager.ViewModels
{
    public class SettingViewModel
    {
        public ReactiveCommand CloseWindow { get; } = new ReactiveCommand();

        public SettingViewModel()
        {
            CloseWindow.Subscribe(x => ((System.Windows.Window)x).Close());
        }
    }
}

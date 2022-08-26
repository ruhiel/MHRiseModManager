using MahApps.Metro.Controls.Dialogs;
using MHRiseModManager.Properties;
using MHRiseModManager.Utils;
using Reactive.Bindings;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace MHRiseModManager.ViewModels
{
    public class SettingViewModel
    {
        public IDialogCoordinator MahAppsDialogCoordinator { get; set; }
        public ReactiveCommand CloseWindow { get; } = new ReactiveCommand();
        public AsyncReactiveCommand SettingResetCommand { get; } = new AsyncReactiveCommand();
        public ReactiveProperty<bool> StartUpVersionCheck { get; } = new ReactiveProperty<bool>();

        public SettingViewModel()
        {
            StartUpVersionCheck.Value = Settings.Default.StartUpVersionCheck;

            SettingResetCommand.Subscribe(async e =>
            {
                var metroDialogSettings = new MetroDialogSettings()
                {
                    AffirmativeButtonText = "はい",
                    NegativeButtonText = "いいえ",
                    AnimateHide = true,
                    AnimateShow = true,
                    ColorScheme = MetroDialogColorScheme.Theme,
                };

                var diagResult = await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "設定の初期化は全てのModのアンインストール後をおすすめします。\r\nよろしいですか？", MessageDialogStyle.AffirmativeAndNegative, metroDialogSettings);

                if (MessageDialogResult.Negative == diagResult)
                {
                    return;
                }

                Utility.FileSafeDelete(Path.Combine(Environment.CurrentDirectory, Settings.Default.DataBaseFileName));

                Utility.CleanDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.ModsCacheDirectoryName));

                Utility.CleanDirectory(Path.Combine(Environment.CurrentDirectory, Settings.Default.ImageCacheDirectoryName));

                Utility.CleanDirectory(Path.Combine(Path.GetTempPath(), Settings.Default.TempDirectoryName));

                await MahAppsDialogCoordinator.ShowMessageAsync(this, Assembly.GetEntryAssembly().GetName().Name, "設定を初期化しました。アプリケーションを再起動します。");

                Application.Current.Shutdown();
                System.Windows.Forms.Application.Restart();
            });

            CloseWindow.Subscribe(x =>
            {
                Settings.Default.StartUpVersionCheck = StartUpVersionCheck.Value;
                Settings.Default.Save();
                ((System.Windows.Window)x).Close();
            });
        }
    }
}

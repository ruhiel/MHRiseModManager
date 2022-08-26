using ControlzEx.Theming;
using MahApps.Metro.Controls.Dialogs;
using MHRiseModManager.Models;
using MHRiseModManager.Properties;
using MHRiseModManager.Utils;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Linq;
using System.Windows.Controls;

namespace MHRiseModManager.ViewModels
{
    public class SettingViewModel
    {
        public IDialogCoordinator MahAppsDialogCoordinator { get; set; }
        public ReactiveCommand CloseWindow { get; } = new ReactiveCommand();
        public ReactiveCommand CloseWindowCancel { get; } = new ReactiveCommand();
        public AsyncReactiveCommand SettingResetCommand { get; } = new AsyncReactiveCommand();
        public ReactiveProperty<bool> StartUpVersionCheck { get; } = new ReactiveProperty<bool>();
        public ReactiveProperty<bool> CheckDark { get; } = new ReactiveProperty<bool>();
        public ReactiveProperty<bool> CheckLight { get; } = new ReactiveProperty<bool>();
        public ReactiveCommand<(object sender, EventArgs args)> SelectDark { get; private set; } = new ReactiveCommand<(object sender, EventArgs args)>();
        public ReactiveCommand<(object sender, EventArgs args)> SelectLight { get; private set; } = new ReactiveCommand<(object sender, EventArgs args)>();
        public ReactiveCommand<(object sender, EventArgs args)> SelectionColorChanged { get; private set; } = new ReactiveCommand<(object sender, EventArgs args)>();
        public ReactiveProperty<ThemaColor> NowSelectColor { get; private set; } = new ReactiveProperty<ThemaColor>();
        public ReactiveCollection<ThemaColor> ColorList { get; set; } = new ReactiveCollection<ThemaColor>();

        private List<ThemaColor> ColorListData = new List<ThemaColor>() {
            new ThemaColor() { Name = "赤", Color = "Red" },
            new ThemaColor() { Name = "緑", Color = "Green" },
            new ThemaColor() { Name = "青", Color = "Blue" },
            new ThemaColor() { Name = "紫", Color = "Purple" },
            new ThemaColor() { Name = "オレンジ", Color = "Orange" },
            new ThemaColor() { Name = "ライム", Color = "Lime" },
            new ThemaColor() { Name = "エメラルド", Color = "Emerald" },
            new ThemaColor() { Name = "青緑", Color = "Teal" },
            new ThemaColor() { Name = "シアン", Color = "Cyan" },
            new ThemaColor() { Name = "コバルトブルー", Color = "Cobalt" },
            new ThemaColor() { Name = "インディゴ", Color = "Indigo" },
            new ThemaColor() { Name = "バイオレット", Color = "Violet" },
            new ThemaColor() { Name = "ピンク", Color = "Pink" },
            new ThemaColor() { Name = "マゼンタ", Color = "Magenta" },
            new ThemaColor() { Name = "クリムゾン", Color = "Crimson" },
            new ThemaColor() { Name = "琥珀色", Color = "Amber" },
            new ThemaColor() { Name = "黄", Color = "Yellow" },
            new ThemaColor() { Name = "茶", Color = "Brown" },
            new ThemaColor() { Name = "オリーブ", Color = "Olive" },
            new ThemaColor() { Name = "スチールグレー", Color = "Steel" },
            new ThemaColor() { Name = "葵", Color = "Mauve" },
            new ThemaColor() { Name = "トープ", Color = "Taupe" },
            new ThemaColor() { Name = "シエナ", Color = "Sienna" },
        };
        public SettingViewModel()
        {
            CheckLight.Value = Settings.Default.BaseColor.Equals("Light");
            CheckDark.Value = !Settings.Default.BaseColor.Equals("Light");
            foreach (var item in ColorListData)
            {
                ColorList.Add(item);
            }

            NowSelectColor.Value = ColorListData.Where(x => x.Color.Equals(Settings.Default.ColorScheme)).First();

            StartUpVersionCheck.Value = Settings.Default.StartUpVersionCheck;
            SelectDark.Subscribe(e =>
            {
                ThemeManager.Current.ChangeThemeBaseColor(Application.Current, "Dark");
            });
            SelectLight.Subscribe(e =>
            {
                ThemeManager.Current.ChangeThemeBaseColor(Application.Current, "Light");
            });
            SelectionColorChanged.Subscribe(e =>
            {
                ThemeManager.Current.ChangeThemeColorScheme(Application.Current, NowSelectColor.Value.Color);
            });
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
                Settings.Default.BaseColor = CheckLight.Value ? "Light" : "Dark";
                Settings.Default.ColorScheme = NowSelectColor.Value.Color;
                Settings.Default.StartUpVersionCheck = StartUpVersionCheck.Value;
                Settings.Default.Save();
                ((System.Windows.Window)x).Close();
            });
            CloseWindowCancel.Subscribe(x =>
            {
                ThemeManager.Current.ChangeThemeBaseColor(Application.Current, Settings.Default.BaseColor);
                ThemeManager.Current.ChangeThemeColorScheme(Application.Current, Settings.Default.ColorScheme);

                ((System.Windows.Window)x).Close();
            });
        }
    }
}

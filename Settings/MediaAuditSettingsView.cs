using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace MediaAudit
{
    public class MediaAuditSettingsView : UserControl
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private class MediaPreset
        {
            public string Name { get; set; }
            public double AspectRatio { get; set; }
            public double Tolerance { get; set; }
            public int MinWidth { get; set; }
            public int MinHeight { get; set; }
            public int MaxWidth { get; set; }
            public int MaxHeight { get; set; }

            public override string ToString() => Name;
        }

        private static readonly List<MediaPreset> IconPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "Square 1:1 (default)",   AspectRatio = 1.0, Tolerance = 0.1, MinWidth = 64,  MinHeight = 64,  MaxWidth = 512,  MaxHeight = 512 },
            new MediaPreset { Name = "Square 1:1 (large)",     AspectRatio = 1.0, Tolerance = 0.1, MinWidth = 128, MinHeight = 128, MaxWidth = 1024, MaxHeight = 1024 },
            new MediaPreset { Name = "ICO standard",           AspectRatio = 1.0, Tolerance = 0.1, MinWidth = 256, MinHeight = 256, MaxWidth = 256,  MaxHeight = 256 },
            new MediaPreset { Name = "Square 1:1 (any size)",  AspectRatio = 1.0, Tolerance = 0.1, MinWidth = 16,  MinHeight = 16,  MaxWidth = 0,    MaxHeight = 0 },
        };

        private static readonly List<MediaPreset> CoverPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "Steam (2:3)",            AspectRatio = 0.667, Tolerance = 0.1,  MinWidth = 300, MinHeight = 450 },
            new MediaPreset { Name = "Epic Games (2:3 HD)",    AspectRatio = 0.667, Tolerance = 0.1,  MinWidth = 600, MinHeight = 900 },
            new MediaPreset { Name = "GOG (27:38)",            AspectRatio = 0.71,  Tolerance = 0.1,  MinWidth = 342, MinHeight = 482 },
            new MediaPreset { Name = "IGDB (3:4)",             AspectRatio = 0.75,  Tolerance = 0.1,  MinWidth = 264, MinHeight = 352 },
            new MediaPreset { Name = "Portrait (3:4)",         AspectRatio = 0.75,  Tolerance = 0.1,  MinWidth = 300, MinHeight = 400 },
            new MediaPreset { Name = "Tall portrait (9:16)",   AspectRatio = 0.5625, Tolerance = 0.1, MinWidth = 360, MinHeight = 640 },
            new MediaPreset { Name = "Any portrait",           AspectRatio = 0.7,   Tolerance = 0.2,  MinWidth = 200, MinHeight = 280 },
            new MediaPreset { Name = "Steam header (46:21)",   AspectRatio = 2.19,  Tolerance = 0.15, MinWidth = 460, MinHeight = 215 },
            new MediaPreset { Name = "Square (1:1)",           AspectRatio = 1.0,   Tolerance = 0.1,  MinWidth = 300, MinHeight = 300 },
        };

        private static readonly List<MediaPreset> LogoPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "Wide logo (default)",    AspectRatio = 2.5, Tolerance = 1.5, MinWidth = 400, MinHeight = 150 },
            new MediaPreset { Name = "SteamGridDB",            AspectRatio = 2.5, Tolerance = 1.5, MinWidth = 500, MinHeight = 200 },
            new MediaPreset { Name = "Steam logo (strict)",    AspectRatio = 2.5, Tolerance = 0.5, MinWidth = 600, MinHeight = 240 },
            new MediaPreset { Name = "Any logo",               AspectRatio = 2.5, Tolerance = 2.0, MinWidth = 200, MinHeight = 80 },
        };

        private static readonly List<MediaPreset> BackgroundPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "16:9 720p",              AspectRatio = 1.778, Tolerance = 0.15, MinWidth = 1280, MinHeight = 720 },
            new MediaPreset { Name = "16:9 1080p",             AspectRatio = 1.778, Tolerance = 0.15, MinWidth = 1920, MinHeight = 1080 },
            new MediaPreset { Name = "16:9 1440p",             AspectRatio = 1.778, Tolerance = 0.15, MinWidth = 2560, MinHeight = 1440 },
            new MediaPreset { Name = "16:9 4K",                AspectRatio = 1.778, Tolerance = 0.15, MinWidth = 3840, MinHeight = 2160 },
            new MediaPreset { Name = "16:10",                  AspectRatio = 1.6,   Tolerance = 0.15, MinWidth = 1920, MinHeight = 1200 },
            new MediaPreset { Name = "21:9 Ultrawide",         AspectRatio = 2.333, Tolerance = 0.15, MinWidth = 2560, MinHeight = 1080 },
            new MediaPreset { Name = "Steam Hero (32:15)",     AspectRatio = 2.133, Tolerance = 0.15, MinWidth = 3200, MinHeight = 1500 },
            new MediaPreset { Name = "Any landscape",          AspectRatio = 1.6,   Tolerance = 0.5,  MinWidth = 1280, MinHeight = 720 },
        };

        public MediaAuditSettingsView()
        {
            logger.Debug("Initializing settings view.");
            var mainStack = new StackPanel { Margin = new Thickness(20) };

            AddSection(mainStack, Loc("LOC_MediaAudit_Section_General"), stack =>
            {
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_BackgroundScanEnabled"), "BackgroundScanEnabled");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_ScanInterval"), "ScanIntervalMinutes");
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_ReportMissing"), "ReportMissing");
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_ShowScanNotification"), "ShowScanNotification");
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_TagUndesiredMedia"), "TagUndesiredMedia");
            });

            AddSection(mainStack, Loc("LOC_MediaAudit_Section_MediaTypes"), stack =>
            {
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_CheckIcons"), "CheckIcons");
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_CheckCovers"), "CheckCovers");
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_CheckBackgrounds"), "CheckBackgrounds");
            });

            AddSection(mainStack, Loc("LOC_MediaAudit_Section_IconStandards"), stack =>
            {
                AddPresetDropdown(stack, Loc("LOC_MediaAudit_Settings_Preset"), IconPresets, s =>
                {
                    s.IconAspectRatioTolerance = 0;
                    s.IconMinSize = 0;
                    s.IconMaxSize = 0;
                }, (s, p) =>
                {
                    s.IconAspectRatioTolerance = p.Tolerance;
                    s.IconMinSize = p.MinWidth;
                    s.IconMaxSize = p.MaxWidth;
                });
                AddTextField(stack, Loc("LOC_MediaAudit_Settings_TagName"), "IconTagName");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_MinSize"), "IconMinSize");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_MaxSize"), "IconMaxSize");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_AspectRatioTolerance"), "IconAspectRatioTolerance");
            });

            AddSection(mainStack, Loc("LOC_MediaAudit_Section_CoverStandards"), stack =>
            {
                AddPresetDropdown(stack, Loc("LOC_MediaAudit_Settings_Preset"), CoverPresets, s =>
                {
                    s.CoverAspectRatio = 0;
                    s.CoverAspectRatioTolerance = 0;
                    s.CoverMinWidth = 0;
                    s.CoverMinHeight = 0;
                }, (s, p) =>
                {
                    s.CoverAspectRatio = p.AspectRatio;
                    s.CoverAspectRatioTolerance = p.Tolerance;
                    s.CoverMinWidth = p.MinWidth;
                    s.CoverMinHeight = p.MinHeight;
                });
                AddTextField(stack, Loc("LOC_MediaAudit_Settings_TagName"), "CoverTagName");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_ExpectedAspectRatio"), "CoverAspectRatio");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_AspectRatioTolerance"), "CoverAspectRatioTolerance");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_MinWidth"), "CoverMinWidth");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_MinHeight"), "CoverMinHeight");
            });

            AddSection(mainStack, Loc("LOC_MediaAudit_Section_BackgroundStandards"), stack =>
            {
                AddPresetDropdown(stack, Loc("LOC_MediaAudit_Settings_Preset"), BackgroundPresets, s =>
                {
                    s.BackgroundAspectRatio = 0;
                    s.BackgroundAspectRatioTolerance = 0;
                    s.BackgroundMinWidth = 0;
                    s.BackgroundMinHeight = 0;
                }, (s, p) =>
                {
                    s.BackgroundAspectRatio = p.AspectRatio;
                    s.BackgroundAspectRatioTolerance = p.Tolerance;
                    s.BackgroundMinWidth = p.MinWidth;
                    s.BackgroundMinHeight = p.MinHeight;
                });
                AddTextField(stack, Loc("LOC_MediaAudit_Settings_TagName"), "BackgroundTagName");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_ExpectedAspectRatio"), "BackgroundAspectRatio");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_AspectRatioTolerance"), "BackgroundAspectRatioTolerance");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_MinWidth"), "BackgroundMinWidth");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_MinHeight"), "BackgroundMinHeight");
            });

            AddSection(mainStack, Loc("LOC_MediaAudit_Section_Logo"), stack =>
            {
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_CheckLogos"), "CheckLogos");
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_InstalledOnly"), "LogoInstalledOnly");
                AddPresetDropdown(stack, Loc("LOC_MediaAudit_Settings_Preset"), LogoPresets, s =>
                {
                    s.LogoAspectRatio = 0;
                    s.LogoAspectRatioTolerance = 0;
                    s.LogoMinWidth = 0;
                    s.LogoMinHeight = 0;
                }, (s, p) =>
                {
                    s.LogoAspectRatio = p.AspectRatio;
                    s.LogoAspectRatioTolerance = p.Tolerance;
                    s.LogoMinWidth = p.MinWidth;
                    s.LogoMinHeight = p.MinHeight;
                });
                AddTextField(stack, Loc("LOC_MediaAudit_Settings_TagName"), "LogoTagName");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_ExpectedAspectRatio"), "LogoAspectRatio");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_AspectRatioTolerance"), "LogoAspectRatioTolerance");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_MinWidth"), "LogoMinWidth");
                AddNumericField(stack, Loc("LOC_MediaAudit_Settings_MinHeight"), "LogoMinHeight");
            });

            AddSection(mainStack, Loc("LOC_MediaAudit_Section_Videos"), stack =>
            {
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_CheckTrailers"), "CheckTrailers");
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_TrailerInstalledOnly"), "TrailerInstalledOnly");
                AddTextField(stack, Loc("LOC_MediaAudit_Settings_TrailerTagName"), "TrailerTagName");
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_CheckMicrotrailers"), "CheckMicrotrailers");
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_MicrotrailerInstalledOnly"), "MicrotrailerInstalledOnly");
                AddTextField(stack, Loc("LOC_MediaAudit_Settings_MicrotrailerTagName"), "MicrotrailerTagName");
            });

            AddSection(mainStack, Loc("LOC_MediaAudit_Section_GameMusic"), stack =>
            {
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_CheckGameMusic"), "CheckGameMusic");
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_InstalledOnly"), "GameMusicInstalledOnly");
                AddTextField(stack, Loc("LOC_MediaAudit_Settings_TagName"), "GameMusicTagName");
            });

            Content = new ScrollViewer
            {
                Content = mainStack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }

        private static string Loc(string key) => ResourceProvider.GetString(key);

        private void AddPresetDropdown(StackPanel parent, string label, List<MediaPreset> presets,
            Action<MediaAuditSettings> clear, Action<MediaAuditSettings, MediaPreset> apply)
        {
            var panel = new DockPanel { Margin = new Thickness(0, 5, 0, 5) };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                Width = 180,
                VerticalAlignment = VerticalAlignment.Center
            });

            var combo = new ComboBox
            {
                Width = 200,
                ItemsSource = presets,
                IsEditable = false
            };

            combo.SelectionChanged += (s, e) =>
            {
                var preset = combo.SelectedItem as MediaPreset;
                var settings = DataContext as MediaAuditSettings;
                if (preset == null || settings == null)
                    return;

                clear(settings);
                apply(settings, preset);
            };

            panel.Children.Add(combo);
            parent.Children.Add(panel);
        }

        private static void AddSection(StackPanel parent, string header, Action<StackPanel> populate)
        {
            var stack = new StackPanel();
            populate(stack);
            parent.Children.Add(new GroupBox
            {
                Header = header,
                Content = stack,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(10)
            });
        }

        private static void AddCheckbox(StackPanel parent, string label, string binding)
        {
            var cb = new CheckBox
            {
                Content = label,
                Margin = new Thickness(0, 5, 0, 0)
            };
            cb.SetBinding(ToggleButton.IsCheckedProperty, new Binding(binding));
            parent.Children.Add(cb);
        }

        private static void AddTextField(StackPanel parent, string label, string binding)
        {
            var panel = new DockPanel { Margin = new Thickness(0, 5, 0, 0) };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                Width = 180,
                VerticalAlignment = VerticalAlignment.Center
            });
            var tb = new TextBox { Width = 200 };
            tb.SetBinding(TextBox.TextProperty, new Binding(binding)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            panel.Children.Add(tb);
            parent.Children.Add(panel);
        }

        private static void AddNumericField(StackPanel parent, string label, string binding)
        {
            var panel = new DockPanel { Margin = new Thickness(0, 5, 0, 0) };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                Width = 180,
                VerticalAlignment = VerticalAlignment.Center
            });
            var tb = new TextBox { Width = 100 };
            tb.SetBinding(TextBox.TextProperty, new Binding(binding)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            panel.Children.Add(tb);
            parent.Children.Add(panel);
        }
    }
}

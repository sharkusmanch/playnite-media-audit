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
            public double MinAspectRatio { get; set; }
            public double MaxAspectRatio { get; set; }
            public int MinWidth { get; set; }
            public int MaxWidth { get; set; }
            public int MinHeight { get; set; }
            public int MaxHeight { get; set; }

            public override string ToString() => Name;
        }

        private static readonly List<MediaPreset> IconPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "Square 1:1 (default)",   MinAspectRatio = 0.9, MaxAspectRatio = 1.1, MinWidth = 64,  MaxWidth = 512,  MinHeight = 64,  MaxHeight = 512 },
            new MediaPreset { Name = "Square 1:1 (large)",     MinAspectRatio = 0.9, MaxAspectRatio = 1.1, MinWidth = 128, MaxWidth = 1024, MinHeight = 128, MaxHeight = 1024 },
            new MediaPreset { Name = "ICO standard",           MinAspectRatio = 0.9, MaxAspectRatio = 1.1, MinWidth = 256, MaxWidth = 256,  MinHeight = 256, MaxHeight = 256 },
            new MediaPreset { Name = "Square 1:1 (any size)",  MinAspectRatio = 0.9, MaxAspectRatio = 1.1, MinWidth = 16,  MaxWidth = 0,    MinHeight = 16,  MaxHeight = 0 },
        };

        private static readonly List<MediaPreset> CoverPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "Steam (2:3)",            MinAspectRatio = 0.6, MaxAspectRatio = 0.75, MinWidth = 300, MaxWidth = 0, MinHeight = 450, MaxHeight = 0 },
            new MediaPreset { Name = "Epic Games (2:3 HD)",    MinAspectRatio = 0.6, MaxAspectRatio = 0.75, MinWidth = 600, MaxWidth = 0, MinHeight = 900, MaxHeight = 0 },
            new MediaPreset { Name = "GOG (27:38)",            MinAspectRatio = 0.65, MaxAspectRatio = 0.77, MinWidth = 342, MaxWidth = 0, MinHeight = 482, MaxHeight = 0 },
            new MediaPreset { Name = "IGDB (3:4)",             MinAspectRatio = 0.7, MaxAspectRatio = 0.8, MinWidth = 264, MaxWidth = 0, MinHeight = 352, MaxHeight = 0 },
            new MediaPreset { Name = "Portrait (3:4)",         MinAspectRatio = 0.7, MaxAspectRatio = 0.8, MinWidth = 300, MaxWidth = 0, MinHeight = 400, MaxHeight = 0 },
            new MediaPreset { Name = "Tall portrait (9:16)",   MinAspectRatio = 0.5, MaxAspectRatio = 0.6, MinWidth = 360, MaxWidth = 0, MinHeight = 640, MaxHeight = 0 },
            new MediaPreset { Name = "Any portrait",           MinAspectRatio = 0.5, MaxAspectRatio = 0.9, MinWidth = 200, MaxWidth = 0, MinHeight = 280, MaxHeight = 0 },
            new MediaPreset { Name = "Steam header (46:21)",   MinAspectRatio = 2.0, MaxAspectRatio = 2.4, MinWidth = 460, MaxWidth = 0, MinHeight = 215, MaxHeight = 0 },
            new MediaPreset { Name = "Square (1:1)",           MinAspectRatio = 0.9, MaxAspectRatio = 1.1, MinWidth = 300, MaxWidth = 0, MinHeight = 300, MaxHeight = 0 },
        };

        private static readonly List<MediaPreset> LogoPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "Wide logo (default)",    MinAspectRatio = 1.0, MaxAspectRatio = 4.0, MinWidth = 400, MaxWidth = 0, MinHeight = 150, MaxHeight = 0 },
            new MediaPreset { Name = "SteamGridDB",            MinAspectRatio = 1.0, MaxAspectRatio = 4.0, MinWidth = 500, MaxWidth = 0, MinHeight = 200, MaxHeight = 0 },
            new MediaPreset { Name = "Steam logo (strict)",    MinAspectRatio = 1.8, MaxAspectRatio = 3.2, MinWidth = 600, MaxWidth = 0, MinHeight = 240, MaxHeight = 0 },
            new MediaPreset { Name = "Any logo",               MinAspectRatio = 0.5, MaxAspectRatio = 5.0, MinWidth = 200, MaxWidth = 0, MinHeight = 80, MaxHeight = 0 },
        };

        private static readonly List<MediaPreset> BackgroundPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "16:9 720p",              MinAspectRatio = 1.6, MaxAspectRatio = 1.9, MinWidth = 1280, MaxWidth = 0, MinHeight = 720, MaxHeight = 0 },
            new MediaPreset { Name = "16:9 1080p",             MinAspectRatio = 1.6, MaxAspectRatio = 1.9, MinWidth = 1920, MaxWidth = 0, MinHeight = 1080, MaxHeight = 0 },
            new MediaPreset { Name = "16:9 1440p",             MinAspectRatio = 1.6, MaxAspectRatio = 1.9, MinWidth = 2560, MaxWidth = 0, MinHeight = 1440, MaxHeight = 0 },
            new MediaPreset { Name = "16:9 4K",                MinAspectRatio = 1.6, MaxAspectRatio = 1.9, MinWidth = 3840, MaxWidth = 0, MinHeight = 2160, MaxHeight = 0 },
            new MediaPreset { Name = "16:10",                  MinAspectRatio = 1.5, MaxAspectRatio = 1.7, MinWidth = 1920, MaxWidth = 0, MinHeight = 1200, MaxHeight = 0 },
            new MediaPreset { Name = "21:9 Ultrawide",         MinAspectRatio = 2.1, MaxAspectRatio = 2.5, MinWidth = 2560, MaxWidth = 0, MinHeight = 1080, MaxHeight = 0 },
            new MediaPreset { Name = "Steam Hero (32:15)",     MinAspectRatio = 2.0, MaxAspectRatio = 2.3, MinWidth = 3200, MaxWidth = 0, MinHeight = 1500, MaxHeight = 0 },
            new MediaPreset { Name = "Any landscape",          MinAspectRatio = 1.1, MaxAspectRatio = 2.6, MinWidth = 1280, MaxWidth = 0, MinHeight = 720, MaxHeight = 0 },
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
                AddPresetDropdown(stack, Loc("LOC_MediaAudit_Settings_Preset"), IconPresets, (s, p) =>
                {
                    s.IconMinAspectRatio = p.MinAspectRatio;
                    s.IconMaxAspectRatio = p.MaxAspectRatio;
                    s.IconMinWidth = p.MinWidth;
                    s.IconMaxWidth = p.MaxWidth;
                    s.IconMinHeight = p.MinHeight;
                    s.IconMaxHeight = p.MaxHeight;
                });
                AddTextField(stack, Loc("LOC_MediaAudit_Settings_TagName"), "IconTagName");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_AspectRatio"), "IconMinAspectRatio", "IconMaxAspectRatio");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_Width"), "IconMinWidth", "IconMaxWidth");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_Height"), "IconMinHeight", "IconMaxHeight");
            });

            AddSection(mainStack, Loc("LOC_MediaAudit_Section_CoverStandards"), stack =>
            {
                AddPresetDropdown(stack, Loc("LOC_MediaAudit_Settings_Preset"), CoverPresets, (s, p) =>
                {
                    s.CoverMinAspectRatio = p.MinAspectRatio;
                    s.CoverMaxAspectRatio = p.MaxAspectRatio;
                    s.CoverMinWidth = p.MinWidth;
                    s.CoverMaxWidth = p.MaxWidth;
                    s.CoverMinHeight = p.MinHeight;
                    s.CoverMaxHeight = p.MaxHeight;
                });
                AddTextField(stack, Loc("LOC_MediaAudit_Settings_TagName"), "CoverTagName");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_AspectRatio"), "CoverMinAspectRatio", "CoverMaxAspectRatio");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_Width"), "CoverMinWidth", "CoverMaxWidth");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_Height"), "CoverMinHeight", "CoverMaxHeight");
            });

            AddSection(mainStack, Loc("LOC_MediaAudit_Section_BackgroundStandards"), stack =>
            {
                AddPresetDropdown(stack, Loc("LOC_MediaAudit_Settings_Preset"), BackgroundPresets, (s, p) =>
                {
                    s.BackgroundMinAspectRatio = p.MinAspectRatio;
                    s.BackgroundMaxAspectRatio = p.MaxAspectRatio;
                    s.BackgroundMinWidth = p.MinWidth;
                    s.BackgroundMaxWidth = p.MaxWidth;
                    s.BackgroundMinHeight = p.MinHeight;
                    s.BackgroundMaxHeight = p.MaxHeight;
                });
                AddTextField(stack, Loc("LOC_MediaAudit_Settings_TagName"), "BackgroundTagName");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_AspectRatio"), "BackgroundMinAspectRatio", "BackgroundMaxAspectRatio");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_Width"), "BackgroundMinWidth", "BackgroundMaxWidth");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_Height"), "BackgroundMinHeight", "BackgroundMaxHeight");
            });

            AddSection(mainStack, Loc("LOC_MediaAudit_Section_Logo"), stack =>
            {
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_CheckLogos"), "CheckLogos");
                AddCheckbox(stack, Loc("LOC_MediaAudit_Settings_InstalledOnly"), "LogoInstalledOnly");
                AddPresetDropdown(stack, Loc("LOC_MediaAudit_Settings_Preset"), LogoPresets, (s, p) =>
                {
                    s.LogoMinAspectRatio = p.MinAspectRatio;
                    s.LogoMaxAspectRatio = p.MaxAspectRatio;
                    s.LogoMinWidth = p.MinWidth;
                    s.LogoMaxWidth = p.MaxWidth;
                    s.LogoMinHeight = p.MinHeight;
                    s.LogoMaxHeight = p.MaxHeight;
                });
                AddTextField(stack, Loc("LOC_MediaAudit_Settings_TagName"), "LogoTagName");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_AspectRatio"), "LogoMinAspectRatio", "LogoMaxAspectRatio");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_Width"), "LogoMinWidth", "LogoMaxWidth");
                AddRangeField(stack, Loc("LOC_MediaAudit_Settings_Height"), "LogoMinHeight", "LogoMaxHeight");
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
            Action<MediaAuditSettings, MediaPreset> apply)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            });

            var combo = new ComboBox
            {
                Width = 200,
                ItemsSource = presets,
                IsEditable = false,
                VerticalAlignment = VerticalAlignment.Center
            };

            combo.SelectionChanged += (s, e) =>
            {
                var preset = combo.SelectedItem as MediaPreset;
                var settings = DataContext as MediaAuditSettings;
                if (preset == null || settings == null)
                    return;

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
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            });
            var tb = new TextBox { Width = 300, VerticalAlignment = VerticalAlignment.Center };
            tb.SetBinding(TextBox.TextProperty, new Binding(binding)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            panel.Children.Add(tb);
            parent.Children.Add(panel);
        }

        private static void AddNumericField(StackPanel parent, string label, string binding)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            });
            var tb = new TextBox { Width = 100, VerticalAlignment = VerticalAlignment.Center };
            tb.SetBinding(TextBox.TextProperty, new Binding(binding)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            panel.Children.Add(tb);
            parent.Children.Add(panel);
        }

        private static void AddRangeField(StackPanel parent, string label, string minBinding, string maxBinding)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            });

            var minBox = new TextBox { Width = 80, VerticalAlignment = VerticalAlignment.Center };
            minBox.SetBinding(TextBox.TextProperty, new Binding(minBinding)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            panel.Children.Add(minBox);

            panel.Children.Add(new TextBlock
            {
                Text = " ~ ",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 2, 0)
            });

            var maxBox = new TextBox { Width = 80, VerticalAlignment = VerticalAlignment.Center };
            maxBox.SetBinding(TextBox.TextProperty, new Binding(maxBinding)
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            panel.Children.Add(maxBox);

            parent.Children.Add(panel);
        }
    }
}
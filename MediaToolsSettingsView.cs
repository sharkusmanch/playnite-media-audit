using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace MediaTools
{
    public class MediaPreset
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

    public class MediaToolsSettingsView : UserControl
    {
        private static readonly List<MediaPreset> IconPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "Square 1:1 (default)",   AspectRatio = 1.0, Tolerance = 0.1, MinWidth = 64,  MinHeight = 64,  MaxWidth = 512,  MaxHeight = 512 },
            new MediaPreset { Name = "Square 1:1 (large)",     AspectRatio = 1.0, Tolerance = 0.1, MinWidth = 128, MinHeight = 128, MaxWidth = 1024, MaxHeight = 1024 },
            new MediaPreset { Name = "Square 1:1 (any size)",  AspectRatio = 1.0, Tolerance = 0.1, MinWidth = 16,  MinHeight = 16,  MaxWidth = 0,    MaxHeight = 0 },
        };

        private static readonly List<MediaPreset> CoverPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "Steam (2:3)",        AspectRatio = 0.667, Tolerance = 0.1,  MinWidth = 300, MinHeight = 450 },
            new MediaPreset { Name = "GOG (27:38)",        AspectRatio = 0.71,  Tolerance = 0.1,  MinWidth = 342, MinHeight = 482 },
            new MediaPreset { Name = "Portrait (3:4)",     AspectRatio = 0.75,  Tolerance = 0.1,  MinWidth = 300, MinHeight = 400 },
            new MediaPreset { Name = "Any portrait",       AspectRatio = 0.7,   Tolerance = 0.2,  MinWidth = 200, MinHeight = 280 },
            new MediaPreset { Name = "Steam header (46:21)", AspectRatio = 2.19, Tolerance = 0.15, MinWidth = 460, MinHeight = 215 },
            new MediaPreset { Name = "Square (1:1)",       AspectRatio = 1.0,  Tolerance = 0.1,  MinWidth = 300, MinHeight = 300 },
        };

        private static readonly List<MediaPreset> LogoPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "Wide logo (default)", AspectRatio = 2.5, Tolerance = 1.5, MinWidth = 400, MinHeight = 150 },
            new MediaPreset { Name = "Any logo",            AspectRatio = 2.5, Tolerance = 2.0, MinWidth = 200, MinHeight = 80 },
            new MediaPreset { Name = "Steam logo (strict)",  AspectRatio = 2.5, Tolerance = 0.5, MinWidth = 600, MinHeight = 240 },
        };

        private static readonly List<MediaPreset> BackgroundPresets = new List<MediaPreset>
        {
            new MediaPreset { Name = "16:9 1080p",         AspectRatio = 1.778, Tolerance = 0.15, MinWidth = 1920, MinHeight = 1080 },
            new MediaPreset { Name = "16:9 720p",          AspectRatio = 1.778, Tolerance = 0.15, MinWidth = 1280, MinHeight = 720 },
            new MediaPreset { Name = "16:10",              AspectRatio = 1.6,   Tolerance = 0.15, MinWidth = 1920, MinHeight = 1200 },
            new MediaPreset { Name = "21:9 Ultrawide",     AspectRatio = 2.333, Tolerance = 0.15, MinWidth = 2560, MinHeight = 1080 },
            new MediaPreset { Name = "Any landscape",      AspectRatio = 1.6,   Tolerance = 0.5,  MinWidth = 1280, MinHeight = 720 },
        };

        public MediaToolsSettingsView()
        {
            var mainStack = new StackPanel { Margin = new Thickness(20) };

            AddSection(mainStack, "General", stack =>
            {
                AddNumericField(stack, "Scan interval (minutes):", "ScanIntervalMinutes");
                AddCheckbox(stack, "Report missing media", "ReportMissing");
                AddCheckbox(stack, "Tag games with issues", "TagUndesiredMedia");
            });

            AddSection(mainStack, "Media Types to Check", stack =>
            {
                AddCheckbox(stack, "Check icons", "CheckIcons");
                AddCheckbox(stack, "Check covers", "CheckCovers");
                AddCheckbox(stack, "Check backgrounds", "CheckBackgrounds");
            });

            AddSection(mainStack, "Icon Standards", stack =>
            {
                AddPresetDropdown(stack, "Preset:", IconPresets, s =>
                {
                    s.IconAspectRatioTolerance = 0; // force notify
                    s.IconMinSize = 0;
                    s.IconMaxSize = 0;
                }, (s, p) =>
                {
                    s.IconAspectRatioTolerance = p.Tolerance;
                    s.IconMinSize = p.MinWidth;
                    s.IconMaxSize = p.MaxWidth;
                });
                AddTextField(stack, "Tag name:", "IconTagName");
                AddNumericField(stack, "Min size (px):", "IconMinSize");
                AddNumericField(stack, "Max size (px):", "IconMaxSize");
                AddNumericField(stack, "Aspect ratio tolerance:", "IconAspectRatioTolerance");
            });

            AddSection(mainStack, "Cover Standards", stack =>
            {
                AddPresetDropdown(stack, "Preset:", CoverPresets, s =>
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
                AddTextField(stack, "Tag name:", "CoverTagName");
                AddNumericField(stack, "Expected aspect ratio:", "CoverAspectRatio");
                AddNumericField(stack, "Aspect ratio tolerance:", "CoverAspectRatioTolerance");
                AddNumericField(stack, "Min width (px):", "CoverMinWidth");
                AddNumericField(stack, "Min height (px):", "CoverMinHeight");
            });

            AddSection(mainStack, "Background Standards", stack =>
            {
                AddPresetDropdown(stack, "Preset:", BackgroundPresets, s =>
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
                AddTextField(stack, "Tag name:", "BackgroundTagName");
                AddNumericField(stack, "Expected aspect ratio:", "BackgroundAspectRatio");
                AddNumericField(stack, "Aspect ratio tolerance:", "BackgroundAspectRatioTolerance");
                AddNumericField(stack, "Min width (px):", "BackgroundMinWidth");
                AddNumericField(stack, "Min height (px):", "BackgroundMinHeight");
            });

            AddSection(mainStack, "Extra Metadata - Logo", stack =>
            {
                AddCheckbox(stack, "Check logos", "CheckLogos");
                AddCheckbox(stack, "Installed games only", "LogoInstalledOnly");
                AddPresetDropdown(stack, "Preset:", LogoPresets, s =>
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
                AddTextField(stack, "Tag name:", "LogoTagName");
                AddNumericField(stack, "Expected aspect ratio:", "LogoAspectRatio");
                AddNumericField(stack, "Aspect ratio tolerance:", "LogoAspectRatioTolerance");
                AddNumericField(stack, "Min width (px):", "LogoMinWidth");
                AddNumericField(stack, "Min height (px):", "LogoMinHeight");
            });

            AddSection(mainStack, "Extra Metadata - Videos", stack =>
            {
                AddCheckbox(stack, "Check trailers", "CheckTrailers");
                AddCheckbox(stack, "Trailers: installed games only", "TrailerInstalledOnly");
                AddTextField(stack, "Trailer tag name:", "TrailerTagName");
                AddCheckbox(stack, "Check microtrailers", "CheckMicrotrailers");
                AddCheckbox(stack, "Microtrailers: installed games only", "MicrotrailerInstalledOnly");
                AddTextField(stack, "Microtrailer tag name:", "MicrotrailerTagName");
            });

            AddSection(mainStack, "PlayniteSound - Game Music", stack =>
            {
                AddCheckbox(stack, "Check game music", "CheckGameMusic");
                AddCheckbox(stack, "Installed games only", "GameMusicInstalledOnly");
                AddTextField(stack, "Tag name:", "GameMusicTagName");
            });

            Content = new ScrollViewer
            {
                Content = mainStack,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
        }

        private void AddPresetDropdown(StackPanel parent, string label, List<MediaPreset> presets,
            Action<MediaToolsSettings> clear, Action<MediaToolsSettings, MediaPreset> apply)
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
                var settings = DataContext as MediaToolsSettings;
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

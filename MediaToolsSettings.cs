using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MediaTools
{
    public class MediaToolsSettings : ISettings, INotifyPropertyChanged
    {
        private readonly MediaToolsPlugin _plugin;
        private MediaToolsSettings _previousSettings;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // General
        private int _scanIntervalMinutes = 60;
        public int ScanIntervalMinutes
        {
            get => _scanIntervalMinutes;
            set { _scanIntervalMinutes = value; NotifyPropertyChanged(); }
        }

        private bool _reportMissing = false;
        public bool ReportMissing
        {
            get => _reportMissing;
            set { _reportMissing = value; NotifyPropertyChanged(); }
        }

        private bool _tagUndesiredMedia = true;
        public bool TagUndesiredMedia
        {
            get => _tagUndesiredMedia;
            set { _tagUndesiredMedia = value; NotifyPropertyChanged(); }
        }

        private string _iconTagName = "Undesired Icon";
        public string IconTagName
        {
            get => _iconTagName;
            set { _iconTagName = value; NotifyPropertyChanged(); }
        }

        private string _coverTagName = "Undesired Cover";
        public string CoverTagName
        {
            get => _coverTagName;
            set { _coverTagName = value; NotifyPropertyChanged(); }
        }

        private string _backgroundTagName = "Undesired Background";
        public string BackgroundTagName
        {
            get => _backgroundTagName;
            set { _backgroundTagName = value; NotifyPropertyChanged(); }
        }

        // What to check
        private bool _checkIcons = true;
        public bool CheckIcons
        {
            get => _checkIcons;
            set { _checkIcons = value; NotifyPropertyChanged(); }
        }

        private bool _checkCovers = true;
        public bool CheckCovers
        {
            get => _checkCovers;
            set { _checkCovers = value; NotifyPropertyChanged(); }
        }

        private bool _checkBackgrounds = true;
        public bool CheckBackgrounds
        {
            get => _checkBackgrounds;
            set { _checkBackgrounds = value; NotifyPropertyChanged(); }
        }

        // Icon standards (square, 1:1)
        private int _iconMinSize = 64;
        public int IconMinSize
        {
            get => _iconMinSize;
            set { _iconMinSize = value; NotifyPropertyChanged(); }
        }

        private int _iconMaxSize = 512;
        public int IconMaxSize
        {
            get => _iconMaxSize;
            set { _iconMaxSize = value; NotifyPropertyChanged(); }
        }

        private double _iconAspectRatioTolerance = 0.1;
        public double IconAspectRatioTolerance
        {
            get => _iconAspectRatioTolerance;
            set { _iconAspectRatioTolerance = value; NotifyPropertyChanged(); }
        }

        // Cover standards (portrait, ~2:3)
        private double _coverAspectRatio = 0.667;
        public double CoverAspectRatio
        {
            get => _coverAspectRatio;
            set { _coverAspectRatio = value; NotifyPropertyChanged(); }
        }

        private double _coverAspectRatioTolerance = 0.15;
        public double CoverAspectRatioTolerance
        {
            get => _coverAspectRatioTolerance;
            set { _coverAspectRatioTolerance = value; NotifyPropertyChanged(); }
        }

        private int _coverMinWidth = 300;
        public int CoverMinWidth
        {
            get => _coverMinWidth;
            set { _coverMinWidth = value; NotifyPropertyChanged(); }
        }

        private int _coverMinHeight = 400;
        public int CoverMinHeight
        {
            get => _coverMinHeight;
            set { _coverMinHeight = value; NotifyPropertyChanged(); }
        }

        // Extra Metadata - Logo
        private bool _checkLogos = false;
        public bool CheckLogos
        {
            get => _checkLogos;
            set { _checkLogos = value; NotifyPropertyChanged(); }
        }

        private bool _logoInstalledOnly = true;
        public bool LogoInstalledOnly
        {
            get => _logoInstalledOnly;
            set { _logoInstalledOnly = value; NotifyPropertyChanged(); }
        }

        private string _logoTagName = "Undesired Logo";
        public string LogoTagName
        {
            get => _logoTagName;
            set { _logoTagName = value; NotifyPropertyChanged(); }
        }

        private double _logoAspectRatio = 2.5;
        public double LogoAspectRatio
        {
            get => _logoAspectRatio;
            set { _logoAspectRatio = value; NotifyPropertyChanged(); }
        }

        private double _logoAspectRatioTolerance = 1.5;
        public double LogoAspectRatioTolerance
        {
            get => _logoAspectRatioTolerance;
            set { _logoAspectRatioTolerance = value; NotifyPropertyChanged(); }
        }

        private int _logoMinWidth = 400;
        public int LogoMinWidth
        {
            get => _logoMinWidth;
            set { _logoMinWidth = value; NotifyPropertyChanged(); }
        }

        private int _logoMinHeight = 150;
        public int LogoMinHeight
        {
            get => _logoMinHeight;
            set { _logoMinHeight = value; NotifyPropertyChanged(); }
        }

        // Extra Metadata - Videos
        private bool _checkTrailers = false;
        public bool CheckTrailers
        {
            get => _checkTrailers;
            set { _checkTrailers = value; NotifyPropertyChanged(); }
        }

        private bool _trailerInstalledOnly = true;
        public bool TrailerInstalledOnly
        {
            get => _trailerInstalledOnly;
            set { _trailerInstalledOnly = value; NotifyPropertyChanged(); }
        }

        private string _trailerTagName = "Missing Trailer";
        public string TrailerTagName
        {
            get => _trailerTagName;
            set { _trailerTagName = value; NotifyPropertyChanged(); }
        }

        private bool _checkMicrotrailers = false;
        public bool CheckMicrotrailers
        {
            get => _checkMicrotrailers;
            set { _checkMicrotrailers = value; NotifyPropertyChanged(); }
        }

        private bool _microtrailerInstalledOnly = true;
        public bool MicrotrailerInstalledOnly
        {
            get => _microtrailerInstalledOnly;
            set { _microtrailerInstalledOnly = value; NotifyPropertyChanged(); }
        }

        private string _microtrailerTagName = "Missing Microtrailer";
        public string MicrotrailerTagName
        {
            get => _microtrailerTagName;
            set { _microtrailerTagName = value; NotifyPropertyChanged(); }
        }

        // Extra Metadata - Game Music (PlayniteSound)
        private bool _checkGameMusic = false;
        public bool CheckGameMusic
        {
            get => _checkGameMusic;
            set { _checkGameMusic = value; NotifyPropertyChanged(); }
        }

        private bool _gameMusicInstalledOnly = true;
        public bool GameMusicInstalledOnly
        {
            get => _gameMusicInstalledOnly;
            set { _gameMusicInstalledOnly = value; NotifyPropertyChanged(); }
        }

        private string _gameMusicTagName = "Missing Game Music";
        public string GameMusicTagName
        {
            get => _gameMusicTagName;
            set { _gameMusicTagName = value; NotifyPropertyChanged(); }
        }

        // Background standards (landscape, ~16:9)
        private double _backgroundAspectRatio = 1.778;
        public double BackgroundAspectRatio
        {
            get => _backgroundAspectRatio;
            set { _backgroundAspectRatio = value; NotifyPropertyChanged(); }
        }

        private double _backgroundAspectRatioTolerance = 0.3;
        public double BackgroundAspectRatioTolerance
        {
            get => _backgroundAspectRatioTolerance;
            set { _backgroundAspectRatioTolerance = value; NotifyPropertyChanged(); }
        }

        private int _backgroundMinWidth = 1280;
        public int BackgroundMinWidth
        {
            get => _backgroundMinWidth;
            set { _backgroundMinWidth = value; NotifyPropertyChanged(); }
        }

        private int _backgroundMinHeight = 720;
        public int BackgroundMinHeight
        {
            get => _backgroundMinHeight;
            set { _backgroundMinHeight = value; NotifyPropertyChanged(); }
        }

        public MediaToolsSettings() { }

        public MediaToolsSettings(MediaToolsPlugin plugin)
        {
            _plugin = plugin;
            var saved = plugin.LoadPluginSettings<MediaToolsSettings>();
            if (saved != null)
            {
                CopyFrom(saved);
            }
        }

        private void CopyFrom(MediaToolsSettings source)
        {
            ScanIntervalMinutes = source.ScanIntervalMinutes;
            ReportMissing = source.ReportMissing;
            TagUndesiredMedia = source.TagUndesiredMedia;
            IconTagName = source.IconTagName;
            CoverTagName = source.CoverTagName;
            BackgroundTagName = source.BackgroundTagName;
            CheckIcons = source.CheckIcons;
            CheckCovers = source.CheckCovers;
            CheckBackgrounds = source.CheckBackgrounds;
            IconMinSize = source.IconMinSize;
            IconMaxSize = source.IconMaxSize;
            IconAspectRatioTolerance = source.IconAspectRatioTolerance;
            CoverAspectRatio = source.CoverAspectRatio;
            CoverAspectRatioTolerance = source.CoverAspectRatioTolerance;
            CoverMinWidth = source.CoverMinWidth;
            CoverMinHeight = source.CoverMinHeight;
            BackgroundAspectRatio = source.BackgroundAspectRatio;
            BackgroundAspectRatioTolerance = source.BackgroundAspectRatioTolerance;
            BackgroundMinWidth = source.BackgroundMinWidth;
            BackgroundMinHeight = source.BackgroundMinHeight;
            CheckLogos = source.CheckLogos;
            LogoInstalledOnly = source.LogoInstalledOnly;
            LogoTagName = source.LogoTagName;
            LogoAspectRatio = source.LogoAspectRatio;
            LogoAspectRatioTolerance = source.LogoAspectRatioTolerance;
            LogoMinWidth = source.LogoMinWidth;
            LogoMinHeight = source.LogoMinHeight;
            CheckTrailers = source.CheckTrailers;
            TrailerInstalledOnly = source.TrailerInstalledOnly;
            TrailerTagName = source.TrailerTagName;
            CheckMicrotrailers = source.CheckMicrotrailers;
            MicrotrailerInstalledOnly = source.MicrotrailerInstalledOnly;
            MicrotrailerTagName = source.MicrotrailerTagName;
            CheckGameMusic = source.CheckGameMusic;
            GameMusicInstalledOnly = source.GameMusicInstalledOnly;
            GameMusicTagName = source.GameMusicTagName;
        }

        public void BeginEdit()
        {
            _previousSettings = Serialization.GetClone(this);
        }

        public void CancelEdit()
        {
            CopyFrom(_previousSettings);
        }

        public void EndEdit()
        {
            _plugin.SavePluginSettings(this);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            if (ScanIntervalMinutes < 1)
                errors.Add("Scan interval must be at least 1 minute.");
            if (IconMinSize < 1)
                errors.Add("Icon minimum size must be at least 1 pixel.");
            if (CoverMinWidth < 1 || CoverMinHeight < 1)
                errors.Add("Cover minimum dimensions must be at least 1 pixel.");
            if (BackgroundMinWidth < 1 || BackgroundMinHeight < 1)
                errors.Add("Background minimum dimensions must be at least 1 pixel.");
            return errors.Count == 0;
        }
    }
}

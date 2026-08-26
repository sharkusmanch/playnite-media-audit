using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MediaAudit
{
    public class MediaAuditSettings : ISettings, INotifyPropertyChanged
    {
        private readonly MediaAuditPlugin _plugin;
        private MediaAuditSettings _previousSettings;

        // Guards TagIds against a scan writing new tag GUIDs on the timer thread while
        // the UI thread serializes this object. Plain fields aren't serialized, so
        // neither of these ends up in the settings JSON.
        internal readonly object TagIdsLock = new object();

        // True between BeginEdit and End/CancelEdit. A scan that finishes in that window
        // must not persist settings, or it would commit edits the user may still cancel.
        internal bool IsEditing;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // General
        private bool _backgroundScanEnabled = true;
        public bool BackgroundScanEnabled
        {
            get => _backgroundScanEnabled;
            set { _backgroundScanEnabled = value; NotifyPropertyChanged(); }
        }

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

        private bool _showScanNotification = true;
        public bool ShowScanNotification
        {
            get => _showScanNotification;
            set { _showScanNotification = value; NotifyPropertyChanged(); }
        }

        private bool _tagUndesiredMedia = true;
        public bool TagUndesiredMedia
        {
            get => _tagUndesiredMedia;
            set { _tagUndesiredMedia = value; NotifyPropertyChanged(); }
        }

        // GUIDs of the tags this plugin owns, keyed by media type. Tags are managed by
        // ID rather than by name so that renaming one in settings renames the existing
        // tag instead of orphaning it and adopting a second one.
        private Dictionary<MediaType, Guid> _tagIds = new Dictionary<MediaType, Guid>();
        public Dictionary<MediaType, Guid> TagIds
        {
            get => _tagIds;
            set { _tagIds = value ?? new Dictionary<MediaType, Guid>(); NotifyPropertyChanged(); }
        }

        private string _iconTagName = "[Media Audit] Undesired Icon";
        public string IconTagName
        {
            get => _iconTagName;
            set { _iconTagName = value; NotifyPropertyChanged(); }
        }

        private string _coverTagName = "[Media Audit] Undesired Cover";
        public string CoverTagName
        {
            get => _coverTagName;
            set { _coverTagName = value; NotifyPropertyChanged(); }
        }

        private string _backgroundTagName = "[Media Audit] Undesired Background";
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

        // Icon standards (square, ~1:1)
        private double _iconMinAspectRatio = 0.9;
        public double IconMinAspectRatio
        {
            get => _iconMinAspectRatio;
            set { _iconMinAspectRatio = value; NotifyPropertyChanged(); }
        }

        private double _iconMaxAspectRatio = 1.1;
        public double IconMaxAspectRatio
        {
            get => _iconMaxAspectRatio;
            set { _iconMaxAspectRatio = value; NotifyPropertyChanged(); }
        }

        private int _iconMinWidth = 64;
        public int IconMinWidth
        {
            get => _iconMinWidth;
            set { _iconMinWidth = value; NotifyPropertyChanged(); }
        }

        private int _iconMaxWidth = 512;
        public int IconMaxWidth
        {
            get => _iconMaxWidth;
            set { _iconMaxWidth = value; NotifyPropertyChanged(); }
        }

        private int _iconMinHeight = 64;
        public int IconMinHeight
        {
            get => _iconMinHeight;
            set { _iconMinHeight = value; NotifyPropertyChanged(); }
        }

        private int _iconMaxHeight = 512;
        public int IconMaxHeight
        {
            get => _iconMaxHeight;
            set { _iconMaxHeight = value; NotifyPropertyChanged(); }
        }

        // Cover standards (portrait, ~2:3)
        private double _coverMinAspectRatio = 0.5;
        public double CoverMinAspectRatio
        {
            get => _coverMinAspectRatio;
            set { _coverMinAspectRatio = value; NotifyPropertyChanged(); }
        }

        private double _coverMaxAspectRatio = 0.9;
        public double CoverMaxAspectRatio
        {
            get => _coverMaxAspectRatio;
            set { _coverMaxAspectRatio = value; NotifyPropertyChanged(); }
        }

        private int _coverMinWidth = 300;
        public int CoverMinWidth
        {
            get => _coverMinWidth;
            set { _coverMinWidth = value; NotifyPropertyChanged(); }
        }

        private int _coverMaxWidth = 0;
        public int CoverMaxWidth
        {
            get => _coverMaxWidth;
            set { _coverMaxWidth = value; NotifyPropertyChanged(); }
        }

        private int _coverMinHeight = 400;
        public int CoverMinHeight
        {
            get => _coverMinHeight;
            set { _coverMinHeight = value; NotifyPropertyChanged(); }
        }

        private int _coverMaxHeight = 0;
        public int CoverMaxHeight
        {
            get => _coverMaxHeight;
            set { _coverMaxHeight = value; NotifyPropertyChanged(); }
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

        private string _logoTagName = "[Media Audit] Undesired Logo";
        public string LogoTagName
        {
            get => _logoTagName;
            set { _logoTagName = value; NotifyPropertyChanged(); }
        }

        private double _logoMinAspectRatio = 1.0;
        public double LogoMinAspectRatio
        {
            get => _logoMinAspectRatio;
            set { _logoMinAspectRatio = value; NotifyPropertyChanged(); }
        }

        private double _logoMaxAspectRatio = 4.0;
        public double LogoMaxAspectRatio
        {
            get => _logoMaxAspectRatio;
            set { _logoMaxAspectRatio = value; NotifyPropertyChanged(); }
        }

        private int _logoMinWidth = 400;
        public int LogoMinWidth
        {
            get => _logoMinWidth;
            set { _logoMinWidth = value; NotifyPropertyChanged(); }
        }

        private int _logoMaxWidth = 0;
        public int LogoMaxWidth
        {
            get => _logoMaxWidth;
            set { _logoMaxWidth = value; NotifyPropertyChanged(); }
        }

        private int _logoMinHeight = 150;
        public int LogoMinHeight
        {
            get => _logoMinHeight;
            set { _logoMinHeight = value; NotifyPropertyChanged(); }
        }

        private int _logoMaxHeight = 0;
        public int LogoMaxHeight
        {
            get => _logoMaxHeight;
            set { _logoMaxHeight = value; NotifyPropertyChanged(); }
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

        private string _trailerTagName = "[Media Audit] Missing Trailer";
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

        private string _microtrailerTagName = "[Media Audit] Missing Microtrailer";
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

        private string _gameMusicTagName = "[Media Audit] Missing Game Music";
        public string GameMusicTagName
        {
            get => _gameMusicTagName;
            set { _gameMusicTagName = value; NotifyPropertyChanged(); }
        }

        // Background standards (landscape, ~16:9)
        private double _backgroundMinAspectRatio = 1.3;
        public double BackgroundMinAspectRatio
        {
            get => _backgroundMinAspectRatio;
            set { _backgroundMinAspectRatio = value; NotifyPropertyChanged(); }
        }

        private double _backgroundMaxAspectRatio = 2.4;
        public double BackgroundMaxAspectRatio
        {
            get => _backgroundMaxAspectRatio;
            set { _backgroundMaxAspectRatio = value; NotifyPropertyChanged(); }
        }

        private int _backgroundMinWidth = 1280;
        public int BackgroundMinWidth
        {
            get => _backgroundMinWidth;
            set { _backgroundMinWidth = value; NotifyPropertyChanged(); }
        }

        private int _backgroundMaxWidth = 0;
        public int BackgroundMaxWidth
        {
            get => _backgroundMaxWidth;
            set { _backgroundMaxWidth = value; NotifyPropertyChanged(); }
        }

        private int _backgroundMinHeight = 720;
        public int BackgroundMinHeight
        {
            get => _backgroundMinHeight;
            set { _backgroundMinHeight = value; NotifyPropertyChanged(); }
        }

        private int _backgroundMaxHeight = 0;
        public int BackgroundMaxHeight
        {
            get => _backgroundMaxHeight;
            set { _backgroundMaxHeight = value; NotifyPropertyChanged(); }
        }

        public MediaAuditSettings() { }

        public MediaAuditSettings(MediaAuditPlugin plugin)
        {
            _plugin = plugin;
            var saved = plugin.LoadPluginSettings<MediaAuditSettings>();
            if (saved != null)
            {
                CopyFrom(saved);
            }
        }

        // Media types the current settings actually audit. Tag handling keys off this,
        // so a disabled check neither creates its tag nor strips it from games.
        internal IEnumerable<MediaType> EnabledMediaTypes()
        {
            if (CheckIcons) yield return MediaType.Icon;
            if (CheckCovers) yield return MediaType.Cover;
            if (CheckBackgrounds) yield return MediaType.Background;
            if (CheckLogos) yield return MediaType.Logo;
            if (CheckTrailers) yield return MediaType.Trailer;
            if (CheckMicrotrailers) yield return MediaType.Microtrailer;
            if (CheckGameMusic) yield return MediaType.GameMusic;
        }

        internal string TagNameFor(MediaType mediaType)
        {
            switch (mediaType)
            {
                case MediaType.Icon: return IconTagName;
                case MediaType.Cover: return CoverTagName;
                case MediaType.Background: return BackgroundTagName;
                case MediaType.Logo: return LogoTagName;
                case MediaType.Trailer: return TrailerTagName;
                case MediaType.Microtrailer: return MicrotrailerTagName;
                case MediaType.GameMusic: return GameMusicTagName;
                default: return null;
            }
        }

        private void CopyFrom(MediaAuditSettings source)
        {
            BackgroundScanEnabled = source.BackgroundScanEnabled;
            ScanIntervalMinutes = source.ScanIntervalMinutes;
            ReportMissing = source.ReportMissing;
            ShowScanNotification = source.ShowScanNotification;
            TagUndesiredMedia = source.TagUndesiredMedia;
            TagIds = source.TagIds == null
                ? new Dictionary<MediaType, Guid>()
                : new Dictionary<MediaType, Guid>(source.TagIds);
            IconTagName = source.IconTagName;
            CoverTagName = source.CoverTagName;
            BackgroundTagName = source.BackgroundTagName;
            CheckIcons = source.CheckIcons;
            CheckCovers = source.CheckCovers;
            CheckBackgrounds = source.CheckBackgrounds;
            IconMinAspectRatio = source.IconMinAspectRatio;
            IconMaxAspectRatio = source.IconMaxAspectRatio;
            IconMinWidth = source.IconMinWidth;
            IconMaxWidth = source.IconMaxWidth;
            IconMinHeight = source.IconMinHeight;
            IconMaxHeight = source.IconMaxHeight;
            CoverMinAspectRatio = source.CoverMinAspectRatio;
            CoverMaxAspectRatio = source.CoverMaxAspectRatio;
            CoverMinWidth = source.CoverMinWidth;
            CoverMaxWidth = source.CoverMaxWidth;
            CoverMinHeight = source.CoverMinHeight;
            CoverMaxHeight = source.CoverMaxHeight;
            BackgroundMinAspectRatio = source.BackgroundMinAspectRatio;
            BackgroundMaxAspectRatio = source.BackgroundMaxAspectRatio;
            BackgroundMinWidth = source.BackgroundMinWidth;
            BackgroundMaxWidth = source.BackgroundMaxWidth;
            BackgroundMinHeight = source.BackgroundMinHeight;
            BackgroundMaxHeight = source.BackgroundMaxHeight;
            CheckLogos = source.CheckLogos;
            LogoInstalledOnly = source.LogoInstalledOnly;
            LogoTagName = source.LogoTagName;
            LogoMinAspectRatio = source.LogoMinAspectRatio;
            LogoMaxAspectRatio = source.LogoMaxAspectRatio;
            LogoMinWidth = source.LogoMinWidth;
            LogoMaxWidth = source.LogoMaxWidth;
            LogoMinHeight = source.LogoMinHeight;
            LogoMaxHeight = source.LogoMaxHeight;
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
            IsEditing = true;
            lock (TagIdsLock)
            {
                // GetClone serializes this object, which enumerates TagIds. A scan on the
                // timer thread writes to that same dictionary, so the clone has to take
                // the lock or opening settings mid-scan can throw.
                _previousSettings = Serialization.GetClone(this);
            }
        }

        public void CancelEdit()
        {
            lock (TagIdsLock)
            {
                // Tag ownership isn't part of the edit transaction. A scan may have created
                // tags and applied them to games while the dialog was open; reverting those
                // GUIDs would orphan exactly the tags this plugin just started managing.
                // The swap has to be atomic against a scan: CopyFrom installs a dictionary
                // from the stale snapshot, and a GUID written into it before the restore
                // below would be thrown away with it.
                var ownedTagIds = TagIds;
                CopyFrom(_previousSettings);
                TagIds = ownedTagIds;
            }
            IsEditing = false;
        }

        public void EndEdit()
        {
            IsEditing = false;
            lock (TagIdsLock)
            {
                _plugin.SavePluginSettings(this);
            }
            // Pick up a changed interval or a toggled-off background scan now, rather
            // than leaving the old schedule running until Playnite restarts.
            _plugin.ApplyScanSchedule();
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            if (BackgroundScanEnabled && ScanIntervalMinutes < 1)
                errors.Add(ResourceProvider.GetString("LOC_MediaAudit_Validation_ScanInterval"));
            if (IconMinWidth < 1 || IconMinHeight < 1)
                errors.Add(ResourceProvider.GetString("LOC_MediaAudit_Validation_IconMinDimensions"));
            if (CoverMinWidth < 1 || CoverMinHeight < 1)
                errors.Add(ResourceProvider.GetString("LOC_MediaAudit_Validation_CoverMinDimensions"));
            if (BackgroundMinWidth < 1 || BackgroundMinHeight < 1)
                errors.Add(ResourceProvider.GetString("LOC_MediaAudit_Validation_BackgroundMinDimensions"));
            if (IconMinAspectRatio > IconMaxAspectRatio)
                errors.Add("Icon: Min aspect ratio cannot exceed max aspect ratio.");
            if (CoverMinAspectRatio > CoverMaxAspectRatio)
                errors.Add("Cover: Min aspect ratio cannot exceed max aspect ratio.");
            if (LogoMinAspectRatio > LogoMaxAspectRatio)
                errors.Add("Logo: Min aspect ratio cannot exceed max aspect ratio.");
            if (BackgroundMinAspectRatio > BackgroundMaxAspectRatio)
                errors.Add("Background: Min aspect ratio cannot exceed max aspect ratio.");
            return errors.Count == 0;
        }
    }
}
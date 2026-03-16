using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace MediaAudit
{
    public class MediaScanner
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly IPlayniteAPI _api;
        private readonly MediaAuditSettings _settings;
        private readonly string _extraMetadataPath;
        private readonly string _playniteSoundPath;

        public MediaScanner(IPlayniteAPI api, MediaAuditSettings settings)
        {
            _api = api;
            _settings = settings;
            _extraMetadataPath = Path.Combine(api.Paths.ConfigurationPath, "ExtraMetadata", "games");
            _playniteSoundPath = Path.Combine(api.Paths.ConfigurationPath, "ExtraMetadata", "PlayniteSound", "games");
        }

        public List<MediaIssue> ScanAll()
        {
            return ScanGames(_api.Database.Games.ToList());
        }

        public List<MediaIssue> ScanGames(IEnumerable<Game> games)
        {
            var issues = new List<MediaIssue>();
            foreach (var game in games)
            {
                if (_settings.CheckIcons)
                    CheckMedia(game, game.Icon, MediaType.Icon, issues);
                if (_settings.CheckCovers)
                    CheckMedia(game, game.CoverImage, MediaType.Cover, issues);
                if (_settings.CheckBackgrounds)
                    CheckMedia(game, game.BackgroundImage, MediaType.Background, issues);
                if (_settings.CheckLogos && ShouldCheckExtra(game, _settings.LogoInstalledOnly))
                    CheckExtraImage(game, "Logo.png", MediaType.Logo, issues);
                if (_settings.CheckTrailers && ShouldCheckExtra(game, _settings.TrailerInstalledOnly))
                    CheckExtraFileExists(game, "VideoTrailer.mp4", MediaType.Trailer, issues);
                if (_settings.CheckMicrotrailers && ShouldCheckExtra(game, _settings.MicrotrailerInstalledOnly))
                    CheckExtraFileExists(game, "VideoMicrotrailer.mp4", MediaType.Microtrailer, issues);
                if (_settings.CheckGameMusic && ShouldCheckExtra(game, _settings.GameMusicInstalledOnly))
                    CheckGameMusicExists(game, issues);
            }
            return issues;
        }

        private static bool ShouldCheckExtra(Game game, bool installedOnly)
        {
            return !installedOnly || game.IsInstalled;
        }

        private void CheckGameMusicExists(Game game, List<MediaIssue> issues)
        {
            var musicDir = Path.Combine(_playniteSoundPath, game.Id.ToString());
            if (!Directory.Exists(musicDir) ||
                !Directory.GetFiles(musicDir, "*.mp3").Any())
            {
                issues.Add(new MediaIssue
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    MediaType = MediaType.GameMusic,
                    IssueType = IssueType.Missing,
                    Description = "No game music found"
                });
            }
        }

        private string GetExtraMetadataFilePath(Game game, string fileName)
        {
            return Path.Combine(_extraMetadataPath, game.Id.ToString(), fileName);
        }

        private void CheckExtraFileExists(Game game, string fileName, MediaType mediaType, List<MediaIssue> issues)
        {
            var filePath = GetExtraMetadataFilePath(game, fileName);
            if (!File.Exists(filePath))
            {
                issues.Add(new MediaIssue
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    MediaType = mediaType,
                    IssueType = IssueType.Missing,
                    Description = $"No {mediaType.ToString().ToLower()} found"
                });
            }
        }

        private void CheckExtraImage(Game game, string fileName, MediaType mediaType, List<MediaIssue> issues)
        {
            var filePath = GetExtraMetadataFilePath(game, fileName);
            if (!File.Exists(filePath))
            {
                issues.Add(new MediaIssue
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    MediaType = mediaType,
                    IssueType = IssueType.Missing,
                    Description = $"No {mediaType.ToString().ToLower()} found"
                });
                return;
            }

            CheckImageFile(game, filePath, mediaType, issues);
        }

        private void CheckMedia(Game game, string mediaRef, MediaType mediaType, List<MediaIssue> issues)
        {
            if (string.IsNullOrEmpty(mediaRef))
            {
                if (_settings.ReportMissing)
                {
                    issues.Add(new MediaIssue
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        MediaType = mediaType,
                        IssueType = IssueType.Missing,
                        Description = $"No {mediaType.ToString().ToLower()} set"
                    });
                }
                return;
            }

            if (mediaRef.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return;

            string filePath;
            try
            {
                filePath = _api.Database.GetFullFilePath(mediaRef);
            }
            catch
            {
                return;
            }

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            CheckImageFile(game, filePath, mediaType, issues);
        }

        private void CheckImageFile(Game game, string filePath, MediaType mediaType, List<MediaIssue> issues)
        {
            try
            {
                int width, height;
                var bytes = File.ReadAllBytes(filePath);
                using (var ms = new MemoryStream(bytes))
                using (var img = Image.FromStream(ms, false, false))
                {
                    width = img.Width;
                    height = img.Height;
                }

                var standards = GetStandards(mediaType);
                double aspectRatio = (double)width / height;

                if (Math.Abs(aspectRatio - standards.ExpectedAspectRatio) > standards.AspectRatioTolerance)
                {
                    issues.Add(new MediaIssue
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        MediaType = mediaType,
                        IssueType = IssueType.BadAspectRatio,
                        Width = width,
                        Height = height,
                        Description = $"Aspect ratio {aspectRatio:F2} (expected ~{standards.ExpectedAspectRatio:F2}), {width}x{height}"
                    });
                }

                if (width < standards.MinWidth || height < standards.MinHeight)
                {
                    issues.Add(new MediaIssue
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        MediaType = mediaType,
                        IssueType = IssueType.LowResolution,
                        Width = width,
                        Height = height,
                        Description = $"Too low: {width}x{height} (min {standards.MinWidth}x{standards.MinHeight})"
                    });
                }

                if (standards.MaxWidth > 0 && standards.MaxHeight > 0 &&
                    (width > standards.MaxWidth || height > standards.MaxHeight))
                {
                    issues.Add(new MediaIssue
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        MediaType = mediaType,
                        IssueType = IssueType.HighResolution,
                        Width = width,
                        Height = height,
                        Description = $"Too high: {width}x{height} (max {standards.MaxWidth}x{standards.MaxHeight})"
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to check {mediaType} for '{game.Name}': {ex.Message}");
            }
        }

        private MediaStandards GetStandards(MediaType type)
        {
            switch (type)
            {
                case MediaType.Icon:
                    return new MediaStandards
                    {
                        ExpectedAspectRatio = 1.0,
                        AspectRatioTolerance = _settings.IconAspectRatioTolerance,
                        MinWidth = _settings.IconMinSize,
                        MinHeight = _settings.IconMinSize,
                        MaxWidth = _settings.IconMaxSize,
                        MaxHeight = _settings.IconMaxSize
                    };
                case MediaType.Cover:
                    return new MediaStandards
                    {
                        ExpectedAspectRatio = _settings.CoverAspectRatio,
                        AspectRatioTolerance = _settings.CoverAspectRatioTolerance,
                        MinWidth = _settings.CoverMinWidth,
                        MinHeight = _settings.CoverMinHeight
                    };
                case MediaType.Background:
                    return new MediaStandards
                    {
                        ExpectedAspectRatio = _settings.BackgroundAspectRatio,
                        AspectRatioTolerance = _settings.BackgroundAspectRatioTolerance,
                        MinWidth = _settings.BackgroundMinWidth,
                        MinHeight = _settings.BackgroundMinHeight
                    };
                case MediaType.Logo:
                    return new MediaStandards
                    {
                        ExpectedAspectRatio = _settings.LogoAspectRatio,
                        AspectRatioTolerance = _settings.LogoAspectRatioTolerance,
                        MinWidth = _settings.LogoMinWidth,
                        MinHeight = _settings.LogoMinHeight
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}

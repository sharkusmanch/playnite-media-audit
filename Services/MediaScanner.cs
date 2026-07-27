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

        public ScanResult ScanGames(IEnumerable<Game> games)
        {
            var result = new ScanResult();
            foreach (var game in games)
            {
                try
                {
                    if (_settings.CheckIcons)
                        CheckMedia(game, game.Icon, MediaType.Icon, result);
                    if (_settings.CheckCovers)
                        CheckMedia(game, game.CoverImage, MediaType.Cover, result);
                    if (_settings.CheckBackgrounds)
                        CheckMedia(game, game.BackgroundImage, MediaType.Background, result);
                    // "Installed only" narrows what gets looked at, not what is known to
                    // be fine. Skipping an uninstalled game leaves its media unevaluated,
                    // so its tags must survive rather than churn on every install cycle.
                    if (_settings.CheckLogos)
                    {
                        if (ShouldCheckExtra(game, _settings.LogoInstalledOnly))
                            CheckExtraImage(game, "Logo.png", MediaType.Logo, result);
                        else
                            result.MarkIndeterminate(game.Id, MediaType.Logo);
                    }
                    if (_settings.CheckTrailers)
                    {
                        if (ShouldCheckExtra(game, _settings.TrailerInstalledOnly))
                            CheckExtraFileExists(game, "VideoTrailer.mp4", MediaType.Trailer, result);
                        else
                            result.MarkIndeterminate(game.Id, MediaType.Trailer);
                    }
                    if (_settings.CheckMicrotrailers)
                    {
                        if (ShouldCheckExtra(game, _settings.MicrotrailerInstalledOnly))
                            CheckExtraFileExists(game, "VideoMicrotrailer.mp4", MediaType.Microtrailer, result);
                        else
                            result.MarkIndeterminate(game.Id, MediaType.Microtrailer);
                    }
                    if (_settings.CheckGameMusic)
                    {
                        if (ShouldCheckExtra(game, _settings.GameMusicInstalledOnly))
                            CheckGameMusicExists(game, result);
                        else
                            result.MarkIndeterminate(game.Id, MediaType.GameMusic);
                    }
                }
                catch (Exception ex)
                {
                    // One unscannable game must not abort the batch. Nothing it would
                    // have reported is known, so leave all of its tags alone.
                    logger.Error(ex, $"Failed to scan media for '{game.Name}'.");
                    foreach (var mediaType in _settings.EnabledMediaTypes())
                        result.MarkIndeterminate(game.Id, mediaType);
                }
            }
            return result;
        }

        private static bool ShouldCheckExtra(Game game, bool installedOnly)
        {
            return !installedOnly || game.IsInstalled;
        }

        private void CheckGameMusicExists(Game game, ScanResult result)
        {
            var musicDir = Path.Combine(_playniteSoundPath, game.Id.ToString());
            if (!Directory.Exists(musicDir) ||
                !Directory.GetFiles(musicDir, "*.mp3").Any())
            {
                result.Issues.Add(new MediaIssue
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

        private void CheckExtraFileExists(Game game, string fileName, MediaType mediaType, ScanResult result)
        {
            var filePath = GetExtraMetadataFilePath(game, fileName);
            if (!File.Exists(filePath))
            {
                result.Issues.Add(new MediaIssue
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    MediaType = mediaType,
                    IssueType = IssueType.Missing,
                    Description = $"No {mediaType.ToString().ToLower()} found"
                });
            }
        }

        private void CheckExtraImage(Game game, string fileName, MediaType mediaType, ScanResult result)
        {
            var filePath = GetExtraMetadataFilePath(game, fileName);
            if (!File.Exists(filePath))
            {
                result.Issues.Add(new MediaIssue
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    MediaType = mediaType,
                    IssueType = IssueType.Missing,
                    Description = $"No {mediaType.ToString().ToLower()} found"
                });
                return;
            }

            CheckImageFile(game, filePath, mediaType, result);
        }

        private void CheckMedia(Game game, string mediaRef, MediaType mediaType, ScanResult result)
        {
            if (string.IsNullOrEmpty(mediaRef))
            {
                if (_settings.ReportMissing)
                {
                    result.Issues.Add(new MediaIssue
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
            {
                // Nothing local to measure yet, which is not the same as conforming.
                result.MarkIndeterminate(game.Id, mediaType);
                return;
            }

            string filePath;
            try
            {
                filePath = _api.Database.GetFullFilePath(mediaRef);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Failed to resolve file path for media ref '{mediaRef}' on game '{game.Name}'.");
                result.MarkIndeterminate(game.Id, mediaType);
                return;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                logger.Warn($"Playnite resolved no path for media ref '{mediaRef}' on game '{game.Name}'.");
                result.MarkIndeterminate(game.Id, mediaType);
                return;
            }

            if (!File.Exists(filePath))
            {
                // A reference pointing at a file that is gone is a genuine defect, and
                // reporting it is the point of the audit. ReportMissing governs media
                // that was never set, which is a different thing, so it doesn't gate this.
                result.Issues.Add(new MediaIssue
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    MediaType = mediaType,
                    IssueType = IssueType.Missing,
                    Description = $"{mediaType} file is referenced but missing: {filePath}"
                });
                return;
            }

            CheckImageFile(game, filePath, mediaType, result);
        }

        // An .ico holds several frames. GDI+ surfaces the smallest of them (16x16 on a
        // typical 8-frame icon) regardless of frame order, which reads a well-formed
        // 64x64 icon as undersized. The ICONDIR header lists every frame, so use it.
        private static bool TryGetIconDimensions(byte[] bytes, out int width, out int height)
        {
            width = 0;
            height = 0;

            // ICONDIR: reserved (0), type (1 = icon), frame count — all little-endian.
            if (bytes.Length < 6 || bytes[0] != 0 || bytes[1] != 0 || bytes[2] != 1 || bytes[3] != 0)
                return false;

            int count = BitConverter.ToUInt16(bytes, 4);
            if (count == 0 || bytes.Length < 6 + (count * 16))
                return false;

            for (int i = 0; i < count; i++)
            {
                int entry = 6 + (i * 16);

                // Every frame's pixel data must actually be present. A truncated icon
                // with an intact directory would otherwise report a confident size for
                // bytes that aren't there; bail out and let GDI+ fail it into
                // "indeterminate" instead of inventing a verdict.
                long imageOffset = BitConverter.ToUInt32(bytes, entry + 12);
                long bytesInRes = BitConverter.ToUInt32(bytes, entry + 8);
                if (imageOffset < 6 || bytesInRes <= 0 || imageOffset + bytesInRes > bytes.Length)
                {
                    width = 0;
                    height = 0;
                    return false;
                }

                // Dimensions are one byte each, where 0 encodes 256.
                int frameWidth = bytes[entry] == 0 ? 256 : bytes[entry];
                int frameHeight = bytes[entry + 1] == 0 ? 256 : bytes[entry + 1];
                if ((long)frameWidth * frameHeight > (long)width * height)
                {
                    width = frameWidth;
                    height = frameHeight;
                }
            }

            return width > 0 && height > 0;
        }

        private void CheckImageFile(Game game, string filePath, MediaType mediaType, ScanResult result)
        {
            int width, height;
            try
            {
                var bytes = File.ReadAllBytes(filePath);
                if (!TryGetIconDimensions(bytes, out width, out height))
                {
                    using (var ms = new MemoryStream(bytes))
                    using (var img = Image.FromStream(ms, false, false))
                    {
                        width = img.Width;
                        height = img.Height;
                    }
                }
            }
            catch (Exception ex)
            {
                // Conformance is unknown, not confirmed. Recording that stops ApplyTags
                // from reading "no issue" as "conforming" and removing the tag.
                logger.Warn(ex, $"Failed to check {mediaType} for '{game.Name}'.");
                result.MarkIndeterminate(game.Id, mediaType);
                return;
            }

            if (width <= 0 || height <= 0)
            {
                logger.Warn($"{mediaType} for '{game.Name}' reported unusable dimensions {width}x{height}.");
                result.MarkIndeterminate(game.Id, mediaType);
                return;
            }

            var standards = GetStandards(mediaType);
            double aspectRatio = (double)width / height;

            if (Math.Abs(aspectRatio - standards.ExpectedAspectRatio) > standards.AspectRatioTolerance)
            {
                result.Issues.Add(new MediaIssue
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
                result.Issues.Add(new MediaIssue
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
                result.Issues.Add(new MediaIssue
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

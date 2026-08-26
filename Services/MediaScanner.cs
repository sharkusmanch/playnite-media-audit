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
                // A reference pointing at a file that is gone was previously skipped in
                // silence, which then drove tag removal. It is reported now, but under
                // ReportMissing: it is still a missing-media report, and gating it means
                // upgrading users don't find new tags they never opted into.
                if (_settings.ReportMissing)
                {
                    result.Issues.Add(new MediaIssue
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        MediaType = mediaType,
                        IssueType = IssueType.Missing,
                        Description = $"{mediaType} file is referenced but missing: {filePath}"
                    });
                }

                return;
            }

            CheckImageFile(game, filePath, mediaType, result);
        }

        // An .ico holds several frames. GDI+ surfaces the smallest of them (16x16 on a
        // typical 8-frame icon) regardless of frame order, which reads a well-formed
        // 64x64 icon as undersized. The ICONDIR header lists every frame, so use it.
        // internal rather than private so the tests can call it directly.
        internal static bool TryGetIconDimensions(byte[] bytes, out int width, out int height)
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
                if (imageOffset < 6 + (count * 16) || bytesInRes <= 0 || imageOffset + bytesInRes > bytes.Length)
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
            bool hasIssue = false;
            var issues = new List<string>();

            // Check aspect ratio range
            if (standards.MinAspectRatio > 0 && standards.MaxAspectRatio > 0)
            {
                if (aspectRatio < standards.MinAspectRatio || aspectRatio > standards.MaxAspectRatio)
                {
                    hasIssue = true;
                    issues.Add($"Aspect ratio {aspectRatio:F2} out of range ({standards.MinAspectRatio:F2}-{standards.MaxAspectRatio:F2})");
                    result.Issues.Add(new MediaIssue
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        MediaType = mediaType,
                        IssueType = IssueType.BadAspectRatio,
                        Width = width,
                        Height = height,
                        Description = $"Aspect ratio {aspectRatio:F2} out of range ({standards.MinAspectRatio:F2}-{standards.MaxAspectRatio:F2}), {width}x{height}"
                    });
                }
            }

            // Check width range
            if (standards.MinWidth > 0 && width < standards.MinWidth)
            {
                hasIssue = true;
                issues.Add($"Width {width} below minimum {standards.MinWidth}");
                result.Issues.Add(new MediaIssue
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    MediaType = mediaType,
                    IssueType = IssueType.LowResolution,
                    Width = width,
                    Height = height,
                    Description = $"Width {width} below minimum {standards.MinWidth}, {width}x{height}"
                });
            }
            else if (standards.MaxWidth > 0 && width > standards.MaxWidth)
            {
                hasIssue = true;
                issues.Add($"Width {width} exceeds maximum {standards.MaxWidth}");
                result.Issues.Add(new MediaIssue
                {
                    GameId = game.Id,
                    GameName = game.Name,
                    MediaType = mediaType,
                    IssueType = IssueType.HighResolution,
                    Width = width,
                    Height = height,
                    Description = $"Width {width} exceeds maximum {standards.MaxWidth}, {width}x{height}"
                });
            }

            // Check height range
            if (standards.MinHeight > 0 && height < standards.MinHeight)
            {
                hasIssue = true;
                issues.Add($"Height {height} below minimum {standards.MinHeight}");
                // Only add if not already added for width issue
                if (issues.Count == 1 || !issues.Any(i => i.Contains("Height")))
                {
                    result.Issues.Add(new MediaIssue
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        MediaType = mediaType,
                        IssueType = IssueType.LowResolution,
                        Width = width,
                        Height = height,
                        Description = $"Height {height} below minimum {standards.MinHeight}, {width}x{height}"
                    });
                }
            }
            else if (standards.MaxHeight > 0 && height > standards.MaxHeight)
            {
                hasIssue = true;
                issues.Add($"Height {height} exceeds maximum {standards.MaxHeight}");
                if (issues.Count == 1 || !issues.Any(i => i.Contains("Height")))
                {
                    result.Issues.Add(new MediaIssue
                    {
                        GameId = game.Id,
                        GameName = game.Name,
                        MediaType = mediaType,
                        IssueType = IssueType.HighResolution,
                        Width = width,
                        Height = height,
                        Description = $"Height {height} exceeds maximum {standards.MaxHeight}, {width}x{height}"
                    });
                }
            }

            if (!hasIssue && _settings.ReportMissing)
            {
                // No issues found, but we don't add any issue here
                // This is fine - the image meets all criteria
            }
        }

        private MediaStandards GetStandards(MediaType type)
        {
            switch (type)
            {
                case MediaType.Icon:
                    return new MediaStandards
                    {
                        MinAspectRatio = _settings.IconMinAspectRatio,
                        MaxAspectRatio = _settings.IconMaxAspectRatio,
                        MinWidth = _settings.IconMinWidth,
                        MaxWidth = _settings.IconMaxWidth,
                        MinHeight = _settings.IconMinHeight,
                        MaxHeight = _settings.IconMaxHeight
                    };
                case MediaType.Cover:
                    return new MediaStandards
                    {
                        MinAspectRatio = _settings.CoverMinAspectRatio,
                        MaxAspectRatio = _settings.CoverMaxAspectRatio,
                        MinWidth = _settings.CoverMinWidth,
                        MaxWidth = _settings.CoverMaxWidth,
                        MinHeight = _settings.CoverMinHeight,
                        MaxHeight = _settings.CoverMaxHeight
                    };
                case MediaType.Background:
                    return new MediaStandards
                    {
                        MinAspectRatio = _settings.BackgroundMinAspectRatio,
                        MaxAspectRatio = _settings.BackgroundMaxAspectRatio,
                        MinWidth = _settings.BackgroundMinWidth,
                        MaxWidth = _settings.BackgroundMaxWidth,
                        MinHeight = _settings.BackgroundMinHeight,
                        MaxHeight = _settings.BackgroundMaxHeight
                    };
                case MediaType.Logo:
                    return new MediaStandards
                    {
                        MinAspectRatio = _settings.LogoMinAspectRatio,
                        MaxAspectRatio = _settings.LogoMaxAspectRatio,
                        MinWidth = _settings.LogoMinWidth,
                        MaxWidth = _settings.LogoMaxWidth,
                        MinHeight = _settings.LogoMinHeight,
                        MaxHeight = _settings.LogoMaxHeight
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
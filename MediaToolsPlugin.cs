using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MediaTools
{
    public class MediaToolsPlugin : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("2e6b5e8a-c42d-4e5b-9f1a-3d7c8e4a6b2f");

        internal MediaToolsSettings Settings { get; set; }
        private Timer _scanTimer;
        private Timer _debounceTimer;
        private readonly object _scanLock = new object();
        private readonly HashSet<Guid> _pendingGameIds = new HashSet<Guid>();
        private readonly object _pendingLock = new object();

        public MediaToolsPlugin(IPlayniteAPI api) : base(api)
        {
            Settings = new MediaToolsSettings(this);
            Properties = new GenericPluginProperties { HasSettings = true };
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            var interval = TimeSpan.FromMinutes(Settings.ScanIntervalMinutes);
            _scanTimer = new Timer(_ => RunBackgroundScan(), null, TimeSpan.FromMinutes(1), interval);

            PlayniteApi.Database.Games.ItemUpdated += OnGamesUpdated;
        }

        private void OnGamesUpdated(object sender, ItemUpdatedEventArgs<Game> args)
        {
            // Only care about media field changes
            var mediaChanged = args.UpdatedItems.Where(u =>
            {
                var o = u.OldData;
                var n = u.NewData;
                return o.Icon != n.Icon
                    || o.CoverImage != n.CoverImage
                    || o.BackgroundImage != n.BackgroundImage;
            }).Select(u => u.NewData.Id);

            lock (_pendingLock)
            {
                foreach (var id in mediaChanged)
                    _pendingGameIds.Add(id);

                if (_pendingGameIds.Count == 0)
                    return;
            }

            // Debounce: wait 5 seconds after last change before scanning
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ => FlushPendingGames(), null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
        }

        private void FlushPendingGames()
        {
            List<Game> games;
            lock (_pendingLock)
            {
                if (_pendingGameIds.Count == 0)
                    return;

                games = _pendingGameIds
                    .Select(id => PlayniteApi.Database.Games.Get(id))
                    .Where(g => g != null)
                    .ToList();
                _pendingGameIds.Clear();
            }

            if (games.Count > 0)
                ScanAndTag(games);
        }

        private void ScanAndTag(List<Game> games)
        {
            lock (_scanLock)
            {
                var scanner = new MediaScanner(PlayniteApi, Settings);
                var issues = scanner.ScanGames(games);
                var scannedGameIds = games.Select(g => g.Id).ToHashSet();
                ApplyTags(issues, scannedGameIds);
                logger.Info($"Media scan (item update): {games.Count} games checked, {issues.Count} issues.");
            }
        }

        public override void Dispose()
        {
            PlayniteApi.Database.Games.ItemUpdated -= OnGamesUpdated;
            _scanTimer?.Dispose();
            _debounceTimer?.Dispose();
            base.Dispose();
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                Description = "Scan All Game Media",
                MenuSection = "@Media Tools",
                Action = _ => Task.Run(() => RunManualScan(PlayniteApi.Database.Games.ToList()))
            };
            yield return new MainMenuItem
            {
                Description = "Scan Selected Games' Media",
                MenuSection = "@Media Tools",
                Action = _ =>
                {
                    var selected = PlayniteApi.MainView.SelectedGames?.ToList();
                    if (selected == null || selected.Count == 0)
                    {
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "MediaTools_NoSelection",
                            "Media Tools: No games selected.",
                            NotificationType.Info));
                        return;
                    }
                    Task.Run(() => RunManualScan(selected));
                }
            };
        }

        private void RunBackgroundScan()
        {
            if (!Monitor.TryEnter(_scanLock))
                return;

            try
            {
                logger.Info("Starting background media scan...");
                var scanner = new MediaScanner(PlayniteApi, Settings);
                var issues = scanner.ScanAll();

                var scannedGameIds = PlayniteApi.Database.Games.Select(g => g.Id).ToHashSet();
                ApplyTags(issues, scannedGameIds);

                if (issues.Count > 0)
                {
                    var summary = string.Join(", ",
                        issues.GroupBy(i => i.MediaType)
                              .Select(g => $"{g.Count()} {g.Key}"));

                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "MediaTools_BackgroundScan",
                        $"Media Tools: {issues.Count} issues found ({summary}).",
                        NotificationType.Info));
                }

                logger.Info($"Background media scan complete. {issues.Count} issues found.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error during background media scan.");
            }
            finally
            {
                Monitor.Exit(_scanLock);
            }
        }

        private void RunManualScan(List<Game> games)
        {
            lock (_scanLock)
            {
                var scanner = new MediaScanner(PlayniteApi, Settings);
                var issues = scanner.ScanGames(games);
                var scannedGameIds = games.Select(g => g.Id).ToHashSet();
                ApplyTags(issues, scannedGameIds);

                if (issues.Count == 0)
                {
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "MediaTools_ManualScan",
                        "Media Tools: No issues found.",
                        NotificationType.Info));
                }
                else
                {
                    var summary = string.Join(", ",
                        issues.GroupBy(i => i.MediaType)
                              .Select(g => $"{g.Count()} {g.Key}"));

                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        "MediaTools_ManualScan",
                        $"Media Tools: {issues.Count} issues found ({summary}). Check game tags for details.",
                        NotificationType.Info));
                }
            }
        }

        private void ApplyTags(List<MediaIssue> issues, HashSet<Guid> scannedGameIds)
        {
            if (!Settings.TagUndesiredMedia)
                return;

            try
            {
                var tagNames = new Dictionary<MediaType, string>
                {
                    { MediaType.Icon, Settings.IconTagName },
                    { MediaType.Cover, Settings.CoverTagName },
                    { MediaType.Background, Settings.BackgroundTagName },
                    { MediaType.Logo, Settings.LogoTagName },
                    { MediaType.Trailer, Settings.TrailerTagName },
                    { MediaType.Microtrailer, Settings.MicrotrailerTagName },
                    { MediaType.GameMusic, Settings.GameMusicTagName }
                };

                var tagMap = new Dictionary<MediaType, Tag>();
                foreach (var kvp in tagNames)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Value))
                        continue;
                    var tag = PlayniteApi.Database.Tags
                        ?.FirstOrDefault(t => t.Name == kvp.Value);
                    if (tag == null)
                    {
                        tag = new Tag(kvp.Value);
                        PlayniteApi.Database.Tags.Add(tag);
                    }
                    tagMap[kvp.Key] = tag;
                }

                // Group issues by game + media type
                var issuesByGame = issues
                    .GroupBy(i => i.GameId)
                    .ToDictionary(g => g.Key, g => g.Select(i => i.MediaType).ToHashSet());

                using (PlayniteApi.Database.BufferedUpdate())
                {
                    foreach (var gameId in scannedGameIds)
                    {
                        var game = PlayniteApi.Database.Games.Get(gameId);
                        if (game == null) continue;

                        bool changed = false;
                        issuesByGame.TryGetValue(gameId, out var gameIssueTypes);

                        foreach (var kvp in tagMap)
                        {
                            var mediaType = kvp.Key;
                            var tag = kvp.Value;
                            bool hasTag = game.TagIds?.Contains(tag.Id) == true;
                            bool shouldTag = gameIssueTypes?.Contains(mediaType) == true;

                            if (shouldTag && !hasTag)
                            {
                                if (game.TagIds == null)
                                    game.TagIds = new List<Guid>();
                                game.TagIds.Add(tag.Id);
                                changed = true;
                            }
                            else if (!shouldTag && hasTag)
                            {
                                game.TagIds.Remove(tag.Id);
                                changed = true;
                            }
                        }

                        if (changed)
                            PlayniteApi.Database.Games.Update(game);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to apply media tags.");
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new MediaToolsSettingsView();
        }
    }
}

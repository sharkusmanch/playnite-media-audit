using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MediaAudit
{
    public class MediaAuditPlugin : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("2e6b5e8a-c42d-4e5b-9f1a-3d7c8e4a6b2f");

        private MediaAuditSettings Settings { get; set; }
        // System.Threading.Timer rejects intervals beyond UInt32.MaxValue milliseconds.
        // Settings validation only enforces a lower bound, so cap here rather than let
        // a mistyped interval throw out of the settings dialog and again at startup.
        private const int MaxScanIntervalMinutes = 44640; // 31 days

        private Timer _scanTimer;
        private Timer _debounceTimer;
        private readonly object _scanLock = new object();
        private readonly object _timerLock = new object();
        private bool _scheduledEnabled;
        private int _scheduledIntervalMinutes;
        private volatile bool _stopped;
        private readonly HashSet<Guid> _pendingGameIds = new HashSet<Guid>();
        private readonly object _pendingLock = new object();

        public MediaAuditPlugin(IPlayniteAPI api) : base(api)
        {
            Settings = new MediaAuditSettings(this);
            Properties = new GenericPluginProperties { HasSettings = true };
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            ApplyScanSchedule();
            PlayniteApi.Database.Games.ItemUpdated += OnGamesUpdated;
        }

        // Rebuilds the background scan timer from current settings. Called at startup
        // and whenever settings are saved, so toggling the scan off actually stops it.
        internal void ApplyScanSchedule()
        {
            lock (_timerLock)
            {
                if (_stopped)
                    return;

                var minutes = Settings.ScanIntervalMinutes;
                if (minutes > MaxScanIntervalMinutes)
                {
                    logger.Warn($"Scan interval {minutes} exceeds the {MaxScanIntervalMinutes} minute maximum; capping.");
                    minutes = MaxScanIntervalMinutes;
                }

                var enabled = Settings.BackgroundScanEnabled && minutes >= 1;

                // Playnite raises EndEdit on OK whether or not anything changed. Rebuilding
                // unconditionally would push the next scan a minute out every time the
                // settings dialog is opened and closed.
                if (enabled == _scheduledEnabled && minutes == _scheduledIntervalMinutes)
                    return;

                _scanTimer?.Dispose();
                _scanTimer = null;
                _scheduledEnabled = enabled;
                _scheduledIntervalMinutes = minutes;

                if (!enabled)
                    return;

                _scanTimer = new Timer(_ => RunBackgroundScan(), null,
                    TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(minutes));
            }
        }

        private void OnGamesUpdated(object sender, ItemUpdatedEventArgs<Game> args)
        {
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

            lock (_timerLock)
            {
                if (_stopped)
                    return;

                _debounceTimer?.Dispose();
                _debounceTimer = new Timer(_ => FlushPendingGames(), null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
            }
        }

        private void FlushPendingGames()
        {
            if (_stopped)
                return;

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
                if (_stopped)
                    return;

                var scanner = new MediaScanner(PlayniteApi, Settings);
                var result = scanner.ScanGames(games);
                var scannedGameIds = games.Select(g => g.Id).ToHashSet();
                ApplyTags(result, scannedGameIds);
                logger.Info($"Media scan (item update): {games.Count} games checked, {result.Issues.Count} issues.");
            }
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            ShutDown();
        }

        public override void Dispose()
        {
            ShutDown();
            base.Dispose();
        }

        // Stops the timers and blocks any scan that hasn't started yet. A scan already
        // running is left to finish: Timer.Dispose() doesn't wait for an in-flight
        // callback, and the _stopped checks keep later stages from touching a database
        // that is being torn down.
        private void ShutDown()
        {
            if (_stopped)
                return;

            _stopped = true;
            PlayniteApi.Database.Games.ItemUpdated -= OnGamesUpdated;

            lock (_timerLock)
            {
                _scanTimer?.Dispose();
                _scanTimer = null;
                _debounceTimer?.Dispose();
                _debounceTimer = null;
            }
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            var menuSection = "@" + ResourceProvider.GetString("LOC_MediaAudit_MenuSection");

            yield return new MainMenuItem
            {
                Description = ResourceProvider.GetString("LOC_MediaAudit_Menu_ScanAll"),
                MenuSection = menuSection,
                Action = _ => Task.Run(() => RunManualScan(PlayniteApi.Database.Games.ToList()))
            };
            yield return new MainMenuItem
            {
                Description = ResourceProvider.GetString("LOC_MediaAudit_Menu_ScanSelected"),
                MenuSection = menuSection,
                Action = _ =>
                {
                    var selected = PlayniteApi.MainView.SelectedGames?.ToList();
                    if (selected == null || selected.Count == 0)
                    {
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "MediaAudit_NoSelection",
                            ResourceProvider.GetString("LOC_MediaAudit_Notification_NoSelection"),
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
                if (_stopped)
                    return;

                logger.Info("Starting background media scan...");
                var scanner = new MediaScanner(PlayniteApi, Settings);

                // One snapshot for both, so a game added mid-scan can't land in
                // scannedGameIds unscanned and get read as having nothing wrong with it.
                var games = PlayniteApi.Database.Games.ToList();
                var result = scanner.ScanGames(games);

                var scannedGameIds = games.Select(g => g.Id).ToHashSet();
                ApplyTags(result, scannedGameIds);

                if (result.Issues.Count > 0 && Settings.ShowScanNotification)
                {
                    var summary = string.Join(", ",
                        result.Issues.GroupBy(i => i.MediaType)
                              .Select(g => $"{g.Count()} {g.Key}"));

                    Application.Current.Dispatcher.Invoke(() =>
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "MediaAudit_BackgroundScan",
                            string.Format(ResourceProvider.GetString("LOC_MediaAudit_Notification_IssuesFound"),
                                result.Issues.Count, summary),
                            NotificationType.Info)));
                }

                logger.Info($"Background media scan complete. {result.Issues.Count} issues found.");
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
            try
            {
                RunManualScanCore(games);
            }
            catch (Exception ex)
            {
                // Runs on a Task.Run thread, where an escaping exception is unobserved.
                logger.Error(ex, "Error during manual media scan.");
            }
        }

        private void RunManualScanCore(List<Game> games)
        {
            lock (_scanLock)
            {
                if (_stopped)
                    return;

                var scanner = new MediaScanner(PlayniteApi, Settings);
                var result = scanner.ScanGames(games);
                var scannedGameIds = games.Select(g => g.Id).ToHashSet();
                ApplyTags(result, scannedGameIds);

                if (result.Issues.Count == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "MediaAudit_ManualScan",
                            ResourceProvider.GetString("LOC_MediaAudit_Notification_NoIssues"),
                            NotificationType.Info)));
                }
                else
                {
                    var summary = string.Join(", ",
                        result.Issues.GroupBy(i => i.MediaType)
                              .Select(g => $"{g.Count()} {g.Key}"));

                    Application.Current.Dispatcher.Invoke(() =>
                        PlayniteApi.Notifications.Add(new NotificationMessage(
                            "MediaAudit_ManualScan",
                            string.Format(ResourceProvider.GetString("LOC_MediaAudit_Notification_IssuesFoundDetail"),
                                result.Issues.Count, summary),
                            NotificationType.Info)));
                }
            }
        }

        // Resolves the tag this plugin owns for each enabled media type, keyed by the
        // GUID stored in settings. A type is omitted when it has no tag yet and no game
        // needs one, so disabled or clean checks don't litter the tag list.
        private Dictionary<MediaType, Tag> ResolveTags(HashSet<MediaType> typesWithIssues)
        {
            // Work from a snapshot so TagIdsLock is never held across a Playnite database
            // call. Tags.Add/Update raise events Playnite's own UI subscribes to; holding
            // the lock through them would deadlock against the UI thread waiting on it in
            // BeginEdit or CancelEdit.
            Dictionary<MediaType, Guid> storedIds;
            lock (Settings.TagIdsLock)
            {
                storedIds = new Dictionary<MediaType, Guid>(Settings.TagIds);
            }

            var resolvedIds = new Dictionary<MediaType, Guid>();
            var tagMap = ResolveTagsCore(typesWithIssues, storedIds, resolvedIds);

            bool storedIdsChanged = false;
            lock (Settings.TagIdsLock)
            {
                foreach (var kvp in resolvedIds)
                {
                    if (!Settings.TagIds.TryGetValue(kvp.Key, out var knownId) || knownId != kvp.Value)
                    {
                        Settings.TagIds[kvp.Key] = kvp.Value;
                        storedIdsChanged = true;
                    }
                }

                // Never persist mid-edit: SavePluginSettings writes the whole live settings
                // object, which the open dialog is binding into on every keystroke, so this
                // would commit changes the user can still cancel. The GUIDs stay in memory
                // and are written by EndEdit, or by the next scan once the dialog is closed.
                if (storedIdsChanged && !Settings.IsEditing)
                    SavePluginSettings(Settings);
            }

            return tagMap;
        }

        private Dictionary<MediaType, Tag> ResolveTagsCore(
            HashSet<MediaType> typesWithIssues,
            Dictionary<MediaType, Guid> storedIds,
            Dictionary<MediaType, Guid> resolvedIds)
        {
            var tagMap = new Dictionary<MediaType, Tag>();

            foreach (var mediaType in Settings.EnabledMediaTypes())
            {
                var name = Settings.TagNameFor(mediaType);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                Tag tag = null;
                if (storedIds.TryGetValue(mediaType, out var storedId) && storedId != Guid.Empty)
                {
                    tag = PlayniteApi.Database.Tags.Get(storedId);
                    if (tag != null && tag.Name != name)
                    {
                        // Renaming in settings renames the tag we already own, rather
                        // than abandoning it and starting a second one.
                        tag.Name = name;
                        PlayniteApi.Database.Tags.Update(tag);
                    }
                }

                if (tag == null)
                {
                    // Nothing on record. Adopt a same-named tag once, so installs that
                    // predate ID ownership keep the tags already on their games. After
                    // this the ID is stored and lookups no longer go by name.
                    tag = PlayniteApi.Database.Tags.FirstOrDefault(t => t.Name == name);
                }

                if (tag == null)
                {
                    if (!typesWithIssues.Contains(mediaType))
                        continue;

                    tag = new Tag(name);
                    PlayniteApi.Database.Tags.Add(tag);
                }

                resolvedIds[mediaType] = tag.Id;
                tagMap[mediaType] = tag;
            }

            return tagMap;
        }

        private void ApplyTags(ScanResult result, HashSet<Guid> scannedGameIds)
        {
            if (!Settings.TagUndesiredMedia)
                return;

            try
            {
                var typesWithIssues = result.Issues.Select(i => i.MediaType).ToHashSet();
                var tagMap = ResolveTags(typesWithIssues);
                if (tagMap.Count == 0)
                    return;

                // Several media types can be configured with the same tag name and so
                // share one Tag. Decide per tag rather than per type, otherwise one type
                // adds the tag and the next removes it again in the same pass.
                var tagGroups = tagMap
                    .GroupBy(kvp => kvp.Value.Id)
                    .Select(g => new { Tag = g.First().Value, Types = g.Select(kvp => kvp.Key).ToList() })
                    .ToList();

                var issuesByGame = result.Issues
                    .GroupBy(i => i.GameId)
                    .ToDictionary(g => g.Key, g => g.Select(i => i.MediaType).ToHashSet());

                using (PlayniteApi.Database.BufferedUpdate())
                {
                    foreach (var gameId in scannedGameIds)
                    {
                        try
                        {
                            var game = PlayniteApi.Database.Games.Get(gameId);
                            if (game == null) continue;

                            bool changed = false;
                            issuesByGame.TryGetValue(gameId, out var gameIssueTypes);

                            foreach (var group in tagGroups)
                            {
                                var tag = group.Tag;
                                bool hasTag = game.TagIds?.Contains(tag.Id) == true;
                                bool shouldTag = gameIssueTypes != null
                                    && group.Types.Any(t => gameIssueTypes.Contains(t));
                                bool indeterminate = group.Types.Any(t => result.IsIndeterminate(gameId, t));

                                switch (TagDecision.For(hasTag, shouldTag, indeterminate))
                                {
                                    case TagAction.Add:
                                        if (game.TagIds == null)
                                            game.TagIds = new List<Guid>();
                                        game.TagIds.Add(tag.Id);
                                        changed = true;
                                        break;

                                    case TagAction.Remove:
                                        game.TagIds.Remove(tag.Id);
                                        changed = true;
                                        break;
                                }
                            }

                            if (changed)
                                PlayniteApi.Database.Games.Update(game);
                        }
                        catch (Exception ex)
                        {
                            // Per game, so one bad entry can't abort tagging for the rest.
                            logger.Error(ex, $"Failed to apply media tags for game {gameId}.");
                        }
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
            return new MediaAuditSettingsView();
        }
    }
}

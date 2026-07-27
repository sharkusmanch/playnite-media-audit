using System;
using System.Collections.Generic;

namespace MediaAudit
{
    public enum MediaType
    {
        Icon,
        Cover,
        Background,
        Logo,
        Trailer,
        Microtrailer,
        GameMusic
    }

    public enum IssueType
    {
        Missing,
        BadAspectRatio,
        LowResolution,
        HighResolution
    }

    public class MediaIssue
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public MediaType MediaType { get; set; }
        public IssueType IssueType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Description { get; set; }
    }

    internal enum TagAction
    {
        Leave,
        Add,
        Remove
    }

    internal static class TagDecision
    {
        // The whole point of the three-state scan result comes down to this: a media type
        // the scan could not evaluate keeps whatever tag it already has, because absence
        // of a finding is not a clean bill of health. Kept as a pure function so the
        // truth table can be tested without a Playnite database.
        internal static TagAction For(bool hasTag, bool shouldTag, bool indeterminate)
        {
            if (!shouldTag && indeterminate)
                return TagAction.Leave;

            if (shouldTag && !hasTag)
                return TagAction.Add;

            if (!shouldTag && hasTag)
                return TagAction.Remove;

            return TagAction.Leave;
        }
    }

    public class ScanResult
    {
        public List<MediaIssue> Issues { get; } = new List<MediaIssue>();

        // Game/media combinations the scan could not evaluate — an unreadable file, a
        // path Playnite would not resolve. Distinct from "no issue found": tags for
        // these are left as they are, so a transient failure can't strip a library.
        private readonly Dictionary<Guid, HashSet<MediaType>> _indeterminate =
            new Dictionary<Guid, HashSet<MediaType>>();

        public void MarkIndeterminate(Guid gameId, MediaType mediaType)
        {
            if (!_indeterminate.TryGetValue(gameId, out var types))
            {
                types = new HashSet<MediaType>();
                _indeterminate[gameId] = types;
            }
            types.Add(mediaType);
        }

        public bool IsIndeterminate(Guid gameId, MediaType mediaType)
        {
            return _indeterminate.TryGetValue(gameId, out var types) && types.Contains(mediaType);
        }
    }

    public class MediaStandards
    {
        public double ExpectedAspectRatio { get; set; }
        public double AspectRatioTolerance { get; set; }
        public int MinWidth { get; set; }
        public int MinHeight { get; set; }
        public int MaxWidth { get; set; }
        public int MaxHeight { get; set; }
    }
}

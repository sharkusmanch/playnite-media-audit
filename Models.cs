using System;

namespace MediaTools
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

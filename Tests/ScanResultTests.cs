using System;
using Xunit;

namespace MediaAudit.Tests
{
    public class ScanResultTests
    {
        [Fact]
        public void UnmarkedMedia_IsNotIndeterminate()
        {
            var result = new ScanResult();

            Assert.False(result.IsIndeterminate(Guid.NewGuid(), MediaType.Icon));
        }

        [Fact]
        public void MarkingIsScopedToTheGameAndMediaType()
        {
            var result = new ScanResult();
            var game = Guid.NewGuid();
            var otherGame = Guid.NewGuid();

            result.MarkIndeterminate(game, MediaType.Background);

            Assert.True(result.IsIndeterminate(game, MediaType.Background));
            Assert.False(result.IsIndeterminate(game, MediaType.Icon));
            Assert.False(result.IsIndeterminate(otherGame, MediaType.Background));
        }

        [Fact]
        public void MultipleTypesPerGameAreTrackedIndependently()
        {
            var result = new ScanResult();
            var game = Guid.NewGuid();

            result.MarkIndeterminate(game, MediaType.Icon);
            result.MarkIndeterminate(game, MediaType.Cover);

            Assert.True(result.IsIndeterminate(game, MediaType.Icon));
            Assert.True(result.IsIndeterminate(game, MediaType.Cover));
            Assert.False(result.IsIndeterminate(game, MediaType.Background));
        }

        [Fact]
        public void MarkingTwiceIsHarmless()
        {
            var result = new ScanResult();
            var game = Guid.NewGuid();

            result.MarkIndeterminate(game, MediaType.Trailer);
            result.MarkIndeterminate(game, MediaType.Trailer);

            Assert.True(result.IsIndeterminate(game, MediaType.Trailer));
        }

        [Fact]
        public void IssuesAndIndeterminateAreSeparateChannels()
        {
            var result = new ScanResult();
            var game = Guid.NewGuid();

            result.Issues.Add(new MediaIssue
            {
                GameId = game,
                MediaType = MediaType.Icon,
                IssueType = IssueType.LowResolution
            });

            Assert.Single(result.Issues);
            Assert.False(result.IsIndeterminate(game, MediaType.Icon));
        }
    }
}

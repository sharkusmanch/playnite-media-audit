using Xunit;

namespace MediaAudit.Tests
{
    // The audit's core finding was that "couldn't evaluate" was collapsed into "no issue",
    // which the tagging pass then read as conforming and used to remove tags. This is the
    // truth table that has to hold for that not to happen again.
    public class TagDecisionTests
    {
        [Theory]
        [InlineData(false, true, "adds when an issue is found and the tag is absent")]
        [InlineData(true, false, "leaves an already-tagged game alone")]
        public void IssueFound_TagIsApplied(bool hasTag, bool expectAdd, string because)
        {
            var action = TagDecision.For(hasTag, shouldTag: true, indeterminate: false);

            Assert.Equal(expectAdd ? TagAction.Add : TagAction.Leave, action);
            Assert.NotNull(because);
        }

        [Fact]
        public void NoIssue_RemovesAStaleTag()
        {
            Assert.Equal(TagAction.Remove, TagDecision.For(hasTag: true, shouldTag: false, indeterminate: false));
        }

        [Fact]
        public void NoIssueAndNoTag_DoesNothing()
        {
            Assert.Equal(TagAction.Leave, TagDecision.For(hasTag: false, shouldTag: false, indeterminate: false));
        }

        // The regression guard: an unreadable file, an unresolved path, a remote URL or an
        // uninstalled game must never cause an existing tag to be stripped.
        [Fact]
        public void Indeterminate_NeverRemovesAnExistingTag()
        {
            Assert.Equal(TagAction.Leave, TagDecision.For(hasTag: true, shouldTag: false, indeterminate: true));
        }

        [Fact]
        public void Indeterminate_DoesNotInventATag()
        {
            Assert.Equal(TagAction.Leave, TagDecision.For(hasTag: false, shouldTag: false, indeterminate: true));
        }

        // A real finding still wins over indeterminate: when two media types share a tag and
        // one of them found a genuine issue, the tag gets applied even though the other
        // could not be evaluated.
        [Fact]
        public void RealIssueOutranksIndeterminate_AppliesMissingTag()
        {
            Assert.Equal(TagAction.Add, TagDecision.For(hasTag: false, shouldTag: true, indeterminate: true));
        }

        [Fact]
        public void RealIssueOutranksIndeterminate_KeepsExistingTag()
        {
            Assert.Equal(TagAction.Leave, TagDecision.For(hasTag: true, shouldTag: true, indeterminate: true));
        }

        [Fact]
        public void RemovalOnlyEverHappensOnAConfirmedCleanResult()
        {
            // Exhaustive: Remove must be unreachable unless the scan positively determined
            // there was nothing wrong.
            foreach (var hasTag in new[] { true, false })
            {
                foreach (var shouldTag in new[] { true, false })
                {
                    foreach (var indeterminate in new[] { true, false })
                    {
                        if (TagDecision.For(hasTag, shouldTag, indeterminate) == TagAction.Remove)
                        {
                            Assert.True(hasTag);
                            Assert.False(shouldTag);
                            Assert.False(indeterminate);
                        }
                    }
                }
            }
        }
    }
}

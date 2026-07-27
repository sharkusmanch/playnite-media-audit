using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MediaAudit.Tests
{
    public class SettingsTests
    {
        // Four of the seven checks default to false. Building tags for all seven anyway is
        // what created unused tags and stripped same-named user tags from every game.
        [Fact]
        public void DefaultSettings_EnableOnlyTheThreeCoreImageChecks()
        {
            var settings = new MediaAuditSettings();

            Assert.Equal(
                new[] { MediaType.Icon, MediaType.Cover, MediaType.Background },
                settings.EnabledMediaTypes().ToArray());
        }

        [Fact]
        public void DisablingACheck_RemovesItFromTheEnabledSet()
        {
            var settings = new MediaAuditSettings { CheckCovers = false };

            Assert.DoesNotContain(MediaType.Cover, settings.EnabledMediaTypes());
            Assert.Contains(MediaType.Icon, settings.EnabledMediaTypes());
        }

        [Fact]
        public void EnablingExtraChecks_AddsThemToTheEnabledSet()
        {
            var settings = new MediaAuditSettings
            {
                CheckTrailers = true,
                CheckGameMusic = true
            };

            var enabled = settings.EnabledMediaTypes().ToList();

            Assert.Contains(MediaType.Trailer, enabled);
            Assert.Contains(MediaType.GameMusic, enabled);
            Assert.DoesNotContain(MediaType.Logo, enabled);
            Assert.DoesNotContain(MediaType.Microtrailer, enabled);
        }

        [Fact]
        public void EveryMediaTypeMapsToATagName()
        {
            var settings = new MediaAuditSettings();

            foreach (MediaType mediaType in Enum.GetValues(typeof(MediaType)))
            {
                Assert.False(string.IsNullOrWhiteSpace(settings.TagNameFor(mediaType)));
            }
        }

        [Fact]
        public void TagNamesAreDistinctByDefault()
        {
            var settings = new MediaAuditSettings();

            var names = Enum.GetValues(typeof(MediaType))
                .Cast<MediaType>()
                .Select(settings.TagNameFor)
                .ToList();

            Assert.Equal(names.Count, names.Distinct().Count());
        }

        // Settings files written before tag ownership existed have no TagIds field, so the
        // property has to survive being deserialized as null.
        [Fact]
        public void TagIds_IsNeverNull()
        {
            var settings = new MediaAuditSettings();
            Assert.NotNull(settings.TagIds);

            settings.TagIds = null;
            Assert.NotNull(settings.TagIds);
            Assert.Empty(settings.TagIds);
        }

        [Fact]
        public void TagIds_RoundTripsAssignedValues()
        {
            var id = Guid.NewGuid();
            var settings = new MediaAuditSettings();

            settings.TagIds = new Dictionary<MediaType, Guid> { { MediaType.Icon, id } };

            Assert.Equal(id, settings.TagIds[MediaType.Icon]);
        }
    }
}

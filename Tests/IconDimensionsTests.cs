using System;
using System.Collections.Generic;
using Xunit;

namespace MediaAudit.Tests
{
    // Covers the bug this plugin was graded down for: GDI+ reports an .ico at its
    // smallest frame, so a well-formed 64x64 icon read as 16x16 and got tagged
    // "Undesired Icon" against a default IconMinSize of 64.
    public class IconDimensionsTests
    {
        // Builds a well-formed icon: real image offsets past the directory table, and
        // payload actually present, so the truncation guard is satisfied.
        private static byte[] BuildIcon(params int[] frameSizes)
        {
            const int payloadPerFrame = 64;
            int directoryEnd = 6 + (frameSizes.Length * 16);
            var bytes = new byte[directoryEnd + (frameSizes.Length * payloadPerFrame)];

            bytes[0] = 0; bytes[1] = 0;   // reserved
            bytes[2] = 1; bytes[3] = 0;   // type 1 = icon
            BitConverter.GetBytes((ushort)frameSizes.Length).CopyTo(bytes, 4);

            for (int i = 0; i < frameSizes.Length; i++)
            {
                int entry = 6 + (i * 16);
                // 256 is encoded as 0 in this single byte, which is what % 256 produces.
                bytes[entry] = (byte)(frameSizes[i] % 256);
                bytes[entry + 1] = (byte)(frameSizes[i] % 256);
                BitConverter.GetBytes((uint)payloadPerFrame).CopyTo(bytes, entry + 8);
                BitConverter.GetBytes((uint)(directoryEnd + (i * payloadPerFrame))).CopyTo(bytes, entry + 12);
            }

            return bytes;
        }

        [Fact]
        public void TakesLargestFrame_NotTheSmallest()
        {
            Assert.True(MediaScanner.TryGetIconDimensions(BuildIcon(16, 32, 64), out var width, out var height));
            Assert.Equal(64, width);
            Assert.Equal(64, height);
        }

        [Fact]
        public void TakesLargestFrame_RegardlessOfOrder()
        {
            Assert.True(MediaScanner.TryGetIconDimensions(BuildIcon(64, 48, 16), out var width, out var height));
            Assert.Equal(64, width);
            Assert.Equal(64, height);
        }

        // The real OneDrive.ico layout that reproduced the original bug: eight frames,
        // largest first, which GDI+ still reported as 16x16.
        [Fact]
        public void EightFrameIcon_ReadsAsLargest()
        {
            Assert.True(MediaScanner.TryGetIconDimensions(
                BuildIcon(64, 48, 40, 32, 28, 24, 20, 16), out var width, out var height));
            Assert.Equal(64, width);
            Assert.Equal(64, height);
        }

        [Theory]
        [InlineData(256)]
        [InlineData(128)]
        [InlineData(16)]
        public void SingleFrame_RoundTrips(int size)
        {
            Assert.True(MediaScanner.TryGetIconDimensions(BuildIcon(size), out var width, out var height));
            Assert.Equal(size, width);
            Assert.Equal(size, height);
        }

        // A zero byte in the dimension field means 256, not 0.
        [Fact]
        public void ZeroByteDimension_MeansTwoFiftySix()
        {
            Assert.True(MediaScanner.TryGetIconDimensions(BuildIcon(16, 256, 32), out var width, out var height));
            Assert.Equal(256, width);
            Assert.Equal(256, height);
        }

        [Fact]
        public void TruncatedPixelData_IsRejected()
        {
            var icon = BuildIcon(64);
            Array.Resize(ref icon, icon.Length / 2);

            // Rejected rather than trusted, so the caller falls back to GDI+ and ends up
            // marking the media indeterminate instead of inventing a confident size.
            Assert.False(MediaScanner.TryGetIconDimensions(icon, out _, out _));
        }

        [Fact]
        public void DirectoryIntactButPixelDataMissing_IsRejected()
        {
            var icon = BuildIcon(64, 32);
            Array.Resize(ref icon, 6 + (2 * 16));

            Assert.False(MediaScanner.TryGetIconDimensions(icon, out _, out _));
        }

        // Image data may not overlap the directory table it is described by.
        [Fact]
        public void ImageOffsetInsideDirectory_IsRejected()
        {
            var icon = BuildIcon(64);
            BitConverter.GetBytes((uint)10).CopyTo(icon, 6 + 12);

            Assert.False(MediaScanner.TryGetIconDimensions(icon, out _, out _));
        }

        [Fact]
        public void FrameCountLargerThanFilePermits_IsRejected()
        {
            var icon = BuildIcon(64);
            BitConverter.GetBytes((ushort)500).CopyTo(icon, 4);

            Assert.False(MediaScanner.TryGetIconDimensions(icon, out _, out _));
        }

        public static IEnumerable<object[]> NonIconHeaders()
        {
            yield return new object[] { "png", new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13 } };
            yield return new object[] { "jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0, 16, 0x4A, 0x46, 0x49, 0x46, 0, 1 } };
            yield return new object[] { "gif", new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 1, 0, 1, 0, 0, 0 } };
            yield return new object[] { "bmp", new byte[] { 0x42, 0x4D, 0x36, 0, 0, 0, 0, 0, 0, 0, 0x36, 0 } };
            yield return new object[] { "webp", new byte[] { 0x52, 0x49, 0x46, 0x46, 0xA8, 0x6F, 4, 0, 0x57, 0x45, 0x42, 0x50 } };
            // Cursors share the icon layout but use type 2, and must not be treated as icons.
            yield return new object[] { "cursor", new byte[] { 0, 0, 2, 0, 1, 0, 0, 0, 0, 0, 0, 0 } };
        }

        [Theory]
        [MemberData(nameof(NonIconHeaders))]
        public void NonIconFormats_FallBackToGdiPlus(string format, byte[] header)
        {
            Assert.False(MediaScanner.TryGetIconDimensions(header, out var width, out var height));
            Assert.Equal(0, width);
            Assert.Equal(0, height);
            Assert.NotNull(format);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(6)]
        public void ShortInput_IsRejectedWithoutReadingPastTheEnd(int length)
        {
            var bytes = new byte[length];
            if (length > 3)
            {
                bytes[2] = 1;
            }

            Assert.False(MediaScanner.TryGetIconDimensions(bytes, out _, out _));
        }

        [Fact]
        public void ZeroFrames_IsRejected()
        {
            var bytes = new byte[] { 0, 0, 1, 0, 0, 0 };

            Assert.False(MediaScanner.TryGetIconDimensions(bytes, out _, out _));
        }
    }
}

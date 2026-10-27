using System.Buffers.Binary;
using System.Text;
using TranslyAI.Api.Common;

namespace TranslyAI.Api.Tests.Common;

public class WavInspectorTests
{
    [Fact]
    public void TryRead_ValidPcm16Mono_ReturnsFormatAndDuration()
    {
        var wav = BuildWav(sampleRate: 16_000, channels: 1, bitsPerSample: 16, seconds: 2.5);

        Assert.True(WavInspector.TryRead(wav, out var info));
        Assert.NotNull(info);
        Assert.Equal(16_000, info.SampleRate);
        Assert.Equal(1, info.Channels);
        Assert.Equal(16, info.BitsPerSample);
        Assert.Equal(2.5, info.DurationSeconds, precision: 3);
    }

    [Fact]
    public void TryRead_ExtraChunkWithOddSize_IsSkipped()
    {
        // LIST chunk حجمه 3 (فردي) + byte padding
        var listChunk = Encoding.ASCII.GetBytes("LIST")
            .Concat(new byte[] { 3, 0, 0, 0, 1, 2, 3, 0 })
            .ToArray();

        var wav = BuildWav(seconds: 1, extraChunk: listChunk);

        Assert.True(WavInspector.TryRead(wav, out var info));
        Assert.NotNull(info);
        Assert.Equal(1.0, info.DurationSeconds, precision: 3);
    }

    [Fact]
    public void TryRead_DataSizePlaceholder_FallsBackToActualBytes()
    {
        var wav = BuildWav(seconds: 1.5, dataSizeOverride: 0xFFFFFFFF);

        Assert.True(WavInspector.TryRead(wav, out var info));
        Assert.NotNull(info);
        Assert.Equal(1.5, info.DurationSeconds, precision: 3);
    }

    [Fact]
    public void TryRead_NotRiff_ReturnsFalse()
    {
        var mp3Like = Encoding.ASCII.GetBytes("ID3\u0004 this is not a wav file at all");

        Assert.False(WavInspector.TryRead(mp3Like, out _));
    }

    [Fact]
    public void TryRead_TooShort_ReturnsFalse()
    {
        Assert.False(WavInspector.TryRead(new byte[] { 0x52, 0x49 }, out _));
    }

    [Fact]
    public void TryRead_FloatFormat_ReturnsFalse()
    {
        var wav = BuildWav(audioFormat: 3, bitsPerSample: 32);

        Assert.False(WavInspector.TryRead(wav, out _));
    }

    [Fact]
    public void TryRead_MissingDataChunk_ReturnsFalse()
    {
        var wav = BuildWav(includeData: false);

        Assert.False(WavInspector.TryRead(wav, out _));
    }

    private static byte[] BuildWav(
        int sampleRate = 16_000,
        int channels = 1,
        int bitsPerSample = 16,
        double seconds = 1,
        ushort audioFormat = 1,
        byte[]? extraChunk = null,
        uint? dataSizeOverride = null,
        bool includeData = true)
    {
        var blockAlign = channels * bitsPerSample / 8;
        var byteRate = sampleRate * blockAlign;
        var dataSize = (int)(byteRate * seconds);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream); // little-endian by default

        writer.Write("RIFF"u8);
        writer.Write(0u); // بيتعدّل في الآخر
        writer.Write("WAVE"u8);

        writer.Write("fmt "u8);
        writer.Write(16u);
        writer.Write(audioFormat);
        writer.Write((ushort)channels);
        writer.Write((uint)sampleRate);
        writer.Write((uint)byteRate);
        writer.Write((ushort)blockAlign);
        writer.Write((ushort)bitsPerSample);

        if (extraChunk is not null)
        {
            writer.Write(extraChunk);
        }

        if (includeData)
        {
            writer.Write("data"u8);
            writer.Write(dataSizeOverride ?? (uint)dataSize);
            writer.Write(new byte[dataSize]);
        }

        writer.Flush();

        var bytes = stream.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), (uint)(bytes.Length - 8));
        return bytes;
    }
}
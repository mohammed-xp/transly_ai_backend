using System.Buffers.Binary;

namespace TranslyAI.Api.Tests.Fakes;

public static class TestWav
{
    // PCM 16-bit mono — نفس الـ format اللي Flutter هيسجّل بيه
    public static byte[] Create(double seconds, int sampleRate = 16_000)
    {
        const int channels = 1;
        const int bitsPerSample = 16;

        var blockAlign = channels * bitsPerSample / 8;
        var byteRate = sampleRate * blockAlign;
        var dataSize = (int)(byteRate * seconds);
        dataSize -= dataSize % blockAlign;

        var wav = new byte[44 + dataSize];
        var span = wav.AsSpan();

        "RIFF"u8.CopyTo(span);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], (uint)(36 + dataSize));
        "WAVE"u8.CopyTo(span[8..]);

        "fmt "u8.CopyTo(span[12..]);
        BinaryPrimitives.WriteUInt32LittleEndian(span[16..], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(span[20..], 1); // PCM
        BinaryPrimitives.WriteUInt16LittleEndian(span[22..], channels);
        BinaryPrimitives.WriteUInt32LittleEndian(span[24..], (uint)sampleRate);
        BinaryPrimitives.WriteUInt32LittleEndian(span[28..], (uint)byteRate);
        BinaryPrimitives.WriteUInt16LittleEndian(span[32..], (ushort)blockAlign);
        BinaryPrimitives.WriteUInt16LittleEndian(span[34..], bitsPerSample);

        "data"u8.CopyTo(span[36..]);
        BinaryPrimitives.WriteUInt32LittleEndian(span[40..], (uint)dataSize);

        return wav;
    }
}
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace TranslyAI.Api.Common;

public sealed record WavInfo(int SampleRate, int Channels, int BitsPerSample, double DurationSeconds);

public static class WavInspector
{
    private const ushort PcmFormat = 1;
    private const ushort ExtensibleFormat = 0xFFFE;

    public static bool TryRead(ReadOnlySpan<byte> wav, [NotNullWhen(true)] out WavInfo? info)
    {
        info = null;

        // الـ header: "RIFF" + حجم (4 bytes) + "WAVE"
        if (wav.Length < 12
            || !wav[..4].SequenceEqual("RIFF"u8)
            || !wav.Slice(8, 4).SequenceEqual("WAVE"u8))
        {
            return false;
        }

        var hasFormat = false;
        ushort audioFormat = 0;
        int channels = 0, sampleRate = 0, bitsPerSample = 0;

        var offset = 12;

        while (offset + 8 <= wav.Length)
        {
            var chunkId = wav.Slice(offset, 4);
            var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(wav.Slice(offset + 4, 4));
            var bodyStart = offset + 8;
            var available = wav.Length - bodyStart;

            if (chunkId.SequenceEqual("fmt "u8))
            {
                if (chunkSize < 16 || available < 16) return false;

                var fmt = wav.Slice(bodyStart, 16);
                audioFormat = BinaryPrimitives.ReadUInt16LittleEndian(fmt[..2]);
                channels = BinaryPrimitives.ReadUInt16LittleEndian(fmt.Slice(2, 2));
                sampleRate = BinaryPrimitives.ReadInt32LittleEndian(fmt.Slice(4, 4));
                bitsPerSample = BinaryPrimitives.ReadUInt16LittleEndian(fmt.Slice(14, 2));
                hasFormat = true;
            }
            else if (chunkId.SequenceEqual("data"u8))
            {
                if (!hasFormat || !IsSupportedFormat(audioFormat, channels, sampleRate, bitsPerSample))
                {
                    return false;
                }

                // بعض الـ recorders بتسيب الحجم 0 أو 0xFFFFFFFF لو الملف ما اتقفلش صح،
                // ساعتها بنعتمد على الـ bytes الموجودة فعلاً
                long dataSize = chunkSize == 0 || chunkSize > available ? (long)available : chunkSize;

                // بنحسب الـ bytes في الثانية بنفسنا بدل ما نصدّق الرقم المكتوب في الـ header
                long bytesPerSecond = (long)sampleRate * channels * (bitsPerSample / 8);

                info = new WavInfo(sampleRate, channels, bitsPerSample, (double)dataSize / bytesPerSecond);
                return true;
            }

            // الـ chunks لازم تبدأ على عنوان زوجي: لو الحجم فردي فيه byte زيادة (padding)
            long next = (long)bodyStart + chunkSize + (chunkSize % 2);
            if (next > wav.Length) return false;

            offset = (int)next;
        }

        // خلّصنا الملف ومالقيناش chunk الـ data
        return false;
    }

    private static bool IsSupportedFormat(ushort audioFormat, int channels, int sampleRate, int bitsPerSample) =>
        audioFormat is PcmFormat or ExtensibleFormat
        && channels is 1 or 2
        && sampleRate is >= 8_000 and <= 48_000
        && bitsPerSample is 8 or 16 or 24 or 32;
}
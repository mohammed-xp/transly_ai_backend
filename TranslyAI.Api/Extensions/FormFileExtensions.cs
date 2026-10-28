namespace TranslyAI.Api.Extensions;

public static class FormFileExtensions
{
    public static async Task<byte[]> ReadAllBytesAsync(this IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream(capacity: (int)file.Length);

        await stream.CopyToAsync(memory, cancellationToken);

        return memory.ToArray();
    }
}
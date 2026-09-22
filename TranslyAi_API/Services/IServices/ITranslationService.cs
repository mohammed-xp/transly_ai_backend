using TranslyAi_API.Dtos;

namespace TranslyAi_API.Services.IServices
{
    public interface ITranslationService
    {
        Task<TranslateResponseDto> TranslateAsync(TranslateRequestDto translateRequestDto, CancellationToken cancellationToken);
    }
}

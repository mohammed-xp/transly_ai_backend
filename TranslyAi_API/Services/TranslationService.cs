using TranslyAi_API.Dtos;
using TranslyAi_API.Services.IServices;

namespace TranslyAi_API.Services
{
    public class TranslationService : ITranslationService
    {
        public Task<TranslateResponseDto> TranslateAsync(TranslateRequestDto translateRequestDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

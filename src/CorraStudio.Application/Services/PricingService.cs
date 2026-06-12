using CorraStudio.Domain.Entities;
using CorraStudio.Domain.Interfaces.Services;
using CorraStudio.Domain.ValueObjects;

namespace CorraStudio.Application.Services;

public class PricingService : IPricingService
{
    private readonly IConfigurationRepository _configurationRepository;

    public PricingService(IConfigurationRepository configurationRepository)
    {
        _configurationRepository = configurationRepository;
    }

    public async Task<Money> CalculatePriceAsync(Session session)
    {
        return await CalculatePriceAsync(session.TenantId ?? Guid.Empty, session.PhotoCount, false);
    }

    public async Task<Money> CalculatePriceAsync(Guid tenantId, int photoCount, bool isGifRequested = false)
    {
        var basePrice = await _configurationRepository.GetValueAsync(tenantId, "PhotoBasePrice", 10000m);
        var pricePerPhoto = await _configurationRepository.GetValueAsync(tenantId, "PricePerPhoto", 5000m);
        var gifFee = isGifRequested ? await _configurationRepository.GetValueAsync(tenantId, "GifFee", 15000m) : 0m;
        
        var total = basePrice + (pricePerPhoto * photoCount) + gifFee;
        return new Money(total);
    }

    public async Task<Money> GetPackagePriceAsync(Guid tenantId, string packageCode)
    {
        var packagePrice = await _configurationRepository.GetValueAsync(tenantId, $"Package_{packageCode}_Price", 0m);
        
        if (packagePrice <= 0)
            throw new DomainException($"Package {packageCode} not found");
        
        return new Money(packagePrice);
    }
}

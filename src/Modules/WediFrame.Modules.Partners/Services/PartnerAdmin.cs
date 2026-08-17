using Microsoft.EntityFrameworkCore;
using WediFrame.Modules.Partners.Domain;
using WediFrame.Shared.Admin;

namespace WediFrame.Modules.Partners.Services;

/// <summary>
/// Partners-side implementation of <see cref="IPartnerAdmin"/>. Keeps all Partner /
/// BonusCode reads and writes inside the owning module; the Admin module only holds
/// the Shared contract.
/// </summary>
public sealed class PartnerAdmin(DbContext db, TimeProvider timeProvider) : IPartnerAdmin
{
    private const int MaxPageSize = 200;

    public async Task<PartnerPage> ListPartnersAsync(string? search, int page, int pageSize, CancellationToken ct)
    {
        var p = page > 0 ? page : 1;
        var size = pageSize is > 0 and <= MaxPageSize ? pageSize : 50;

        var q = db.Set<Partner>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.Name, pattern));
        }

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((p - 1) * size)
            .Take(size)
            .Select(x => new
            {
                x.Id, x.Name, x.Type, x.ContactEmail, x.ContactPhone, x.CreatedAt,
                CodeCount = db.Set<BonusCode>().Count(c => c.PartnerId == x.Id),
            })
            .ToListAsync(ct);

        var items = rows
            .Select(x => new PartnerRecord(
                x.Id, x.Name, x.Type.ToString(), x.ContactEmail, x.ContactPhone, x.CodeCount, x.CreatedAt))
            .ToList();

        return new PartnerPage(items, total);
    }

    public async Task<PartnerRecord> CreatePartnerAsync(PartnerInput input, CancellationToken ct)
    {
        var type = Enum.TryParse<PartnerType>(input.Type, ignoreCase: true, out var t) ? t : PartnerType.Other;

        var partner = new Partner
        {
            Id = Guid.NewGuid(),
            Name = input.Name.Trim(),
            Type = type,
            ContactEmail = Trim(input.ContactEmail),
            ContactPhone = Trim(input.ContactPhone),
            Notes = Trim(input.Notes),
            CreatedAt = timeProvider.GetUtcNow(),
        };

        db.Set<Partner>().Add(partner);
        await db.SaveChangesAsync(ct);

        return new PartnerRecord(
            partner.Id, partner.Name, partner.Type.ToString(),
            partner.ContactEmail, partner.ContactPhone, 0, partner.CreatedAt);
    }

    public async Task<PartnerDetail?> GetPartnerAsync(Guid partnerId, CancellationToken ct)
    {
        var partner = await db.Set<Partner>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == partnerId, ct);
        if (partner is null)
        {
            return null;
        }

        var codes = await LoadCodesAsync(partnerId, ct);

        return new PartnerDetail(
            partner.Id, partner.Name, partner.Type.ToString(),
            partner.ContactEmail, partner.ContactPhone, partner.Notes, partner.CreatedAt, codes);
    }

    public async Task<CreateCodeResult> CreateCodeAsync(Guid partnerId, BonusCodeInput input, CancellationToken ct)
    {
        var partnerExists = await db.Set<Partner>().AnyAsync(x => x.Id == partnerId, ct);
        if (!partnerExists)
        {
            return new CreateCodeResult(CreateCodeOutcome.PartnerNotFound);
        }

        var code = (input.Code ?? "").Trim().ToUpperInvariant();
        if (code.Length is 0 or > 64)
        {
            return new CreateCodeResult(CreateCodeOutcome.Invalid);
        }

        if (!Enum.TryParse<DiscountType>(input.DiscountType, ignoreCase: true, out var discountType))
        {
            return new CreateCodeResult(CreateCodeOutcome.Invalid);
        }

        // Percentage must be 1–100; fixed amount must be a positive cent value.
        if (input.DiscountValue <= 0
            || (discountType == DiscountType.Percentage && input.DiscountValue > 100)
            || (input.MaxRedemptions is <= 0))
        {
            return new CreateCodeResult(CreateCodeOutcome.Invalid);
        }

        if (await db.Set<BonusCode>().AnyAsync(c => c.Code == code, ct))
        {
            return new CreateCodeResult(CreateCodeOutcome.DuplicateCode);
        }

        var entity = new BonusCode
        {
            Id = Guid.NewGuid(),
            PartnerId = partnerId,
            Code = code,
            DiscountType = discountType,
            DiscountValue = input.DiscountValue,
            MaxRedemptions = input.MaxRedemptions,
            ExpiresAt = input.ExpiresAt,
            RedemptionCount = 0,
            IsActive = true,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        db.Set<BonusCode>().Add(entity);
        await db.SaveChangesAsync(ct);

        return new CreateCodeResult(CreateCodeOutcome.Ok, ToRecord(entity));
    }

    public async Task<BonusCodeRecord?> SetCodeActiveAsync(Guid partnerId, Guid codeId, bool active, CancellationToken ct)
    {
        var entity = await db.Set<BonusCode>()
            .SingleOrDefaultAsync(c => c.Id == codeId && c.PartnerId == partnerId, ct);
        if (entity is null)
        {
            return null;
        }

        if (entity.IsActive != active)
        {
            entity.IsActive = active;
            await db.SaveChangesAsync(ct);
        }

        return ToRecord(entity);
    }

    public async Task<PartnerReport?> GetReportAsync(Guid partnerId, CancellationToken ct)
    {
        var partner = await db.Set<Partner>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == partnerId, ct);
        if (partner is null)
        {
            return null;
        }

        var codes = await LoadCodesAsync(partnerId, ct);
        var totalRedemptions = codes.Sum(c => c.RedemptionCount);

        return new PartnerReport(partner.Id, partner.Name, codes.Count, totalRedemptions, codes);
    }

    private async Task<List<BonusCodeRecord>> LoadCodesAsync(Guid partnerId, CancellationToken ct)
    {
        var codes = await db.Set<BonusCode>().AsNoTracking()
            .Where(c => c.PartnerId == partnerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        return codes.Select(ToRecord).ToList();
    }

    private static BonusCodeRecord ToRecord(BonusCode c) => new(
        c.Id, c.Code, c.DiscountType.ToString(), c.DiscountValue,
        c.MaxRedemptions, c.ExpiresAt, c.RedemptionCount, c.IsActive, c.CreatedAt);

    private static string? Trim(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

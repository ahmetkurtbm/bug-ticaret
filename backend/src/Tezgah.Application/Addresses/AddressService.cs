using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Common.Exceptions;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Domain.Identity;

namespace Tezgah.Application.Addresses;

public interface IAddressService
{
    Task<IReadOnlyList<AddressDto>> ListAsync(long userId, CancellationToken ct);
    Task<AddressDto> CreateAsync(long userId, CreateAddressRequest request, CancellationToken ct);
}

public class AddressService(ITezgahDbContext db) : IAddressService
{
    public async Task<IReadOnlyList<AddressDto>> ListAsync(long userId, CancellationToken ct) =>
        await db.Addresses.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.Id)
            .Select(a => new AddressDto(a.Id, a.Title, a.FullName, a.Phone, a.City, a.District, a.Line, a.PostalCode, a.IsDefault))
            .ToListAsync(ct);

    public async Task<AddressDto> CreateAsync(long userId, CreateAddressRequest request, CancellationToken ct)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
            throw new NotFoundException("Kullanıcı bulunamadı.");

        var hasAny = await db.Addresses.AnyAsync(a => a.UserId == userId, ct);
        var makeDefault = request.IsDefault || !hasAny;
        if (makeDefault)
            await db.Addresses.Where(a => a.UserId == userId && a.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDefault, false), ct);

        var phone = request.Phone.StartsWith('0') ? request.Phone : "0" + request.Phone;
        var address = new Address
        {
            UserId = userId,
            Title = request.Title.Trim(),
            FullName = request.FullName.Trim(),
            Phone = phone,
            City = request.City.Trim(),
            District = request.District.Trim(),
            Line = request.Line.Trim(),
            PostalCode = request.PostalCode,
            IsDefault = makeDefault,
        };
        db.Addresses.Add(address);
        await db.SaveChangesAsync(ct);
        return new AddressDto(address.Id, address.Title, address.FullName, address.Phone, address.City, address.District, address.Line, address.PostalCode, address.IsDefault);
    }
}

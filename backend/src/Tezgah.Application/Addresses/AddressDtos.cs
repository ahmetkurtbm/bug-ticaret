using System.ComponentModel.DataAnnotations;

namespace Tezgah.Application.Addresses;

public record AddressDto(long Id, string Title, string FullName, string? Phone, string City, string District, string Line, string? PostalCode, bool IsDefault);

public class CreateAddressRequest
{
    [Required, MaxLength(50)]
    public string Title { get; set; } = default!;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = default!;

    [Required, RegularExpression(@"^0?5\d{9}$", ErrorMessage = "Cep telefonunu 05XXXXXXXXX biçiminde girin.")]
    public string Phone { get; set; } = default!;

    [Required, MaxLength(60)]
    public string City { get; set; } = default!;

    [Required, MaxLength(60)]
    public string District { get; set; } = default!;

    [Required, MinLength(10), MaxLength(500)]
    public string Line { get; set; } = default!;

    [RegularExpression(@"^\d{5}$", ErrorMessage = "Posta kodu 5 haneli olmalı.")]
    public string? PostalCode { get; set; }

    public bool IsDefault { get; set; }
}

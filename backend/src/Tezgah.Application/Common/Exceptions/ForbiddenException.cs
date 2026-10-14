namespace Tezgah.Application.Common.Exceptions;

/// <summary>Kimliği belli ama bu işlemi yapmaya yetkisi olmayan kullanıcı (403).</summary>
public class ForbiddenException(string message) : Exception(message);

namespace Tezgah.Domain.Common;

/// <summary>Tüm kalıcı varlıkların ortak tabanı. Kimlikler veritabanında bigint identity.</summary>
public abstract class Entity
{
    public long Id { get; set; }
}

using Tezgah.Domain.Common;

namespace Tezgah.Domain.Reviews;

public class Review : AuditableEntity
{
    public long ProductId { get; set; }
    public long UserId { get; set; }
    public long? OrderItemId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
}

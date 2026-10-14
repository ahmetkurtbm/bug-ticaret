using System.Text.Json;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Domain.Messaging;

namespace Tezgah.Application.Common.Messaging;

public static class Outbox
{
    /// <summary>Olayı outbox'a ekler. Kaydetme çağıranın SaveChanges/işlemiyle birlikte olur.</summary>
    public static void Add(ITezgahDbContext db, Guid eventId, string type, object payload, DateTimeOffset now) =>
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = eventId,
            Type = type,
            Payload = JsonSerializer.Serialize(payload, payload.GetType(), JsonSerializerOptions.Web),
            OccurredAt = now,
            NextAttemptAt = now,
        });
}

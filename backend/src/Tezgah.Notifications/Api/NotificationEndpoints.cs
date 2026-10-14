using Microsoft.EntityFrameworkCore;
using Tezgah.Notifications.Domain;
using Tezgah.Notifications.Persistence;

namespace Tezgah.Notifications.Api;

public record NotificationSummary(Guid Id, Guid EventId, string EventType, string RecipientEmail, string Subject,
    string Status, int Attempts, string? LastError, DateTimeOffset CreatedAt, DateTimeOffset? SentAt);

public record NotificationDetail(Guid Id, Guid EventId, string EventType, string RecipientEmail, string Subject, string BodyHtml,
    string Status, int Attempts, string? LastError, DateTimeOffset CreatedAt, DateTimeOffset? SentAt);

public record NotificationPage(int Page, int PageSize, int Total, IReadOnlyList<NotificationSummary> Items);

public static class NotificationEndpoints
{
    private const int PageSize = 20;

    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithMetadata(new RequireApiKeyMetadata());

        group.MapGet("", async (string? email, int? page, NotificationsDbContext db, CancellationToken ct) =>
        {
            var current = Math.Max(page ?? 1, 1);
            IQueryable<Notification> query = db.Notifications.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(email))
            {
                var lowered = email.Trim().ToLowerInvariant();
                query = query.Where(n => n.RecipientEmail.ToLower() == lowered);
            }

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .ThenBy(n => n.Id)
                .Skip((current - 1) * PageSize)
                .Take(PageSize)
                .Select(n => new NotificationSummary(n.Id, n.EventId, n.EventType, n.RecipientEmail, n.Subject,
                    n.Status, n.Attempts, n.LastError, n.CreatedAt, n.SentAt))
                .ToListAsync(ct);
            return Results.Ok(new NotificationPage(current, PageSize, total, items));
        });

        group.MapGet("{id:guid}", async (Guid id, NotificationsDbContext db, CancellationToken ct) =>
        {
            var n = await db.Notifications.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
            return n is null
                ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Bildirim bulunamadı")
                : Results.Ok(new NotificationDetail(n.Id, n.EventId, n.EventType, n.RecipientEmail, n.Subject, n.BodyHtml,
                    n.Status, n.Attempts, n.LastError, n.CreatedAt, n.SentAt));
        });
    }
}

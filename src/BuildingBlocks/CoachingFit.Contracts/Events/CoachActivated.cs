namespace CoachingFit.Contracts.Events;

/// <summary>
/// Event: an admin activated a coach account.
///
/// This is a CONTRACT shared between the publisher (Identity) and any consumers
/// (the Notification worker now; the Catalog service later). It is a plain,
/// dependency-free record on purpose — no MassTransit, no EF, nothing — so every
/// service can reference it without dragging in infrastructure.
///
/// As an EVENT it is published, not sent: the publisher announces that something
/// happened and does not know or care who listens. Add a new consumer later and
/// it just works — the publisher never changes.
/// </summary>
public sealed record CoachActivated(
    string UserId,
    string Email,
    string FullName,
    DateTime ActivatedAtUtc);

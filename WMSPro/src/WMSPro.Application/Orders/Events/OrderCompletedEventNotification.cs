using MediatR;
using WMSPro.Domain.Events;

namespace WMSPro.Application.Orders.Events;

/// <summary>
/// MediatR notification wrapper for OrderCompletedEvent.
/// Keeps Domain free from MediatR dependency.
/// </summary>
public record OrderCompletedEventNotification(OrderCompletedEvent Event) : INotification;

using MediatR;
using Winnow.API.Infrastructure.Identity;

namespace Winnow.API.Features.Auth.Register;

public record UserRegisteredEvent(ApplicationUser User) : INotification;

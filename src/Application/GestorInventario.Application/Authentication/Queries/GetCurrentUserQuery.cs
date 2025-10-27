using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Interfaces;
using MediatR;

namespace GestorInventario.Application.Authentication.Queries;

public record GetCurrentUserQuery(int UserId) : IRequest<UserSummaryDto?>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserSummaryDto?>
{
    private readonly IIdentityService identityService;

    public GetCurrentUserQueryHandler(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    public Task<UserSummaryDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        return identityService.GetByIdAsync(request.UserId, cancellationToken);
    }
}

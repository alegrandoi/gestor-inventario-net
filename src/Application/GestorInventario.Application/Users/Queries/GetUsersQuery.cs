using GestorInventario.Application.Authentication.Models;
using GestorInventario.Application.Common.Interfaces;
using MediatR;

namespace GestorInventario.Application.Users.Queries;

public record GetUsersQuery() : IRequest<IReadOnlyCollection<UserSummaryDto>>;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyCollection<UserSummaryDto>>
{
    private readonly IIdentityService identityService;

    public GetUsersQueryHandler(IIdentityService identityService)
    {
        this.identityService = identityService;
    }

    public Task<IReadOnlyCollection<UserSummaryDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return identityService.GetUsersAsync(cancellationToken);
    }
}

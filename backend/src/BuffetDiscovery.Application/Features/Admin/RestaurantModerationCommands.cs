using BuffetDiscovery.Application.Common.Exceptions;
using BuffetDiscovery.Application.Common.Interfaces;
using BuffetDiscovery.Domain.Entities;
using MediatR;

namespace BuffetDiscovery.Application.Features.Admin;

// Four distinct moderation actions an admin can take on a restaurant. Kept as separate
// commands (rather than one generic "SetStatus") because each represents a distinct
// business action even though their handlers are mechanically similar.

public record ApproveRestaurantCommand(int Id) : IRequest;
public record RejectRestaurantCommand(int Id) : IRequest;
public record SuspendRestaurantCommand(int Id) : IRequest;
public record ReinstateRestaurantCommand(int Id) : IRequest;

public class ApproveRestaurantCommandHandler(IRestaurantRepository restaurants, IUnitOfWork unitOfWork)
    : IRequestHandler<ApproveRestaurantCommand>
{
    public async Task Handle(ApproveRestaurantCommand request, CancellationToken ct)
    {
        var r = await restaurants.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException("Restaurant not found.");
        r.Status = RestaurantStatus.Approved;
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public class RejectRestaurantCommandHandler(IRestaurantRepository restaurants, IUnitOfWork unitOfWork)
    : IRequestHandler<RejectRestaurantCommand>
{
    public async Task Handle(RejectRestaurantCommand request, CancellationToken ct)
    {
        var r = await restaurants.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException("Restaurant not found.");
        r.Status = RestaurantStatus.Rejected;
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public class SuspendRestaurantCommandHandler(IRestaurantRepository restaurants, IUnitOfWork unitOfWork)
    : IRequestHandler<SuspendRestaurantCommand>
{
    public async Task Handle(SuspendRestaurantCommand request, CancellationToken ct)
    {
        var r = await restaurants.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException("Restaurant not found.");
        r.Status = RestaurantStatus.Suspended;
        await unitOfWork.SaveChangesAsync(ct);
    }
}

public class ReinstateRestaurantCommandHandler(IRestaurantRepository restaurants, IUnitOfWork unitOfWork)
    : IRequestHandler<ReinstateRestaurantCommand>
{
    public async Task Handle(ReinstateRestaurantCommand request, CancellationToken ct)
    {
        var r = await restaurants.GetByIdAsync(request.Id, ct) ?? throw new NotFoundException("Restaurant not found.");
        r.Status = RestaurantStatus.Approved;
        await unitOfWork.SaveChangesAsync(ct);
    }
}

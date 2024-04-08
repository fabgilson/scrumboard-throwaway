using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using IdentityProvider.Extensions;
using IdentityProvider.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SharedLensResources;

namespace IdentityProvider.Services.External;

[Authorize(Roles = nameof(GlobalLensRole.SystemAdmin))]
public class UserGrpcService : LensUserService.LensUserServiceBase
{
    private readonly UserManager<User> _userManager;

    public UserGrpcService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }
    
    public override async Task<UserResponse> GetUserById(GetUserByIdRequest request, ServerCallContext context)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null) throw new RpcException(new Status(StatusCode.NotFound, "No such user found with given ID"));
        return user.ToUserResponse();
    }

    public override async Task<UserResponse> GetUserByEmail(GetUserByEmailRequest request, ServerCallContext context)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null) throw new RpcException(new Status(StatusCode.NotFound, "No such user found with given email"));
        return user.ToUserResponse();
    }

    public override async Task<UserResponse> GetUserByUserName(GetUserByUserNameRequest request, ServerCallContext context)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user is null) throw new RpcException(new Status(StatusCode.NotFound, "No such user found with given username"));
        return user.ToUserResponse();
    }

    public override async Task<MultipleUserResponse> GetUsersById(GetUsersByIdRequest request, ServerCallContext context)
    {
        var users = await _userManager.Users
            .Where(x => request.UserIds.Contains(x.Id))
            .ToListAsync();
        return new MultipleUserResponse { UserResponses = { users.Select(x => x.ToUserResponse()) } };
    }

    public override async Task<ValidationResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        var existingUser = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (existingUser is null) throw new RpcException(new Status(StatusCode.NotFound, "No such user found with given ID"));
        var newUserValue = request.ToModifiedEntity(existingUser, out var validationState);
        
        if (!validationState.IsValid) return validationState.ToValidationResponse();

        validationState.WithIdentityErrorsForProperty(await _userManager.UpdateAsync(newUserValue), nameof(request.UserId));

        return validationState.ToValidationResponse();
    }

    public override async Task<PaginatedUsersResponse> GetPaginatedUsers(GetPaginatedUsersRequest request, ServerCallContext context)
    {
        var results = _userManager.Users
            .ApplyStringFilter(request.FilteringOptions, nameof(User.FirstName), nameof(User.LastName), nameof(User.Email))
            .ApplyOrderByOptions(request.PaginationRequestOptions.OrderByOptions);
        
        return new PaginatedUsersResponse
        {
            UserResponses = { await results
                .Skip(request.PaginationRequestOptions.Offset)
                .Take(request.PaginationRequestOptions.Limit)
                .Select(x => x.ToUserResponse())
                .ToListAsync() },
            PaginationResponseOptions = new PaginationResponseOptions
            {
                ResultSetSize = results.Count()
            }
        };
    }
}
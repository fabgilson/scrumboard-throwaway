using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Grpc.Core;
using IdentityProvider.Models.Entities;
using IdentityProvider.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SharedLensResources;
using Xunit;

namespace IdentityProvider.Tests.Integration.Tests;

public class UserGrpcServiceTestStartUp : BaseTestStartup
{
    public UserGrpcServiceTestStartUp(IConfiguration configuration, IWebHostEnvironment webHost) 
        : base(configuration, webHost, "LensUserGrpcServiceTestDatabase") { }
}

public class UserGrpcServiceTest : BaseAuthenticationGrpcServiceTest<UserGrpcServiceTestStartUp, LensUserService.LensUserServiceClient>
{
    public UserGrpcServiceTest(CustomWebApplicationFactory<UserGrpcServiceTestStartUp> customWebFactory) 
        : base(customWebFactory) { }

    [Fact]
    public async Task GetUserById_Unauthenticated_UnauthenticatedErrorReturned()
    {
        var action = async () => await UnauthenticatedClient.GetUserByIdAsync(new GetUserByIdRequest());
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.Unauthenticated);
    }
    
    [Fact]
    public async Task GetUserByEmail_Unauthenticated_UnauthenticatedErrorReturned()
    {
        var action = async () => await UnauthenticatedClient.GetUserByEmailAsync(new GetUserByEmailRequest());
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.Unauthenticated);
    }
    
    [Fact]
    public async Task GetUserByUserName_Unauthenticated_UnauthenticatedErrorReturned()
    {
        var action = async () => await UnauthenticatedClient.GetUserByUserNameAsync(new GetUserByUserNameRequest());
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.Unauthenticated);
    }
    
    [Fact]
    public async Task GetUsersById_Unauthenticated_UnauthenticatedErrorReturned()
    {
        var action = async () => await UnauthenticatedClient.GetUsersByIdAsync(new GetUsersByIdRequest());
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.Unauthenticated);
    }
    
    [Fact]
    public async Task GetUserById_RegularUser_PermissionDeniedErrorReturned()
    {
        var action = async () => await RegularUserLensIdAuthClient.GetUserByIdAsync(new GetUserByIdRequest());
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.PermissionDenied);
    }
    
    [Fact]
    public async Task GetUserByEmail_RegularUser_PermissionDeniedErrorReturned()
    {
        var action = async () => await RegularUserLensIdAuthClient.GetUserByEmailAsync(new GetUserByEmailRequest());
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.PermissionDenied);
    }
    
    [Fact]
    public async Task GetUserByUserName_RegularUser_PermissionDeniedErrorReturned()
    {
        var action = async () => await RegularUserLensIdAuthClient.GetUserByUserNameAsync(new GetUserByUserNameRequest());
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.PermissionDenied);
    }
    
    [Fact]
    public async Task GetUsersById_RegularUser_PermissionDeniedErrorReturned()
    {
        var action = async () => await RegularUserLensIdAuthClient.GetUsersByIdAsync(new GetUsersByIdRequest());
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GetUserById_LensAdmin_CorrectUserReturned()
    {
        var expectedBennie = await FindBennieRegular_LensId_Async();
        var actualBennie = await SystemAdminLensIdAuthClient.GetUserByIdAsync(new GetUserByIdRequest { UserId = expectedBennie.Id });
        actualBennie.ShouldMatch(expectedBennie);
    }

    [Fact]
    public async Task GetNonExistingUserById_LensAdmin_NotFoundErrorReturned()
    {
        var action = async () => await SystemAdminLensIdAuthClient.GetUserByIdAsync(new GetUserByIdRequest {UserId = 100});
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetUserByEmail_LensAdmin_CorrectUserReturned()
    {
        var expectedBennie = await FindBennieRegular_LensId_Async();
        var actualBennie = await SystemAdminLensIdAuthClient.GetUserByEmailAsync(new GetUserByEmailRequest { Email = expectedBennie.Email });
        actualBennie.ShouldMatch(expectedBennie);
    }

    [Fact]
    public async Task GetNonExistingUserByEmail_LensAdmin_NotFoundErrorReturned()
    {
        var action = async () => await SystemAdminLensIdAuthClient.GetUserByEmailAsync(new GetUserByEmailRequest { Email = "nothinghere@gmail.com"});
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetUserByUserName_LensAdmin_CorrectUserReturned()
    {
        var expectedBennie = await FindBennieRegular_LensId_Async();
        var actualBennie = await SystemAdminLensIdAuthClient.GetUserByUserNameAsync(new GetUserByUserNameRequest { UserName = expectedBennie.UserName });
        actualBennie.ShouldMatch(expectedBennie);
    }

    [Fact]
    public async Task GetNonExistingUserByUserName_LensAdmin_NotFoundErrorReturned()
    {
        var action = async () => await SystemAdminLensIdAuthClient.GetUserByUserNameAsync(new GetUserByUserNameRequest { UserName = "nobody"});
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.NotFound);
    }
    
    [Fact]
    public async Task GetUsersById_LensAdmin_CorrectUsersReturned()
    {
        var expectedBennieLens = await FindBennieRegular_LensId_Async();
        var expectedBennieLdap = await FindReggieRegular_UcLdap_Async();
        var actualBennieResponse = await SystemAdminLensIdAuthClient.GetUsersByIdAsync(new GetUsersByIdRequest { UserIds = { expectedBennieLdap.Id, expectedBennieLens.Id }});
        actualBennieResponse.UserResponses.Should().HaveCount(2);
        actualBennieResponse.UserResponses.First(x => x.Id == expectedBennieLens.Id).ShouldMatch(expectedBennieLens);
        actualBennieResponse.UserResponses.First(x => x.Id == expectedBennieLdap.Id).ShouldMatch(expectedBennieLdap);
    }

    [Fact]
    public async Task GetNonExistingUsersById_LensAdmin_EmptyListReturned()
    {
        var response = await SystemAdminLensIdAuthClient.GetUsersByIdAsync(new GetUsersByIdRequest { UserIds = { 10, 20, 30 }});
        response.UserResponses.Should().HaveCount(0);
    }

    [Fact]
    public async Task UpdateNonExistingUser_LensAdmin_NotFoundErrorReturned()
    {
        var action = async () => await SystemAdminLensIdAuthClient.UpdateUserAsync(new UpdateUserRequest { UserId = 10 });
        await action.Should().ThrowExactlyAsync<RpcException>()
            .Where(x => x.StatusCode == StatusCode.NotFound);
    }
    
    [Fact]
    public async Task UpdateUserWithAllNewValidDetails_LensAdmin_UserUpdatedSuccessfully()
    {
        var startingBennie = await FindBennieRegular_LensId_Async();
        var updateResponse = await SystemAdminLensIdAuthClient.UpdateUserAsync(new UpdateUserRequest
        {
            UserId = startingBennie.Id,
            NewEmail = "someNewEmail@gmail.com",
            NewFirstName = "Benjamin",
            NewLastName = "Normal",
            NewRole = GlobalLensRole.SystemAdmin,
            NewUserName = "benjamin.normal"
        });
        var updatedBennie = await FindUserByIdAsync(startingBennie.Id);

        using (new AssertionScope())
        {
            updateResponse.IsSuccess.Should().BeTrue();
            updateResponse.ValidationErrors.Should().BeEmpty();
            
            updatedBennie.Should().NotBeEquivalentTo(startingBennie);
            updatedBennie.UserName.Should().Be("benjamin.normal");
            updatedBennie.Email.Should().Be("someNewEmail@gmail.com");
            updatedBennie.FirstName.Should().Be("Benjamin");
            updatedBennie.LastName.Should().Be("Normal");
            updatedBennie.GlobalLensRole.Should().Be(GlobalLensRole.SystemAdmin);
        }
    }
    
    [Fact]
    public async Task UpdateUserWithAllInvalidDetails_LensAdmin_MultipleValidationErrorsReturned()
    {
        var startingBennie = await FindBennieRegular_LensId_Async();
        var updateResponse = await SystemAdminLensIdAuthClient.UpdateUserAsync(new UpdateUserRequest
        {
            UserId = startingBennie.Id,
            NewEmail = "invalid@",
            NewFirstName = "",
            NewLastName = "",
            NewUserName = ""
        });

        using (new AssertionScope())
        {
            updateResponse.IsSuccess.Should().BeFalse();
            updateResponse.ValidationErrors.Should().HaveCount(4);
            updateResponse.ValidationErrors.Should().ContainSingle(x => x.FieldNames.Contains(nameof(User.Email)));
            updateResponse.ValidationErrors.Should().ContainSingle(x => x.FieldNames.Contains(nameof(User.FirstName)));
            updateResponse.ValidationErrors.Should().ContainSingle(x => x.FieldNames.Contains(nameof(User.LastName)));
            updateResponse.ValidationErrors.Should().ContainSingle(x => x.FieldNames.Contains(nameof(User.UserName)));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t \n  ")]
    [InlineData("@gmail.com")]
    [InlineData("email@")]
    public async Task UpdateUserWithInvalidEmail_LensAdmin_EmailValidationErrorReturned(string invalidEmail)
    {
        var startingBennie = await FindBennieRegular_LensId_Async();
        var updateResponse = await SystemAdminLensIdAuthClient.UpdateUserAsync(new UpdateUserRequest
        {
            UserId = startingBennie.Id,
            NewEmail = invalidEmail
        });
        updateResponse.IsSuccess.Should().BeFalse();
        updateResponse.ValidationErrors.Should().HaveCount(1);
        updateResponse.ValidationErrors.Should().ContainSingle(x => x.FieldNames.Contains(nameof(User.Email)));
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t \n  ")]
    [InlineData("username.that.is.too.loooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong")]
    public async Task UpdateUserWithInvalidUsername_LensAdmin_EmailValidationErrorReturned(string invalidUsername)
    {
        var startingBennie = await FindBennieRegular_LensId_Async();
        var updateResponse = await SystemAdminLensIdAuthClient.UpdateUserAsync(new UpdateUserRequest
        {
            UserId = startingBennie.Id,
            NewUserName = invalidUsername
        });
        updateResponse.IsSuccess.Should().BeFalse();
        updateResponse.ValidationErrors.Should().HaveCount(1);
        updateResponse.ValidationErrors.Should().ContainSingle(x => x.FieldNames.Contains(nameof(User.UserName)));
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t \n  ")]
    [InlineData("Name that is too loooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong")]
    public async Task UpdateUserWithInvalidFirstName_LensAdmin_EmailValidationErrorReturned(string invalidFirstName)
    {
        var startingBennie = await FindBennieRegular_LensId_Async();
        var updateResponse = await SystemAdminLensIdAuthClient.UpdateUserAsync(new UpdateUserRequest
        {
            UserId = startingBennie.Id,
            NewFirstName = invalidFirstName
        });
        updateResponse.IsSuccess.Should().BeFalse();
        updateResponse.ValidationErrors.Should().HaveCount(1);
        updateResponse.ValidationErrors.Should().ContainSingle(x => x.FieldNames.Contains(nameof(User.FirstName)));
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t \n  ")]
    [InlineData("Name that is too loooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooong")]
    public async Task UpdateUserWithInvalidLastName_LensAdmin_EmailValidationErrorReturned(string invalidLastName)
    {
        var startingBennie = await FindBennieRegular_LensId_Async();
        var updateResponse = await SystemAdminLensIdAuthClient.UpdateUserAsync(new UpdateUserRequest
        {
            UserId = startingBennie.Id,
            NewLastName = invalidLastName
        });
        updateResponse.IsSuccess.Should().BeFalse();
        updateResponse.ValidationErrors.Should().HaveCount(1);
        updateResponse.ValidationErrors.Should().ContainSingle(x => x.FieldNames.Contains(nameof(User.LastName)));
    }

    [Fact]
    public async Task GetPaginatedUsers_FirstPageOfMany_CorrectResponseReturned()
    {
        var userManager = GetScopedService<UserManager<User>>();
        var expectedTotalCount = userManager.Users.Count();
        
        var paginatedResponse = await SystemAdminLensIdAuthClient.GetPaginatedUsersAsync(new GetPaginatedUsersRequest
        {
            PaginationRequestOptions = new PaginationRequestOptions{ Limit = 1 }
        });

        paginatedResponse.UserResponses.Should().ContainSingle();
        paginatedResponse.PaginationResponseOptions.ResultSetSize.Should().Be(expectedTotalCount);
    }
    
    [Fact]
    public async Task GetPaginatedUsers_SecondPageOfMany_CorrectResponseReturned()
    {
        var userManager = GetScopedService<UserManager<User>>();
        var expectedTotalCount = userManager.Users.Count();
        
        var paginatedResponse = await SystemAdminLensIdAuthClient.GetPaginatedUsersAsync(new GetPaginatedUsersRequest
        {
            PaginationRequestOptions = new PaginationRequestOptions{ Limit = 1, Offset = 1 }
        });

        paginatedResponse.UserResponses.Should().ContainSingle();
        paginatedResponse.PaginationResponseOptions.ResultSetSize.Should().Be(expectedTotalCount);
    }
    
    [Fact]
    public async Task GetPaginatedUsers_PageNumberTooHigh_NoUsersReturned()
    {
        var userManager = GetScopedService<UserManager<User>>();
        var expectedTotalCount = userManager.Users.Count();
        
        var paginatedResponse = await SystemAdminLensIdAuthClient.GetPaginatedUsersAsync(new GetPaginatedUsersRequest
        {
            PaginationRequestOptions = new PaginationRequestOptions{ Limit = 1, Offset = 100 }
        });

        paginatedResponse.UserResponses.Should().BeEmpty();
        paginatedResponse.PaginationResponseOptions.ResultSetSize.Should().Be(expectedTotalCount);
    }

    public static TheoryData<Func<User, object>, string, bool> PaginatedUsersTheoryData => new()
    {
        {user => user.FirstName, nameof(User.FirstName), true},
        {user => user.FirstName, nameof(User.FirstName), false},
        {user => user.LastName, nameof(User.LastName), true},
        {user => user.LastName, nameof(User.LastName), false},
        {user => user.Email, nameof(User.Email), true},
        {user => user.Email, nameof(User.Email), false},
        {user => user.UserName, nameof(User.UserName), true},
        {user => user.UserName, nameof(User.UserName), false},
    };

    [Theory]
    [MemberData(nameof(PaginatedUsersTheoryData))]
    public async Task GetPaginatedUsers_OrderedBySingleProperty_CorrectOrderReturned(Func<User, object> orderBySelector, string propertyName, bool isAscending)
    {
        var userManager = GetScopedService<UserManager<User>>();
        
        var paginatedResponse = await SystemAdminLensIdAuthClient.GetPaginatedUsersAsync(new GetPaginatedUsersRequest
        {
            PaginationRequestOptions = new PaginationRequestOptions 
            { 
                Limit = 10, 
                OrderByOptions = { new OrderByOption {IsAscendingOrder = isAscending, PropertyName = propertyName} }
            }
        });

        var expectedOrderedIds = isAscending 
            ? userManager.Users.AsEnumerable().OrderBy(orderBySelector).Select(x => x.Id) 
            : userManager.Users.AsEnumerable().OrderByDescending(orderBySelector).Select(x => x.Id);
        paginatedResponse.UserResponses.Select(x => x.Id).Should().ContainInConsecutiveOrder(expectedOrderedIds);
    }

    public static TheoryData<string, StringFilterType, bool, string[]> PaginatedFilteredUsersTheoryData = new()
    {
        // 'Contains' matches for first name
        { "Nobody", StringFilterType.Contains, true, new string[] { } },
        { "Bennie", StringFilterType.Contains, true, new [] { "bennie.regular" } },
        { "Sally", StringFilterType.Contains, true, new [] { "sally.sysadmin" } },
        // 'Contains' matches for last name
        { "Regular", StringFilterType.Contains, true, new [] { "reggie.regular", "bennie.regular" } },
        { "Sysadmin", StringFilterType.Contains, true, new [] { "sammie.sysadmin", "sally.sysadmin" } },
        // 'Contains' matches for email
        { "sally.sysadmin@lens.com", StringFilterType.Contains, true, new [] { "sally.sysadmin" } },
        { "@lens.com", StringFilterType.Contains, true, new [] { "bennie.regular", "reggie.regular", "sammie.sysadmin", "sally.sysadmin" } },
    };

    [Theory]
    [MemberData(nameof(PaginatedFilteredUsersTheoryData))]
    public async Task GetPaginatedUsers_Filtered_CorrectUsersReturned(string filterText, StringFilterType filterType, bool isCaseSensitive, string[] expectedUserNames)
    {
        var actualUsers = await SystemAdminLensIdAuthClient.GetPaginatedUsersAsync(new GetPaginatedUsersRequest
        {
            PaginationRequestOptions = new PaginationRequestOptions{ Limit = 10 },
            FilteringOptions = new BasicStringFilteringOptions
            {
                FilterText = filterText,
                FilterType = filterType,
                IsCaseSensitive = isCaseSensitive
            }
        });
        actualUsers.UserResponses.Select(x => x.UserName).Should().BeEquivalentTo(expectedUserNames);
    }
}
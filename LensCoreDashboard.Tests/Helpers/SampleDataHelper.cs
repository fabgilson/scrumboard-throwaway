using Bogus;
using Google.Protobuf.WellKnownTypes;
using SharedLensResources;

namespace LensCoreDashboard.Tests.Helpers;

public class SampleDataHelper
{
    private static int _currentId = 1;

    /// <summary>
    /// Create a user response object representing some user. Values may be given for fields, or random values will
    /// be populated instead.
    /// </summary>
    /// <param name="firstName">Optional first name override</param>
    /// <param name="lastName">Optional last name override</param>
    /// <param name="userName">Optional username override</param>
    /// <param name="email">Optional email override</param>
    /// <param name="identitySource">Optional identity source, defaults to 'Lens'</param>
    /// <param name="lensRole">Optional lens role, defaults to 'User'</param>
    /// <returns>New UserResponse object for some fake user</returns>
    public static UserResponse CreateNewUserResponse(
        string? firstName = null,
        string? lastName = null,
        string? userName = null,
        string? email = null,
        string? identitySource = "Lens",
        GlobalLensRole lensRole = GlobalLensRole.User
    ) {
        var faker = new Faker();
        var fakePerson = faker.Person;
        return new UserResponse
        { 
            Id = _currentId++,
            FirstName = firstName ?? fakePerson.FirstName,
            LastName = lastName ?? fakePerson.LastName,
            UserName = userName ?? fakePerson.UserName,
            Email = email ?? fakePerson.Email,
            IdentitySource = identitySource,
            Created = Timestamp.FromDateTimeOffset(DateTime.Now),
            LensRole = lensRole
        };
    }
}
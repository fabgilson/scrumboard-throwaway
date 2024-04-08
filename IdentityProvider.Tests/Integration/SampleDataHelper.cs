using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityProvider.DataAccess;
using IdentityProvider.Models.Entities;
using SharedLensResources;

namespace IdentityProvider.Tests.Integration;

public static class SampleDataHelper
{
    // Credentials that are true LENS user accounts
    public static User GetBennieRegular_LensId() => new()
    {
        Email = "bennie.regular@lens.com",
        GlobalLensRole = GlobalLensRole.User,
        FirstName = "Bennie",
        LastName = "Regular",
        UserName = "bennie.regular",
        EmployeeId = 1111122222,
        Created = DateTime.Now,
        IdentitySource = IdentitySource.Lens
    };
    public static readonly string BennieRegularPassword = "B3nni3Regul@r";
    
    public static User GetSallySysadmin_LensId() => new()
    {
        Email = "sally.sysadmin@lens.com",
        GlobalLensRole = GlobalLensRole.SystemAdmin,
        FirstName = "Sally",
        LastName = "Sysadmin",
        UserName = "sally.sysadmin",
        EmployeeId = 999991111,
        Created = DateTime.Now,
        IdentitySource = IdentitySource.Lens
    };
    public static readonly string SallySysAdminPassword = "S@llySys4dmin";
}
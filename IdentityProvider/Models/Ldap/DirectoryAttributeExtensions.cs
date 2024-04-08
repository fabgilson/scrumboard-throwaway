using System;
using Novell.Directory.Ldap;

namespace IdentityProvider.Models.Ldap;

public static class DirectoryAttributeExtensions
{
    public static string ToSingletonString(this LdapAttribute attribute)
    {
        if (attribute == null) return "";
        return attribute.StringValue;
    }

    public static int ToSingletonInteger(this LdapAttribute attribute)
    {
        if (attribute == null) return 0;
        return Convert.ToInt32(attribute.StringValue);
    }
}
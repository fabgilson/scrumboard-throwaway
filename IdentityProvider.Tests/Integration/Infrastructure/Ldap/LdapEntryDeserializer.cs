using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Novell.Directory.Ldap;

namespace IdentityProvider.Tests.Integration.Infrastructure.Ldap
{
    public static class LdapEntryDeserializer
    {
        private class SerializedAttributeSingle
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("value")]
            public string? Value { get; set; }
        }

        private class SerializedAttributeArray
        {
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            [JsonPropertyName("values")]
            public string[]? Values { get; set; }
        }

        private class LdapEntryData
        {
            public string DN { get; set; } = "";
            public ICollection<SerializedAttributeSingle> LdapAttributeSetSingles { get; set; } = new List<SerializedAttributeSingle>();
            public ICollection<SerializedAttributeArray> LdapAttributeSetArrays { get; set; } = new List<SerializedAttributeArray>();

            public LdapEntry ToLdapEntry()
            {
                var ldapEntry = new LdapEntry();
                ldapEntry.Dn = DN;
                foreach(var single in LdapAttributeSetSingles)
                {
                    ldapEntry.GetAttributeSet().Add(new LdapAttribute(single.Type, single.Value));
                }
                foreach(var array in LdapAttributeSetArrays)
                {
                    ldapEntry.GetAttributeSet().Add(new LdapAttribute(array.Type, array.Values));
                }
                return ldapEntry;
            }
        }

        public static LdapEntry DeserializeJsonFile(string filename)
        {
            string jsonString = File.ReadAllText(filename);
            var ldapEntryData = JsonSerializer.Deserialize<LdapEntryData>(jsonString)!;
            return ldapEntryData.ToLdapEntry();
        }
    }
}

# IdentityProvider

# Configuration

The application primarily sources its configuration values from the `appsettings.{Environment}.json` files located in the project root. You can reference the `appsettings.Development.json` for an example of the default configurations. Additionally, these configuration values can be overridden by environment variables for added flexibility and security (such as when creating docker containers for deployments).

- `Urls`: The URLs for the application, separated by semicolons (e.g., `http://localhost:9000;https://localhost:9001`). **Note**: A _gRPC_ server will be hosted at these endpoints.

- `Database`:
    - `UseInMemory`: True to use in-memory database, false to use an actual database instance.
    - `DatabaseName`: Name of the database, always required.
    - `Host`: Database host, required only if not using InMemory.
    - `Username`: Database username, required only if not using InMemory.
    - `Password`: Database password, required only if not using InMemory.
    - `Port`: Database port, required only if not using InMemory.

- `Ldap`: Configuration related to LDAP.
    - `HostName`: LDAP host name.
    - `HostPort`: LDAP host port.
    - `UseSsl`: Whether to use SSL in LDAP connections.
    - `IgnoreCertificateVerification`: Set to true to skip checking certificates.
    - `DomainName`: AD Domain name, appended to usernames when authenticating.
    - `UserQueryBase`: Base for user queries in LDAP.
    - `DefaultAdminUserCodes`: Default admin user codes for LDAP.

- `SigningKey`: Key used for signing (should be big and kept secret).

- `SeedSampleUserAccounts`: Flag to determine if sample user accounts should be seeded, only use in development.

## Overriding with Environment Variables

To override any of the above configurations, you can set environment variables. The environment variables should be named by following the path to the JSON property in the file, with each level separated by a double underscore (__).

```shell
export IdentityProviderUrl=http://localhost:16000
export Database__UseInMemory=false
```

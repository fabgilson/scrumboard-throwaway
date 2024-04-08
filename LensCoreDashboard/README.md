# LensCoreDashboard Configuration

The application primarily sources its configuration values from the `appsettings.{Environment}.json` files located in the project root. You can reference the `appsettings.Development.json` for an example of the default configurations. Additionally, these configuration values can be overridden by environment variables for added flexibility and security (such as when creating docker containers for deployments).

## Configuration Variables

Here's a list of the main configuration variables available and a brief description for each:

- `Urls`: The URLs for the application, separated by semicolons (e.g., `http://localhost:9000;https://localhost:9001`)

- `AppBaseUrl`: Any base for the application URL, such as if running behind a reverse proxy. Leave as empty string for no prefix.

- `IdentityProviderUrl`: URL pointing to the `IdentityProvider` for authentication.

## Overriding with Environment Variables

To override any of the above configurations, you can set environment variables. The environment variables should be named by following the path to the JSON property in the file, with each level separated by a double underscore (__).

```shell
export IdentityProviderUrl=http://localhost:16000
export Database__UseInMemory=false
```

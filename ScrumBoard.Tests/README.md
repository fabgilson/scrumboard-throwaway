# ScrumBoard.Tests

Our testing strategy has evolved since the start of this project, so there remain a lot of older testing approaches still in use in this code base (IIABDFI). For any new features, or new tests for old features, the following test types should be used.

## Unit tests

Where possible, unit tests should be written for incoming functionality and placed in `ScrumBoard.Tests/Unit`. In a pragmatic sense, testing of flows that depend heavily on data persistence and retrieval should probably be avoided here, as the degree of mocking needed would greatly reduce the value of such tests. As such, we will generally only write unit tests for [***pure functions***](https://functionalprogrammingcsharp.com/pure-functions). 

### Examples

Some good examples to look to of when unit testing is a sensible approach include:

- Unit/Utils/DurationUtilsTest.cs
- Unit/Validators/*

## Blazor Component tests

All new Blazor components (pages included) should be accompanied with bUnit tests in `ScrumBoard.Tests/Blazor`. The only external dependencies such components should have are for service-layer objects, and all such dependencies should be mocked (we let the integration tests determine if the inner workings of services are behaving as expected). 

> As static code analysers like SonarQube aren't great at picking up coverage on .razor files, ensure that new Blazor components with any significant C# code have this code placed in a code behind file (.razor.cs).

### Examples

- Blazor/StandUps/AdminStandUpSchedulePageTest.cs
- Blazor/LiveTimeTextTests.cs
  - Handling time-sensitive events using `IClock` interface
- Blazor/StudentGuideTests.cs
  - Mocking file system and IO operations with `MockFileSystem`
- Blazor/Reports/ReportComponentTest.cs
  - Conditional rendering based on project role
- Blazor/Reports/BurndownReportTest.cs
  - Verifying JavaScript interoperability

### ToDo

With a recent change to have many project-scoped components inheriting from `BaseProjectScopedComponent`, we can create a corresponding base class for tests to extend from to simplify the testing configuration. Currently tracked by GitLab issue [#146](https://eng-git.canterbury.ac.nz/se-projects/lens-group/lens-resurrected/-/work_items/146).

## Integration tests

Any new features involving data persistence or retrieval should be accompanied by integration tests in `ScrumBoard.Tests/Integration`. For general integration tests (i.e., for services and controllers) there should be no mocking, and tests should use an in memory database with testing data added as necessary. All such integration tests should inherit from `Integration/Infrastructure/BaseIntegrationTestFixture.cs` to simplify this process. 

### BaseIntegrationTestFixture.cs

This class is intended to be used as a base class for (pretty much) all integration tests. It registers all default services using their concrete implementations as specified in `ScrumBoard/Program.cs` (except those which require JS interop for which mocks are provided), and handles resetting the database after each test case. Most setup needed for an integration test, e.g. adding test data to DB, should be doable through overriding the setup methods in this class.
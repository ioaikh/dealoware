using Microsoft.AspNetCore.Mvc.Testing;

namespace Dealoware.Api.Tests;

/// <summary>
/// Collection definition for tests that use WebApplicationFactory.
/// All test classes in this collection share the same factory instance
/// and run serially to avoid SQLite database file conflicts.
/// </summary>
[CollectionDefinition("WebAppTests")]
public class WebAppTestCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
}

namespace TaskFlow.IntegrationTests;

/// <summary>
/// All integration test classes share a single <see cref="TaskFlowApiFactory"/> (and therefore a
/// single Postgres Testcontainer), since starting a new container per test class would be slow.
/// Tests are responsible for using unique data or relying on <see cref="TaskFlowApiFactory.ResetDatabaseAsync"/>.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<TaskFlowApiFactory>
{
    public const string Name = "TaskFlow API integration tests";
}

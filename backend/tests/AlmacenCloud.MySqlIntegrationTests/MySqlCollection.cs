namespace AlmacenCloud.MySqlIntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MySqlCollection : ICollectionFixture<MySqlTestDatabase>
{
    public const string Name = "MySQL relational tests";
}

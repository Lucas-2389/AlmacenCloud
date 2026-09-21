namespace AlmacenCloud.MySqlIntegrationTests;

[Collection(MySqlCollection.Name)]
public sealed class MySqlSafetyTests
{
    [Fact]
    public void UnsafeDatabaseName_IsRejectedBeforeAnyConnection()
    {
        const string variableName = "ALMACENCLOUD_TEST_MYSQL_CONNECTION";
        var originalValue = Environment.GetEnvironmentVariable(variableName);

        try
        {
            Environment.SetEnvironmentVariable(
                variableName,
                "Server=127.0.0.1;Port=1;Database=AlmacenCloud;User=test;Password=test;");

            var database = new MySqlTestDatabase();

            var exception = Assert.Throws<InvalidOperationException>(() => database.RequireConnection());
            Assert.Contains(MySqlTestDatabase.RequiredDatabaseName, exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }
}

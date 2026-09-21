namespace AlmacenCloud.MySqlIntegrationTests;

public sealed class MySqlFactAttribute : FactAttribute
{
    public MySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(MySqlTestDatabase.LoadConnectionString()))
            Skip = "MySQL no configurado: use ALMACENCLOUD_TEST_MYSQL_CONNECTION o ConnectionStrings:MySqlTests.";
    }
}

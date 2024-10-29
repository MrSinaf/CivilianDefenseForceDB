using MySqlConnector;

namespace DataBaseCDF;

public static class DataBase
{
    public static MySqlDataSource cdf { get; internal set; }
}
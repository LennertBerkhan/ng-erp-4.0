using System;
using System.Runtime.InteropServices;

namespace Zpp.Util
{
    public static class Constants
    {
        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsLocalDb = false;
        // TODO: the random/dateTime is a workaround, remove this if drop database query in Dispose() in TestClasses is added

        public static readonly string DefaultDbName = "zpp"; 
        
        public static string GetDbName()
        {
            /*
            // TODO: This is broken locally: every second test run, it breaks with:
            System.Data.SqlClient.SqlException : Cannot open database "zpp2" requested by the login. 
            The login failed. Login failed for user 'sa'.
            
            if (IsWindows)
            {
                // use always the same databaseName and drop db before the next test
                return "zpp2";
            }
            else
            {*/
                // never got this feature (reuse same database by dropping an recreating)
                // working on linux/unix,
                // so use a new database for every test
                return $"zpp{GetDateString()}";
            //}
        }

        private static string GetDateString()
        {
            string ticks = DateTime.UtcNow.Ticks.ToString();
            // the last 9 decimal places of ticks are enough
            return DateTime.Now.ToString("MM-dd_HH:mm") + $"__{ticks.Substring(10, ticks.Length-10)}";
        }

        private static string DbConnectionZppLocalDb(string dbName)
        {
            return $"Server=(localdb)\\mssqllocaldb;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=true";
        }
        
        private static String DbConnectionZppSqlServer(string dbName)
        {
            return $"Data Source=localhost;Database={dbName};" +
                   $"MultipleActiveResultSets=true;User ID=SA;Password=123*Start#";
        }

        public static string GetConnectionString(string dbName)
        {
            if (UseLocalDb() && Constants.IsWindows)
            {
                return DbConnectionZppLocalDb(dbName);
            }
            else
            {
                return DbConnectionZppSqlServer(GetDbName() + dbName);
            }
        }

        public static string EnumToString<T>(T enumValue, Type enumType)
        {
            return Enum.GetName(enumType, enumValue);
        }
        
        /// <summary>
        /// If localDb shall be used - set it via command line with :
        /// setx UseLocalDb true
        /// </summary>
        /// <returns></returns>
        public static bool UseLocalDb()
        {
            var environmentUseLocalDb = Environment.GetEnvironmentVariable("UseLocalDb", EnvironmentVariableTarget.User);
            if (environmentUseLocalDb != null)
                return environmentUseLocalDb.Equals("true");
            return false;
        }        
    }
}
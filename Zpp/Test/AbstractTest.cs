using Master40.DB.Data.Context;
using Master40.DB.Data.WrappersForPrimitives;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.IO;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Test.Configuration;
using Zpp.Test.Configuration.Scenarios;
using Zpp.Utils;

namespace Zpp.Test
{
    /**
     * A test can be initialized via base() constructor on three ways:
     * - no dbInit: use base(false)
     * - dbInit: default db (truck scenario) use base(true) else use base(TestConfigurationFileNames.X)
     * - dbInit + CO/COP: use base(false) and call InitTestScenario(TestConfigurationFileNames.X)
     */
    public abstract class AbstractTest : IDisposable
    {
        private readonly NLog.Logger LOGGER = NLog.LogManager.GetCurrentClassLogger();
        protected readonly ProductionDomainContext ProductionDomainContext;

        protected static TestConfiguration TestConfiguration;

        protected static readonly string DefaultTestScenario =
            TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2;

        private IDbMasterDataCache _dbMasterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();

        public AbstractTest() : this(true)
        {
            
        }

        /**
         * This constructor must be always called (else ProductionDomainContext is null)
         * --> seems a bit strange, but is needed to enable parameterized tests:
         * the default db should not be initialized in this case, but the testConfig is not available as constructor parameter
         */
        public AbstractTest(bool initDefaultTestConfig)
        {
            ProductionDomainContext = Dbms.GetDbContext();
            if (initDefaultTestConfig)
            {
                InitTestScenario(DefaultTestScenario);
            }
        }

        // @before
        public AbstractTest(string testConfiguration) : this(false)
        {
            InitDb(testConfiguration);
        }

        // @after
        public void Dispose()
        {
            ProductionDomainContext.Database.CloseConnection();
        }

        /**
         * Initialize the db:
         * - deletes current
         * - creates db according to given configuration
         */
        protected void InitDb(string testConfiguration)
        {
            TestConfiguration = ReadTestConfiguration(testConfiguration);
            if (Constants.IsLocalDb)
            {
                bool isDeleted = ProductionDomainContext.Database.EnsureDeleted();
                if (!isDeleted)
                {
                    LOGGER.Error("Database could not be deleted.");
                }
            }

            else if(Constants.IsLocalDb == false && Constants.IsWindows)
            {
                bool wasDropped = Dbms.DropDatabase(
                    Constants.GetDbName(),
                    Constants.GetConnectionString());
                if (wasDropped == false)
                {
                    LOGGER.Warn($"Database {Constants.GetDbName()} could not be dropped.");
                }
            }

            Type dbSetInitializer = Type.GetType(TestConfiguration.DbSetInitializer);
            dbSetInitializer.GetMethod("DbInitialize").Invoke(null, new[]
            {
                ProductionDomainContext
            });

            LotSize.LotSize.SetDefaultLotSize(new Quantity(TestConfiguration.LotSize));
            LotSize.LotSize.SetLotSizeType(TestConfiguration.LotSizeType);
        }

        /**
         * init db and customerOrders
         */
        protected void InitTestScenario(string testConfiguration)
        {
            InitDb(testConfiguration);
            
        }

        private static TestConfiguration ReadTestConfiguration(string testConfigurationFileNames)
        {
            return JsonConvert.DeserializeObject<TestConfiguration>(
                File.ReadAllText(testConfigurationFileNames));
        }
    }
}
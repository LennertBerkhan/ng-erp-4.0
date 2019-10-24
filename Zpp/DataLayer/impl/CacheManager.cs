using System;
using System.IO;
using Master40.DB.Data.Context;
using Master40.DB.Data.WrappersForPrimitives;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Zpp.DataLayer.impl.OpenDemand;
using Zpp.Mrp2.impl.Mrp1.impl.LotSize.Impl;
using Zpp.Test.Configuration;
using Zpp.Util;

namespace Zpp.DataLayer.impl
{
    public class CacheManager: ICacheManager
    {
        private DbTransactionData _dbTransactionData;
        private DbTransactionData _dbTransactionDataArchive;
        private DbMasterDataCache _dbMasterDataCache;
        private ProductionDomainContext _productionDomainContext;
        private ProductionDomainContext _productionDomainContextArchive;
        private IOpenDemandManager _openDemandManager;
        private TestConfiguration _testConfiguration;
        private IAggregator _aggregator;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        
        public void InitByReadingFromDatabase(string testConfiguration)
        {
            ProductionDomainContexts productionDomainContexts = Dbms.GetDbContext();
            _productionDomainContext = productionDomainContexts.ProductionDomainContext;
            
            _productionDomainContextArchive = productionDomainContexts.ProductionDomainContextArchive;
            
            InitDb(testConfiguration, _productionDomainContext, true);
            InitDb(testConfiguration, _productionDomainContextArchive, false);
            
            _dbMasterDataCache = new DbMasterDataCache(_productionDomainContext);
            // duplicate masterData for archive
            _dbMasterDataCache.Clone(_productionDomainContextArchive);
            
            _dbTransactionData = new DbTransactionData(_productionDomainContext);
            _dbTransactionDataArchive = new DbTransactionData(_productionDomainContextArchive);
            _aggregator = new Aggregator(_dbTransactionData);
            _openDemandManager = new OpenDemandManager(true);
        }

        public IDbTransactionData ReloadTransactionData()
        {
            _dbTransactionData = new DbTransactionData(_productionDomainContext);
            _dbTransactionDataArchive = new DbTransactionData(_productionDomainContextArchive);
            _openDemandManager = new OpenDemandManager(false);
            _aggregator = new Aggregator(_dbTransactionData);
            return _dbTransactionData;
        }

        public IDbMasterDataCache GetMasterDataCache()
        {
            return _dbMasterDataCache;
        }

        public IDbTransactionData GetDbTransactionData()
        {
            return _dbTransactionData;
        }

        public IDbTransactionData GetDbTransactionDataArchive()
        {
            return _dbTransactionDataArchive;
        }

        public IOpenDemandManager GetOpenDemandManager()
        {
            return _openDemandManager;
        }
        
        /**
         * Initialize the db:
         * - deletes current
         * - creates db according to given configuration
         */
        private void InitDb(string testConfiguration, ProductionDomainContext productionDomainContext, bool InitData)
        {
            _testConfiguration = ReadTestConfiguration(testConfiguration);
            if (Constants.IsLocalDb)
            {
                bool isDeleted = productionDomainContext.Database.EnsureDeleted();
                if (!isDeleted)
                {
                    _logger.Error("Database could not be deleted.");
                }
            }

            else if(Constants.IsLocalDb == false && Constants.IsWindows)
            {
                bool wasDropped = Dbms.DropDatabase(
                    Constants.GetDbName(),
                    Constants.GetConnectionString(Constants.DefaultDbName));
                if (wasDropped == false)
                {
                    _logger.Warn($"Database {Constants.GetDbName()} could not be dropped.");
                }
            }

            if (InitData)
            {
                Type dbSetInitializer = Type.GetType(_testConfiguration.DbSetInitializer);
                dbSetInitializer.GetMethod("DbInitialize").Invoke(null, new[]
                {
                    productionDomainContext
                });
            }
            else
            {
                productionDomainContext.Database.EnsureCreated();
            }
            
            LotSize.SetDefaultLotSize(new Quantity(_testConfiguration.LotSize));
            LotSize.SetLotSizeType(_testConfiguration.LotSizeType);
        }
        
        private static TestConfiguration ReadTestConfiguration(string testConfigurationFileNames)
        {
            return JsonConvert.DeserializeObject<TestConfiguration>(
                File.ReadAllText(testConfigurationFileNames));
        }

        public ProductionDomainContext GetProductionDomainContext()
        {
            return _productionDomainContext;
        }

        public TestConfiguration GetTestConfiguration()
        {
            return _testConfiguration;
        }

        public void Dispose()
        {
            _openDemandManager.Dispose();
            _openDemandManager = null;
            _dbMasterDataCache = null;
            _testConfiguration = null;
            
            _productionDomainContext.Database.CloseConnection();
            _dbTransactionData.Dispose();
            
            _productionDomainContextArchive.Database.CloseConnection();
            _dbTransactionDataArchive.Dispose();
            
            _productionDomainContext = null;
            _productionDomainContextArchive = null;
            
            _dbTransactionData = null;
            _dbTransactionDataArchive = null;

        }
        
        public IAggregator GetAggregator()
        {
            return _aggregator;
        }

        public void Persist()
        {
            _dbTransactionData.PersistDbCache();
            _dbTransactionDataArchive.PersistDbCache();
        }
    }
}
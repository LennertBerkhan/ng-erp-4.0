using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Xunit;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain.WrappersForCollections;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp;
using Zpp.WrappersForCollections;

namespace Zpp.Test.Integration_Tests
{
    public class TestDemandToProvider : AbstractTest
    {

        public TestDemandToProvider()
        {
            
        }
        
        [Fact]
        public void TestTransactionDataIsCompletelyPersistedAndNoEntityWasLostDuringPersisting()
        {
            // TODO
        }
        
        [Fact]
        public void TestAllDemandsAreInDemandToProviderTable()
        {
            IMrpRun mrpRun = new MrpRun(ProductionDomainContext);
            mrpRun.Start();

            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            IDemands allDbDemands = dbTransactionData.DemandsGetAll();
            IDemandToProviderTable demandToProviderTable = dbTransactionData.DemandToProviderGetAll();

            foreach (var demand in allDbDemands)
            {
                bool isInDemandToProviderTable =
                    demandToProviderTable.Contains(demand);
                Assert.True(isInDemandToProviderTable,
                    $"Demand {demand} is NOT in demandToProviderTable.");
            }
        }
        
        /**
         * Tests, if the demands are theoretically satisfied by looking for providers in ProviderTable
         * --> success does not mean, that the demands from demandToProvider table are satisfied by providers from demandToProviderTable
         */
        [Fact]
        public void TestAllDemandsAreSatisfiedWithinProviderTable()
        {
            IMrpRun mrpRun = new MrpRun(ProductionDomainContext);
            mrpRun.Start();
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            IDemands demands = dbTransactionData.DemandsGetAll();
            IProviders providers = dbTransactionData.ProvidersGetAll();
            IDemands unsatisfiedDemands = providers.CalculateUnsatisfiedDemands(demands);
            foreach (var unsatisfiedDemand in unsatisfiedDemands)
            {
                Assert.True(false,
                    $"The demand {unsatisfiedDemand} should be satisfied, but it is NOT.");
            }
        }
        
        [Fact]
        public void TestAllDemandsAreSatisfiedByProvidersOfDemandToProviderTable()
        {
            IMrpRun mrpRun = new MrpRun(ProductionDomainContext);
            mrpRun.Start();
            
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();
            
            IDemands allDbDemands = dbTransactionData.DemandsGetAll();
            foreach (var demand in allDbDemands)
            {
                Quantity satisfiedQuantity = Quantity.Null();
                dbTransactionData.DemandToProviderGetAll().Select(x =>
                {
                    satisfiedQuantity.IncrementBy(x.Quantity);
                    return x;
                }).Where(x => x.GetDemandId().Equals(demand.GetId()));
                Assert.True(satisfiedQuantity.Equals(demand.GetQuantity()), $"Demand {demand} is not satisfied.");
            }
        }

    }
}
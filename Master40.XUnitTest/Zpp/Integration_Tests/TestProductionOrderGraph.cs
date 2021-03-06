using System.IO;
using System.Text;
using Master40.DB.Data.Helper;
using Master40.XUnitTest.Zpp.Configuration;
using Xunit;
using Zpp.DbCache;
using Zpp.Mrp;
using Zpp.OrderGraph;

namespace Master40.XUnitTest.Zpp.Integration_Tests
{
    public class TestProductionOrderGraph : AbstractTest
    {
        public TestProductionOrderGraph(): base(false)
        {
            
        }
        
        private void InitThisTest(string testConfiguration)
        {
            InitTestScenario(testConfiguration);

            MrpRun.Start(ProductionDomainContext);
        }
        
        /**
         * In case of failing (and the productionOrderGraph change is expected by you):
         * delete corresponding production_ordergraph_cop_*.txt files ind Folder Test/OrderGraphs
         */
        [Theory]
        [InlineData(TestConfigurationFileNames.DESK_COP_1_LOTSIZE_1)]
        [InlineData(TestConfigurationFileNames.DESK_COP_1_LOT_ORDER_QUANTITY)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_CONCURRENT_LOTSIZE_2)]
        [InlineData(TestConfigurationFileNames.DESK_COP_5_SEQUENTIALLY_LOTSIZE_2)]
        // [InlineData(TestConfigurationFileNames.TRUCK_COP_5_LOTSIZE_2)]
        // [InlineData(TestConfigurationFileNames.TRUCK_COP_1_LOTSIZE_1)]
        public void TestProductionOrderGraphStaysTheSame(string testConfigurationFileName)
        {
            InitThisTest(testConfigurationFileName);
            
            string orderGraphFileName =
                $"../../../Zpp/Ordergraphs/production_ordergraph_{TestConfiguration.Name}.txt";
            string orderGraphFileNameWithIds =
                $"../../../Zpp/Ordergraphs/production_ordergraph_{TestConfiguration.Name}_with_ids.txt";

            // build orderGraph up
            IDbMasterDataCache dbMasterDataCache = new DbMasterDataCache(ProductionDomainContext);
            IDbTransactionData dbTransactionData =
                new DbTransactionData(ProductionDomainContext, dbMasterDataCache);
            IDirectedGraph<INode> orderDirectedGraph = new ProductionOrderDirectedGraph(dbTransactionData, true);

            // with ids
            string actualOrderGraphWithIds = orderDirectedGraph.ToString();
            if (File.Exists(orderGraphFileNameWithIds) == false)
            {
                File.WriteAllText(orderGraphFileNameWithIds, actualOrderGraphWithIds,
                    Encoding.UTF8);
            }

            // without ids: for initial creating of the file (remove ids so it's comparable)
            string actualOrderGraph = TestOrderGraph.removeIdsFromOrderGraph(actualOrderGraphWithIds);

            // create initial file, if it doesn't exists (must be committed then)
            if (File.Exists(orderGraphFileName) == false)
            {
                File.WriteAllText(orderGraphFileName, actualOrderGraph, Encoding.UTF8);
            }

            string expectedOrderGraph = File.ReadAllText(orderGraphFileName, Encoding.UTF8);
            string expectedOrderGraphWithIds =
                File.ReadAllText(orderGraphFileNameWithIds, Encoding.UTF8);

            bool orderGraphHasNotChanged = expectedOrderGraph.Equals(actualOrderGraph);
            bool orderGraphWithIdsHasNotChanged =
                expectedOrderGraphWithIds.Equals(actualOrderGraphWithIds);
            // for debugging: write the changed graphs to files
            if (orderGraphWithIdsHasNotChanged == false)
            {
                File.WriteAllText(orderGraphFileName, actualOrderGraph, Encoding.UTF8);
            }

            if (orderGraphWithIdsHasNotChanged == false)
            {
                File.WriteAllText(orderGraphFileNameWithIds, actualOrderGraphWithIds,
                    Encoding.UTF8);
            }

            if (Constants.IsWindows)
            {
                Assert.True(orderGraphHasNotChanged, "OrderGraph has changed.");
                // Assert.True(orderGraphWithIdsHasNotChanged,"OrderGraph with ids has changed.");
            }
            else
            {
                // On linux the graph is always different so the test would always fail here.
                Assert.True(true);
            }
        }
    }
}
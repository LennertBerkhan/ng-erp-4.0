using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Xunit;
using Zpp.DataLayer;
using Zpp.Mrp2.impl.Mrp1.impl.Production.impl.ProductionTypes;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class TestProductionOrders : AbstractTest
    {
        [Fact]
        public void TestProductionOrderBomIsACopyOfArticleBom()
        {
            if (ZppConfiguration.ProductionType.Equals(ProductionType
                .WorkshopProductionClassic))
            {
                Assert.True(true);
            }
            else
            {
                IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
                IDbMasterDataCache dbMasterDataCache =
                    ZppConfiguration.CacheManager.GetMasterDataCache();
                IDbTransactionData dbTransactionData =
                    ZppConfiguration.CacheManager.ReloadTransactionData();

                foreach (var productionOrderBom in dbTransactionData.ProductionOrderBomGetAll())
                {
                    T_ProductionOrderBom tProductionOrderBom =
                        (T_ProductionOrderBom) productionOrderBom.ToIDemand();
                    M_ArticleBom articleBom =
                        dbMasterDataCache.M_ArticleBomGetByArticleChildId(
                            new Id(tProductionOrderBom.ArticleChildId));
                    Assert.True(tProductionOrderBom.Quantity.Equals(articleBom.Quantity),
                        "Quantity of ProductionOrderBom does not equal the quantity from its articleBom.");
                }
            }
        }

        [Fact]
        public void TestProductionOrderOperationIsACopyOfM_Operation()
        {
            if (ZppConfiguration.ProductionType.Equals(ProductionType
                .WorkshopProductionClassic))
            {
                Assert.True(true);
            }
            else
            {
                IZppSimulator zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
                IDbMasterDataCache dbMasterDataCache =
                    ZppConfiguration.CacheManager.GetMasterDataCache();
                IDbTransactionData dbTransactionData =
                    ZppConfiguration.CacheManager.ReloadTransactionData();

                foreach (var productionOrderOperation in dbTransactionData
                    .ProductionOrderOperationGetAll())
                {
                    T_ProductionOrderOperation tProductionOrderOperation =
                        productionOrderOperation.GetValue();
                    T_ProductionOrderBom aProductionOrderBom =
                        (T_ProductionOrderBom) ZppConfiguration.CacheManager.GetAggregator()
                            .GetAnyProductionOrderBomByProductionOrderOperation(
                                productionOrderOperation).ToIDemand();
                    M_ArticleBom articleBom =
                        dbMasterDataCache.M_ArticleBomGetByArticleChildId(
                            new Id(aProductionOrderBom.ArticleChildId));
                    M_Operation mOperation =
                        dbMasterDataCache.M_OperationGetById(
                            new Id(articleBom.OperationId.GetValueOrDefault()));

                    string errorMessage =
                        "Property of ProductionOrderBom does not equal the one from its articleBom.";
                    Assert.True(
                        tProductionOrderOperation.HierarchyNumber.Equals(mOperation
                            .HierarchyNumber), errorMessage);
                    Assert.True(tProductionOrderOperation.Duration.Equals(mOperation.Duration),
                        errorMessage);
                    Assert.True(
                        tProductionOrderOperation.ResourceToolId.Equals(mOperation.ResourceToolId),
                        errorMessage);
                    Assert.True(
                        tProductionOrderOperation.ResourceSkillId.Equals(mOperation
                            .ResourceSkillId), errorMessage);
                }
            }
        }
    }
}
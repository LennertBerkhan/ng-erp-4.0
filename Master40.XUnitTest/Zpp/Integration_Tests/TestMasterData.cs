using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Zpp.Mrp;

namespace Master40.XUnitTest.Zpp.Integration_Tests
{
    public class TestMasterData : AbstractTest
    {

        public TestMasterData()
        {

        }

        [Fact]
        public void TestMasterDataIsNotChangedByMrpRun()
        {
            List<int> countsMasterDataBefore = CountMasterData();

            Assert.True(ProductionDomainContext.CustomerOrders.Count() == TestConfiguration.CustomerOrderPartQuantity,
                "No customerOrders are initially available.");

            MrpRun.Start(ProductionDomainContext);

            // check certain constraints are not violated

            // masterData entities in db must not change within an MrpRun
            List<int> countsMasterDataAfter = CountMasterData();
            Assert.True(countsMasterDataBefore.SequenceEqual(countsMasterDataAfter),
                $"MasterData has changed, which should not be modified by MrpRun: " +
                $"\nBefore: {String.Join(", ", countsMasterDataBefore)}" +
                $"\nAfter: {String.Join(", ", countsMasterDataAfter)}");
        }

        private List<int> CountMasterData()
        {
            List<int> counts = new List<int>();
            counts.Add(ProductionDomainContext.Articles.Count());
            counts.Add(ProductionDomainContext.ArticleBoms.Count());
            counts.Add(ProductionDomainContext.ArticleTypes.Count());
            counts.Add(ProductionDomainContext.ArticleToBusinessPartners.Count());
            counts.Add(ProductionDomainContext.BusinessPartners.Count());
            counts.Add(ProductionDomainContext.Resources.Count());
            counts.Add(ProductionDomainContext.ResourceSetups.Count());
            counts.Add(ProductionDomainContext.ResourceSkills.Count());
            counts.Add(ProductionDomainContext.ResourceTools.Count());
            counts.Add(ProductionDomainContext.Stocks.Count());
            counts.Add(ProductionDomainContext.Units.Count());
            counts.Add(ProductionDomainContext.Operations.Count());
            counts.Add(ProductionDomainContext.CustomerOrders.Count());
            counts.Add(ProductionDomainContext.CustomerOrderParts.Count());
            return counts;
        }
    }
}
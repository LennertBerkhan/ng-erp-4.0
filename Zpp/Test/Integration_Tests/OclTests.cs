using System.Reflection;
using OclAspectTest;
using Xunit;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class OclTests : AbstractTest
    {
        [Fact]
        public void OCL_ProductionOrderOperationScheduling()
        {
            var ocls = @"
context ProductionOrderOperation::GetValue()
    post ProductionOrderOperationScheduling:
        self._productionOrderOperation.StartBackward >= 0 
            or self._productionOrderOperation.StartForward >= 0 
                and self._productionOrderOperation.EndForward >= 0
";
            OclTestProvider.AddConstraints(new[] {"Zpp", "Master40.DB"}, ocls);

            var zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
            var dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            foreach (var productionOrderOperation in dbTransactionData
                .ProductionOrderOperationGetAll())
            {
                var v = productionOrderOperation.GetValue();
            }
        }
    }
}
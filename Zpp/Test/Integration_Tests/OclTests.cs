using System;
using System.IO;
using System.Reflection;
using System.Text;
using OclAspectTest;
using Xunit;
using Xunit.Abstractions;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.ZppSimulator;

namespace Zpp.Test.Integration_Tests
{
    public class OclTests : AbstractTest
    {
        public OclTests(ITestOutputHelper output) : base(true)
        {
            Console.SetOut(new Converter(output));
        }

        [Fact]
        public void OCL_ProductionOrderOperationScheduling()
        {
            var zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
            var dbTransactionData =
                ZppConfiguration.CacheManager.ReloadTransactionData();

            var ocls = @"
        context ProductionOrderOperation::GetValue()
            post ProductionOrderOperationScheduling:
                self._productionOrderOperation.StartBackward >= 0 
                    or self._productionOrderOperation.StartForward >= 0 
                        and self._productionOrderOperation.EndForward >= 0
        ";
            OclTestProvider.AddConstraints(new[] {"Zpp", "Master40.DB"}, ocls);


            foreach (var productionOrderOperation in dbTransactionData
                .ProductionOrderOperationGetAll())
            {
                Console.WriteLine(productionOrderOperation);
                var v = productionOrderOperation.GetValue();
                Assert.True(!(v is null));
            }
        }

        private class Converter : TextWriter
        {
            ITestOutputHelper _output;

            public Converter(ITestOutputHelper output)
            {
                _output = output;
            }

            public override Encoding Encoding => Encoding.Default;

            public override void WriteLine(string message)
            {
                _output.WriteLine(message);
            }

            public override void WriteLine(string format, params object[] args)
            {
                _output.WriteLine(format, args);
            }
        }
    }
}
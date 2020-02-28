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
        public void OCL_Collection()
        {
            /*var ocls = @"
                context DbTransactionData::PersistDbCache()
                    post Allquantor: self.tProductionOrders_FS->forAll(ProdOr | ProdOr.Quantity <> 0)
                    post Existenzquantor: self.tProductionOrders_FS->exists(ProdOr | ProdOr.Id = 12839)
                    post AllquantorExistenzquantor: self.tProductionOrders_FS->forAll( ProdOr | ProdOr.ProductionOrderOperations.ToList()->exists( PrOO | PrOO.HierarchyNumber = 10 ))
                    post Summe: self.tProductionOrders_FS->sum( po | po.Quantity ) > 0
                    post TestBackwardScheduling: self.tProductionOrderOperations_FS->forAll(ProdOrOp | ProdOrOp.EndBackward <> null and ProdOrOp.StartBackward <> null )
                    post TestForwardSchedulingNull: self.tProductionOrderOperations_FS->forAll(ProdOrOp | ProdOrOp.EndForward <> null and ProdOrOp.StartForward <> null )
                    post TestForwardScheduling: self.tProductionOrderOperations_FS->forAll(ProdOrOp | ProdOrOp.EndForward >= 0 and ProdOrOp.StartForward >= 0 )

                context T_ProductionOrder::set_Quantity(value: decimal)
                    pre QuantityGreaterThanZeroPre: value > 0

                context T_ProductionOrderOperation::set_Duration(value: int)
                    pre DurationGreaterThanZeroPre: value > 0

                context T_PurchaseOrderPart::set_Quantity(value: decimal)
                    pre PurQuantityGreaterThanZeroPre: value > 0

                context T_ProductionOrder::get_Name()
                    post CheckStartAndEndTimes: self.ProductionOrderOperations.ToList()->forAll(ProdOrOp1, ProdOrOp2 | 
                            ProdOrOp1.HierarchyNumber < ProdOrOp2.HierarchyNumber and ProdOrOp1 <> ProdOrOp2 
                            implies ProdOrOp1.Start < ProdOrOp1.End and ProdOrOp1.Start >= ProdOrOp2.End) 

                context DbTransactionData::PersistDbCache()
                    post CheckArticleBomSize: self.tProductionOrders_FS->forAll(ProdOr | self._articles->forAll( Art | Art.Id = ProdOr.ArticleId 
                        implies ProdOr.ProductionOrderBoms.ToList().size() = Art.ArticleBoms.ToList().size()))

                context M_Resource::set_ProductionOrderOperations( value: T_ProductionOrderOperation)
                    post CheckMachineOccupancy: self.ProductionOrderOperations.ToList()->forAll( ProOrOp1, ProOrOp2 | ProOrOp1 <> ProOrOp2 implies (ProOrOp1.Start < ProOrOp2.Start and ProOrOp1.End <= ProOrOp2.Start) or ProOrOp1.Start >= ProOrOp2.End )
            ";*/
            
            //Read conditions / OCLs from file
            string ocls = File.ReadAllText(@"C:\temp\OCL_Demo.txt");

            //Attach OCLs to program (Bool para 1. debugger 2. custom methode)
            OclTestProvider.AddConstraints(new[] { "Zpp", "Master40.DB" }, ocls, false, false);

            //Start test
            var zppSimulator = new ZppSimulator.impl.ZppSimulator();
            zppSimulator.StartTestCycle();
            Assert.True(true);
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
            OclTestProvider.AddConstraints(new[] {"Zpp", "Master40.DB"}, ocls, false, false);


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
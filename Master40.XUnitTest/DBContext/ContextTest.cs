using Master40.Agents;
using Master40.Agents.Agents.DataTransformation;
using Master40.DB.Data.Context;
using Master40.DB.Data.Initializer;
using Master40.DB.DataTransformation;
using Master40.DB.GanttplanDB.Models;
using Master40.Tools.Simulation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Master40.XUnitTest.DBContext
{
    public class ContextTest
    {
        private readonly ITestOutputHelper output;

        ProductionDomainContext _ctx = new ProductionDomainContext(new DbContextOptionsBuilder<MasterDBContext>()
            .UseInMemoryDatabase(databaseName: "InMemoryDB")
            .Options);

        InMemoryContext _inMemmoryContext = new InMemoryContext(new DbContextOptionsBuilder<MasterDBContext>()
            .UseInMemoryDatabase(databaseName: "InMemoryDB")
            .Options);

        MasterDBContext _masterDBContext = new MasterDBContext(new DbContextOptionsBuilder<MasterDBContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=Master40;Trusted_Connection=True;MultipleActiveResultSets=true")
            .Options);

        ProductionDomainContext _productionDomainContext = new ProductionDomainContext(new DbContextOptionsBuilder<MasterDBContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=Master40;Trusted_Connection=True;MultipleActiveResultSets=true")
            .Options);

        GPSzenarioContext _gpSzenarioContext = new GPSzenarioContext(new DbContextOptionsBuilder<GPSzenarioContext>()
            .UseSqlite("Filename=C:\\source\\repo\\Master-4.0\\Master40.DB\\GanttplanDB\\GPSzenario.gpsx")
            .Options);

        public ContextTest(ITestOutputHelper outputHelper)
        {
            this.output = outputHelper;
            //_ctx.Database.EnsureDeleted();
            //MasterDBInitializerLarge.DbInitialize(_ctx);
            //MasterDBInitializerSmall.DbInitialize(_ctx);
            //_productionDomainContext.Database.EnsureDeleted();
            //_productionDomainContext.Database.EnsureCreated();
            //MasterDBInitializerLarge.DbInitialize(_productionDomainContext);

        }

        [Fact]
        public void GPContextTest()
        {
            _gpSzenarioContext.Database.EnsureCreated();

            foreach (Config conf in _gpSzenarioContext.Config)
            {
                Debug.WriteLine(conf.PropertyName + ": " + conf.Value + "\n");
                Assert.NotEmpty(conf.PropertyName);
            }
        }

        [Fact]
        public void TransformationTest()
        {
            _masterDBContext.Database.EnsureDeleted();
            MasterDBInitializerBasic.DbInitialize(_masterDBContext);
            _masterDBContext.Database.EnsureCreated();

            List<Dictionary<string, object>> dummyAgentData = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>{
                    { "ContractAgent.requestItem.OrderId", 123 },
                    { "ContractAgent.requestItem.DueTime", 0 },
                    { "ContractAgent.requestItem.Article.Id", 1 },
                    { "ContractAgent.requestItem.Quantity", 2 },
                    { "ContractAgent.requestItem.Article.UnitId", 3 },
                    { "ContractAgent.requestItem.IDemandToProvider.State", 0 },
                    //{ "StorageAgent.StockElement.Id", 12345 },
                    //{ "StorageAgent.StockElement.Article.Id", 1 },
                    //{ "StorageAgent.StockElement.Current", 3.0m },
                    //{ "StorageAgent.StockElement.Article.UnitId", 3 },
                    { "ProductionAgent.RequestItem.Article.Id", 1 },
                    { "ProductionAgent.RequestItem.DueTime", 9000 },
                    { "ProductionAgent.RequestItem.OrderId", 123 },
                    { "ProductionAgent.RequestItem.Quantity", 2 },
                    { "ProductionAgent.RequestItem.Article.UnitId", 3 },
                    { "ProductionAgent.WorkItems.EstimatedStart", 9000},  // Workitems ist eigentlich Liste
                    { "ProductionAgent.WorkItems.EstimatedEnd", 10200},
                    { "ProductionAgent.WorkItems.WorkSchedule.Id", 1},
                    { "ProductionAgent.WorkItems.Id", Guid.Parse("00000000-0000-0000-0000-000000000001")},

                    //{ "ProductionAgent.WorkItems.WorkSchedule.HierarchyNumber", 1},
                    //{ "ProductionAgent.WorkItems.WorkSchedule.Duration", 600},
                },

                new Dictionary<string, object>{
                    { "ProductionAgent.RequestItem.Article.Id", 2 },
                    { "ProductionAgent.RequestItem.DueTime", 7200 },
                    { "ProductionAgent.RequestItem.OrderId", 124 },
                    { "ProductionAgent.RequestItem.Quantity", 2 },
                    { "ProductionAgent.RequestItem.Article.UnitId", 3 },
                    { "ProductionAgent.WorkItems.EstimatedStart", 5400},  // Workitems ist eigentlich Liste
                    { "ProductionAgent.WorkItems.EstimatedEnd", 8400},
                    { "ProductionAgent.WorkItems.WorkSchedule.Id", 2},
                    { "ProductionAgent.WorkItems.Id", Guid.Parse("00000000-0000-0000-0000-000000000002")},
                },

                new Dictionary<string, object>{
                    { "ProductionAgent.RequestItem.Article.Id", 3 },
                    { "ProductionAgent.RequestItem.DueTime", 3600 },
                    { "ProductionAgent.RequestItem.OrderId", 125 },
                    { "ProductionAgent.RequestItem.Quantity", 8 },
                    { "ProductionAgent.RequestItem.Article.UnitId", 3 },
                    { "ProductionAgent.WorkItems.EstimatedStart", 1800},  // Workitems ist eigentlich Liste
                    { "ProductionAgent.WorkItems.EstimatedEnd", 5160},
                    { "ProductionAgent.WorkItems.WorkSchedule.Id", 3},
                    { "ProductionAgent.WorkItems.Id", Guid.Parse("00000000-0000-0000-0000-000000000003")},
                }
            };

            DataTransformationHelper helper = new DataTransformationHelper(_masterDBContext, _gpSzenarioContext);
            //helper.TransformCoreData();
            helper.TransformAgentDataToGp(dummyAgentData);
            //List<Dictionary<string, object>> receivedAgentData = helper.TransformAgentDataToMaster();
        }

        [Fact]
        public void ClearProductionOrdersTest()
        {
            foreach (Productionorder po in _gpSzenarioContext.Productionorder)
            {
                _gpSzenarioContext.Productionorder.Remove(po);
                _gpSzenarioContext.SaveChanges();
            }
            foreach (ProductionorderOperationActivity pooa in _gpSzenarioContext.ProductionorderOperationActivity)
            {
                _gpSzenarioContext.ProductionorderOperationActivity.Remove(pooa);
                _gpSzenarioContext.SaveChanges();
            }
        }

        [Fact]
        public void RetransformAgentDataTest()
        {
            DataTransformationHelper helper = new DataTransformationHelper(_masterDBContext, _gpSzenarioContext);
            List<List<Dictionary<string, object>>> receivedAgentData = helper.TransformAgentDataToMaster();
            foreach (List<Dictionary<string, object>> list in receivedAgentData)
            {
                foreach (Dictionary<string, object> dict in list)
                {
                    output.WriteLine(string.Join("\n", dict.Select(x => x.Key + "=" + x.Value).ToArray()));
                    output.WriteLine("----------");
                }
                output.WriteLine("==========");
            }
            return;
        }

        /// <summary>
        /// to Cleanup ctx for every Test // uncomment this. 
        /// </summary>
        /*
        public void Dispose()
        {
            _ctx.Dispose();
        }
        */
        /*
        [Fact]
        public async Task MrpTestAsync()
        {
            var imc = InMemoryContext.CreateInMemoryContext();
            InMemoryContext.LoadData(_productionDomainContext, imc);
            var msgHub = new Moc.MessageHub();
            var simulation = new Simulator(imc, msgHub);
            await simulation.Simulate(1);
            Assert.Equal(true, _productionDomainContext.Kpis.Any());
        }
        */
        //public DemandToProvider getRequester
        [Fact]
        public async Task AgentSimulationTestAsync()
        {
            //_productionDomainContext.Database.EnsureDeleted();
            //MasterDBInitializerBasic.DbInitialize(_productionDomainContext);
            //_productionDomainContext.Database.EnsureCreated();

            // In-memory database only exists while the connection is open
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
            var connection = new SqliteConnection(connectionStringBuilder.ToString());

            // create OptionsBuilder with InMemmory Context
            var builder = new DbContextOptionsBuilder<MasterDBContext>();
            builder.UseSqlite(connection);

            using (var c = new InMemoryContext(builder.Options))
            {
                c.Database.OpenConnection();
                c.Database.EnsureCreated();
                InMemoryContext.LoadData(_productionDomainContext, c);

                var sim = new AgentSimulation(c, _gpSzenarioContext, new Moc.MessageHub());
                await sim.RunSim(1,1);

                CalculateKpis.CalculateAllKpis(c, 1, DB.Enums.SimulationType.Decentral, 
                                                    _productionDomainContext.GetSimulationNumber(1, DB.Enums.SimulationType.Decentral),true, int.MaxValue);
                //CopyResults.Copy(c, _productionDomainContext);
                Assert.Equal(c.Kpis.Any(), true);
            }
            connection.Close();

            
        }

        [Fact]
        public void TestGanttPlanDll()
        {
            GanttPlanApi.GPGetVersion();

            Assert.True(GanttPlanApi.GPInitInstance());
            Assert.True(GanttPlanApi.GPLic());
            //ApiWrapper.GPImport(null);
            //ApiWrapper.GPOptInit(null);
            //ApiWrapper.GPOptRun();
            //ApiWrapper.GPExport(0);
            //ApiWrapper.GPSaveScenario(0, @"C:\...\MyTargetFile.gpsx");
            Assert.True(GanttPlanApi.GPExitInstance());
        }
        
        /*
        [Fact]
        public async Task MrpInMemmoryTest()
        {            
            var msgHub = new Moc.MessageHub();
            var simulation = new Simulator(_productionDomainContext, msgHub);
            await simulation.Simulate(1);
            Assert.Equal(true, _productionDomainContext.Kpis.Any());
        }


        [Fact]
        public async Task MrpGifflerTest()
        {
            // what we need:
            // processMrp.ExecutePlanning(IDemandToProvider demand, MrpTask task, int simulationId)
            // _capacityScheduling.GifflerThompsonScheduling(simulationId);
            // probably _rebuildNets.Rebuild(simulationId, evaluationContext);
            var ctx = InMemoryContext.CreateInMemoryContext();
            InMemoryContext.LoadData(_productionDomainContext, ctx);

            var scheduling = new Scheduling(ctx);
            var capacityScheduling = new CapacityScheduling(ctx);
            var msgHub = new Moc.MessageHub();
            var rebuildNets = new RebuildNets(ctx);
            var mrpContext = new ProcessMrp(ctx, scheduling, capacityScheduling, msgHub, rebuildNets);
            var maxAllowedTime = ctx.SimulationConfigurations.Where(a => a.Id == 1).Select(x => x.Time + x.MaxCalculationTime).First();
            var orderParts = ctx.OrderParts.Include(a => a.Order).Where(a => a.IsPlanned == false
                                                 && a.Order.DueTime < maxAllowedTime)
                                                 .Include(a => a.Article).ToList();
            var sw = new Stopwatch();
            sw.Start();
            foreach (var orderPart in orderParts.ToList())
            {

                Debug.WriteLine(sw.ElapsedMilliseconds + " Start orderPart Id:" + orderPart.Id);
                var demand = mrpContext.GetDemand(orderPart);
                //run the requirements planning and backward/forward termination algorithm
                mrpContext.ExecutePlanning(demand, MrpTask.All,1);
                Debug.WriteLine(sw.ElapsedMilliseconds + " End orderPart Id:" + orderPart.Id);
            }

            sw.Restart();
            Debug.WriteLine(sw.ElapsedMilliseconds + " Start Rebuild Net");
            rebuildNets.Rebuild(1, _productionDomainContext);
            Debug.WriteLine(sw.ElapsedMilliseconds + " End Rebuild Net");
            sw.Restart();
            Debug.WriteLine(sw.ElapsedMilliseconds + " Start Giffler");
            capacityScheduling.GifflerThompsonScheduling(1);
            Debug.WriteLine(sw.ElapsedMilliseconds + " End Giffler");
            //await mrpTest.CreateAndProcessOrderDemandAll(mrpContext);
            InMemoryContext.SaveData(ctx, _productionDomainContext);
            Assert.Equal(true, (ctx.ProductionOrderWorkSchedules.Any()));
            
        }
        // Load Database from SimulationJason


        private async Task CreateNewContextOptions()
        {
            // Create a fresh service provider, and therefore a fresh 
            // InMemory database instance.
            var serviceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            // Create a new options instance telling the context to use an
            // InMemory database and the new service provider.
            var builder = new DbContextOptionsBuilder<MasterDBContext>();
            builder.UseInMemoryDatabase()
                .UseInternalServiceProvider(serviceProvider);

           var inMem = new MasterDBContext(builder.Options);

            Assert.Equal(true, inMem.Database.GetDbConnection().State == ConnectionState.Open);

        }

        // HardDatabase To InMemory
        [Fact]
        public async Task CopyContext()
        {
            var context = InMemoryContext.CreateInMemoryContext();
            InMemoryContext.LoadData(_productionDomainContext, context);
            Assert.Equal(1, context.SimulationConfigurations.Count());
        }
        
        
        [Theory]
        //[InlineData(SimulationType.Decentral, 5)]
        [InlineData(SimulationType.Decentral, 6)]
        //[InlineData(SimulationType.Decentral, 2)]
        //[InlineData(SimulationType.Decentral, 3)]
        //[InlineData(SimulationType.Central, 6)]
        // [InlineData(SimulationType.Central, 4)]
        // [InlineData(SimulationType.Central, 5)]
        // [InlineData(SimulationType.Central, 6)]
        // [InlineData(SimulationType.Central, 7)]
        public async Task TestKpiCalculation(SimulationType simType, int simId)
        {
             var toRemove = await _productionDomainContext.Kpis.Where(x => x.SimulationType == simType
                                                                           && x.SimulationConfigurationId == 1
                                                                           && x.KpiType == KpiType.MeanTimeToStart)
                 .ToListAsync();
             _productionDomainContext.Kpis.RemoveRange(toRemove);
             _productionDomainContext.SaveChanges();
             //var simConfig = _productionDomainContext.SimulationConfigurations.Where(x => x.Id == 1);
             CalculateKpis.CalculateMeanStartTime(_productionDomainContext
                                             , 1
                                             , simType
                                             , 1
                                             , true
                                             , 20160);

           //  var toRemove2 = await _productionDomainContext.Kpis.Where(x => x.SimulationType == simType
           //                                                                && x.SimulationConfigurationId == simId
           //                                                                && x.KpiType == KpiType.StockEvolution)
           //      .ToListAsync();
           //  _productionDomainContext.Kpis.RemoveRange(toRemove2);
           //  _productionDomainContext.SaveChanges();
           //  
           //  CalculateKpis.ArticleStockEvolution(_productionDomainContext
           //      , simId
           //      , simType
           //      , 1
           //      , true
           //      , 20160);
           // 


            //            CalculateKpis.CalculateAllKpis(
            //CalculateKpis.CalculateMachineUtilization(
            //context: _productionDomainContext,
            //    simulationId: 1,
            //    simulationType: simType,
            //    simulationNumber: 1,
            //    final: true,
            //    time: 20000);

        }

        
    
    /*
        [Fact]
        public async Task TestDistribution()
        {      //Sigma� ==> Varianz
               //Sigma  ==> Standardabweichung
               //Erwartungswert ==> 

            var dl = new List<double>();                        // Erwartungswert // Standardab
            var ln = new MathNet.Numerics.Distributions.LogNormal(0.0, 0.2);
            
            for (int i = 0; i < 10000; i++)
            {
                //Debug.WriteLine(ln.Sample());
                dl.Add(ln.Sample());
            }
            CreateCSVFromDoubleList(dl,"lognormal.csv");

            var uniformSamples = OrderGenerator.TestUniformDistribution(1000);
            CreateCSVFromDoubleList(uniformSamples, "uniform.csv");

            var exponentialSamples = OrderGenerator.TestExponentialDistribution(1000);
            CreateCSVFromDoubleList(exponentialSamples.ToList(), "exponential.csv");
        }

    */

        /// <summary>
        /// Creates the CSV from a generic list.
        /// </summary>;
        /// <typeparam name="T"></typeparam>;
        /// <param name="list">The list.</param>;
        /// <param name="csvNameWithExt">Name of CSV (w/ path) w/ file ext.</param>;
        private static void CreateCSVFromDoubleList (List<double> list, string csvNameWithExt)
        {
            if (list == null || list.Count == 0) return;
            
            var filestream = System.IO.File.Create(csvNameWithExt);
            var sw = new System.IO.StreamWriter(filestream);
            foreach (var item in list)
            {
                sw.WriteLine(item);
            }
        }
        
    }
}


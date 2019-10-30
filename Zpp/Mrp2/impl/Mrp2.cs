using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.Context;
using Zpp.DataLayer;
using Zpp.DataLayer.impl;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.WrappersForCollections;
using Zpp.DataLayer.impl.ProviderDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain.WrappersForCollections;
using Zpp.Mrp2.impl.Mrp1;
using Zpp.Mrp2.impl.Scheduling;
using Zpp.Mrp2.impl.Scheduling.impl;
using Zpp.Mrp2.impl.Scheduling.impl.JobShopScheduler;
using Zpp.Util;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;
using Zpp.Util.Performance;

namespace Zpp.Mrp2.impl
{
    public class Mrp2 : IMrp2
    {
        
        private readonly IJobShopScheduler _jobShopScheduler = new JobShopScheduler();
        private readonly PerformanceMonitors _performanceMonitors;

        public Mrp2(PerformanceMonitors performanceMonitors)
        {
            _performanceMonitors = performanceMonitors;
        }

        private void ManufacturingResourcePlanning(IDemands dbDemands)
        {
            if (dbDemands == null || dbDemands.Any() == false)
            {
                throw new MrpRunException(
                    "How could it happen, that no dbDemands are given to plan ?");
            }

            // MaterialRequirementsPlanning
            _performanceMonitors.Start(InstanceToTrack.Mrp1);
            IMrp1 mrp1 = new Mrp1.impl.Mrp1(dbDemands);
            mrp1.StartMrp1();
            _performanceMonitors.Stop(InstanceToTrack.Mrp1);
            
            // BackwardForwardBackwardScheduling
            _performanceMonitors.Start(InstanceToTrack.BackwardForwardBackwardScheduling);
            OrderOperationGraph orderOperationGraph = new OrderOperationGraph();
            AssertGraphsAreNotEmpty(orderOperationGraph);

            ScheduleBackward(orderOperationGraph.GetRootNodes().ToStack(), orderOperationGraph,
                true);

            ScheduleForward(orderOperationGraph);

            INodes childRootNodes = new Nodes();
            foreach (var rootNode in orderOperationGraph.GetRootNodes().ToStackSet())
            {
                IProviders childProviders = ZppConfiguration.CacheManager.GetAggregator()
                    .GetAllChildProvidersOf((Demand) rootNode.GetEntity());
                if (childProviders.Count() != 1)
                {
                    throw new MrpRunException(
                        "A CustomerOrderPart is only allowed to have exact one provider.");
                }

                childRootNodes.AddAll(childProviders.ToNodes());
            }

            ScheduleBackward(childRootNodes.ToStack(), orderOperationGraph, false);
            _performanceMonitors.Stop(InstanceToTrack.BackwardForwardBackwardScheduling);

            // job shop scheduling
            _performanceMonitors.Start(InstanceToTrack.JobShopScheduling);
            JobShopScheduling();
            _performanceMonitors.Stop(InstanceToTrack.JobShopScheduling);
            
        }

        private void AssertGraphsAreNotEmpty(OrderOperationGraph orderOperationGraph)
        {
            if (ZppConfiguration.IsInPerformanceMode == false)
            {
                DemandToProviderGraph demandToProviderGraph = new DemandToProviderGraph();
                if (demandToProviderGraph.IsEmpty())
                {
                    throw new MrpRunException("How could the demandToProviderGraph be empty ?");
                }
                
                if (orderOperationGraph.IsEmpty())
                {
                    throw new MrpRunException("How could the orderOperationGraph be empty ?");
                }
            }
        }

        private void ScheduleBackward(Stack<INode> rootNodes,
            IDirectedGraph<INode> orderOperationGraph, bool clearOldTimes)
        {
            IBackwardsScheduler backwardsScheduler =
                new BackwardScheduler(rootNodes, orderOperationGraph, clearOldTimes);
            backwardsScheduler.ScheduleBackward();
        }

        private void ScheduleForward(OrderOperationGraph orderOperationGraph)
        {
            IForwardScheduler forwardScheduler = new ForwardScheduler(orderOperationGraph);
            forwardScheduler.ScheduleForward();
        }

        private void JobShopScheduling()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            IAggregator aggregator = ZppConfiguration.CacheManager.GetAggregator();

            if (dbTransactionData.ProductionOrderGetAll().Any() == false)
            {
                // no JobShopScheduling needed, all Demands were satisfied without the need for a productionOrder
                return;
            }

            foreach (var productionOrder in dbTransactionData.ProductionOrderGetAll())
            {
                List<ProductionOrderOperation> operations =
                    aggregator.GetProductionOrderOperationsOfProductionOrder(
                        (ProductionOrder) productionOrder);
                if (operations == null || operations.Any() == false)
                {
                    throw new MrpRunException(
                        "How could it happen, that a productionOrder without operations exists ?");
                }
            }

            _jobShopScheduler.ScheduleWithGifflerThompsonAsZaepfel(new PriorityRule());
        }

        public void StartMrp2()
        {
            // execute mrp2
            Demands unsatisfiedCustomerOrderParts = ZppConfiguration.CacheManager.GetAggregator()
                .GetUnsatisifedCustomerOrderParts();
            ManufacturingResourcePlanning(unsatisfiedCustomerOrderParts);
        }
    }
}
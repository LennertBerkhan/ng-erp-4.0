using System;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.SimulationCore.DistributionProvider;
using Master40.SimulationCore.Environment.Options;
using Zpp.DataLayer;
using Zpp.Test.Configuration.Scenarios;

namespace Zpp.ZppSimulator.impl.CustomerOrder.impl
{
    public class CustomerOrderCreator : ICustomerOrderCreator
    {
        private IOrderGenerator _orderGenerator = null;

        public void CreateCustomerOrders(SimulationInterval interval)
        {
            CreateCustomerOrders(interval, null);
        }

        public void CreateCustomerOrders(SimulationInterval interval,
            Quantity customerOrderQuantity)
        {
            IDbMasterDataCache masterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();
            OrderArrivalRate orderArrivalRate;
            if (customerOrderQuantity == null)
            {
                orderArrivalRate = new OrderArrivalRate(0.025);
            }
            else
            {
                // (Menge der zu erzeugenden auftrage im intervall +1) / (die dauer des intervalls)
                // works only small numbers e.g. 10
                orderArrivalRate =
                    new OrderArrivalRate((double) (customerOrderQuantity.GetValue() * 2) /
                                         interval.Interval);
            }

            if (_orderGenerator == null ||
                _orderGenerator.GetOrderArrivalRate().Equals(orderArrivalRate) == false)
            {
                _orderGenerator = TestScenario.GetOrderGenerator(new MinDeliveryTime(200),
                    new MaxDeliveryTime(1430), orderArrivalRate, masterDataCache.M_ArticleGetAll(),
                    masterDataCache.M_BusinessPartnerGetAll());
            }

            var random = new Random();
            var startOrderCreation = interval.StartAt;
            var endOrderCreation = interval.EndAt;
            var defaultMaxOrdersPerInterval = 10;
            int createdCustomerOrders = 0;

            // Generate exact given quantity of customerOrders
            while (true)
            {
                long creationTime = startOrderCreation +
                                    (long) random.NextDouble() *
                                    (endOrderCreation - startOrderCreation);
                var order = _orderGenerator.GetNewRandomOrder(time: creationTime);
                foreach (var orderPart in order.CustomerOrderParts)
                {
                    orderPart.CustomerOrder = order;
                    orderPart.CustomerOrderId = order.Id;
                    dbTransactionData.CustomerOrderPartAdd(orderPart);
                }

                dbTransactionData.CustomerOrderAdd(order);
                createdCustomerOrders++;
                
                if (customerOrderQuantity != null &&
                    createdCustomerOrders >= customerOrderQuantity.GetValue())
                {
                    break;
                }
                else if (customerOrderQuantity == null &&
                         createdCustomerOrders >= defaultMaxOrdersPerInterval)
                {
                }
            }
        }
    }
}
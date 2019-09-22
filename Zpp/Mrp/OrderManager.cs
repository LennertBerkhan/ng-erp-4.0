using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Common.DemandDomain;
using Zpp.DbCache;
using Zpp.Mrp.ProductionManagement;
using Zpp.Mrp.PurchaseManagement;

namespace Zpp.Mrp
{
    /**
     * abstracts over PurchaseManager+ProductionManager
     */
    public class OrderManager : IProvidingManager
    {
        private readonly IProvidingManager _purchaseManager;
        private readonly IProvidingManager _productionManager;

        public OrderManager()
        {
            _purchaseManager = new PurchaseManager();
            _productionManager = new ProductionManager();
        }

        public ResponseWithProviders Satisfy(Demand demand, Quantity demandedQuantity, IDbTransactionData dbTransactionData)
        {
            if (demand.GetArticle().ToBuild)
            {
                return _productionManager.Satisfy(demand,
                    demandedQuantity, dbTransactionData);
            }
            else
            {
                return _purchaseManager.Satisfy(demand,
                    demandedQuantity, dbTransactionData);
            }
        }
    }
}
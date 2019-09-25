using Master40.DB.Data.WrappersForPrimitives;
using Zpp.Common.DemandDomain;
using Zpp.DbCache;

namespace Zpp.Mrp
{
    /**
     * Central interface for the mainModules
     */
    public interface IProvidingManager
    {
        EntityCollector Satisfy(Demand demand, Quantity demandedQuantity);
    }
}
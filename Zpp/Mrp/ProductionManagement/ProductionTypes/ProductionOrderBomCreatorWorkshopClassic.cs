using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Common.DemandDomain.Wrappers;
using Zpp.Common.DemandDomain.WrappersForCollections;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.DbCache;
using Zpp.Utils;

namespace Zpp.Mrp.ProductionManagement.ProductionTypes
{
    /**
     * (articleBom.Quantity) ProductionOrderBoms will be created
     * ProductionOrderOperation.Duration == articleBom.Duration * quantity
     */
    public class ProductionOrderBomCreatorWorkshopClassic
    : IProductionOrderBomCreator
    {
        private readonly Dictionary<M_Operation, ProductionOrderOperation>
            _alreadyCreatedProductionOrderOperations =
                new Dictionary<M_Operation, ProductionOrderOperation>();

        public ProductionOrderBomCreatorWorkshopClassic()
        {
            if (Configuration.Configuration.ProductionType.Equals(ProductionType.WorkshopProductionClassic) == false)
            {
                throw new MrpRunException("This is class is intended for productionType AssemblyLine.");
            }
        }

        public Demands CreateProductionOrderBomsForArticleBom(IDbMasterDataCache dbMasterDataCache,
            IDbTransactionData dbTransactionData, M_ArticleBom articleBom, Quantity quantity,
            ProductionOrder parentProductionOrder)
        {
            
            Demands newProductionOrderBoms = new Demands();
            ProductionOrderOperation productionOrderOperation = null;
            if (articleBom.OperationId == null)
            {
                throw new MrpRunException(
                    "Every PrOBom must have an operation. Add an operation to the articleBom.");
            }

            // load articleBom.Operation
            if (articleBom.Operation == null)
            {
                articleBom.Operation =
                    dbMasterDataCache.M_OperationGetById(
                        new Id(articleBom.OperationId.GetValueOrDefault()));
            }

            // don't create an productionOrderOperation twice, take existing
            if (_alreadyCreatedProductionOrderOperations.ContainsKey(articleBom.Operation))
            {

                    productionOrderOperation =
                        _alreadyCreatedProductionOrderOperations[articleBom.Operation];

            }

            ProductionOrderBom newProductionOrderBom =
                ProductionOrderBom.CreateTProductionOrderBom(articleBom, parentProductionOrder,
                    dbMasterDataCache, productionOrderOperation, quantity);

            if (newProductionOrderBom.HasOperation() == false)
            {
                throw new MrpRunException(
                    "Every PrOBom must have an operation. Add an operation to the articleBom.");
            }

            if (_alreadyCreatedProductionOrderOperations.ContainsKey(articleBom.Operation) == false)
            {
                _alreadyCreatedProductionOrderOperations.Add(articleBom.Operation,
                    newProductionOrderBom.GetProductionOrderOperation(dbTransactionData));
            }

            newProductionOrderBoms.Add(newProductionOrderBom);


            return newProductionOrderBoms;
        }
        
    }
}
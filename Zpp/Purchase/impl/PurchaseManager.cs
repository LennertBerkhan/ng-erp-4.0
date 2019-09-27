using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Master40.DB.Enums;
using Zpp.Common.DemandDomain;
using Zpp.Common.ProviderDomain;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.Configuration;
using Zpp.DbCache;
using Zpp.Mrp.NodeManagement;
using Zpp.Utils;
using Zpp.WrappersForPrimitives;

namespace Zpp.Mrp.PurchaseManagement
{
    public class PurchaseManager : ProviderManager
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IDbMasterDataCache _dbMasterDataCache = ZppConfiguration.CacheManager.GetMasterDataCache();

        private readonly ICacheManager _cacheManager =
            ZppConfiguration.CacheManager;
        
        public PurchaseManager()
        {
            
        }

        public EntityCollector Satisfy(Demand demand, Quantity demandedQuantity)
        {
            EntityCollector entityCollector = new EntityCollector();
            M_Article article = demand.GetArticle();
            DueTime dueTime = demand.GetDueTime();
            if (article.ToBuild)
            {
                throw new MrpRunException(
                    "You try to create a purchaseOrderPart for a articleToBuild.");
            }

            // currently only one businessPartner per article TODO: This could be changing
            M_ArticleToBusinessPartner articleToBusinessPartner =
                _dbMasterDataCache.M_ArticleToBusinessPartnerGetAllByArticleId(article.GetId())[0];
            M_BusinessPartner businessPartner =
                _dbMasterDataCache.M_BusinessPartnerGetById(new Id(articleToBusinessPartner
                    .BusinessPartnerId));
            T_PurchaseOrder purchaseOrder = new T_PurchaseOrder();
            // [Name],[DueTime],[BusinessPartnerId]
            purchaseOrder.DueTime = dueTime.GetValue();
            purchaseOrder.BusinessPartner = businessPartner;
            purchaseOrder.Name = $"PurchaseOrder{article.Name} for " +
                                 $"businessPartner {purchaseOrder.BusinessPartner.Id}";

            // demand cannot be fulfilled in time
            if (articleToBusinessPartner.TimeToDelivery > dueTime.GetValue())
            {
                Logger.Error($"Article {article.GetId()} from demand {demand.GetId()} " +
                             $"should be available at {dueTime}, but " +
                             $"businessPartner {businessPartner.Id} " +
                             $"can only deliver at {articleToBusinessPartner.TimeToDelivery}.");
            }

            // init a new purchaseOderPart
            T_PurchaseOrderPart tPurchaseOrderPart = new T_PurchaseOrderPart();

            // [PurchaseOrderId],[ArticleId],[Quantity],[State],[ProviderId]
            tPurchaseOrderPart.PurchaseOrder = purchaseOrder;
            tPurchaseOrderPart.Article = article;
            tPurchaseOrderPart.ArticleId = article.Id;
            tPurchaseOrderPart.Quantity =
                CalculateQuantity(articleToBusinessPartner, demandedQuantity) *
                articleToBusinessPartner
                    .PackSize;
            if (tPurchaseOrderPart.Quantity < demandedQuantity.GetValue())
            {
                throw new MrpRunException("You cannot purchase less than you need!");
            }

            tPurchaseOrderPart.State = State.Created;

            Logger.Debug("PurchaseOrderPart created.");
            PurchaseOrderPart purchaseOrderPart =
                new PurchaseOrderPart(tPurchaseOrderPart, null);
            
            T_DemandToProvider demandToProvider = new T_DemandToProvider()
            {
                DemandId = demand.GetId().GetValue(),
                ProviderId = purchaseOrderPart.GetId().GetValue(),
                Quantity = demandedQuantity.GetValue()
            };
            entityCollector.Add(purchaseOrderPart);
            entityCollector.Add(demandToProvider);
            return entityCollector;
        }
        
        private static int CalculateQuantity(M_ArticleToBusinessPartner articleToBusinessPartner,
            Quantity demandQuantity)
        {
            // force round up the decimal demandQuantity
            int demandQuantityInt = (int) decimal.Truncate(demandQuantity.GetValue());
            if (demandQuantityInt < demandQuantity.GetValue())
            {
                demandQuantityInt++;
            }
            int purchaseQuantity = 0;
            
            for (int quantity = 0;
                quantity < demandQuantityInt;
                quantity += articleToBusinessPartner.PackSize)
            {
                purchaseQuantity++;
            }

            return purchaseQuantity;
        }

        public EntityCollector CreateDependingDemands(IOpenDemandManager openDemandManager, Provider provider)
        {
            // NOT needed, since PurchaseOrderPart has never dependingDemands
            throw new System.NotImplementedException();
        }
    }
}
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;

namespace Zpp.DataLayer.impl.OpenDemand
{
    /**
     * Represents an node, which quantity is not yet exceeded and can satisfy more demands/providers.
     */
    public class OpenNode<T>: IId where T: IId
    {
        private readonly T _openNode;
        private readonly Quantity _openQuantity;
        private readonly M_Article _article;

        public OpenNode(T openNode, Quantity openQuantity, M_Article article)
        {
            _openNode = openNode;
            _openQuantity = openQuantity;
            _article = article;
        }

        public T GetOpenNode()
        {
            return _openNode;
        }

        public Quantity GetOpenQuantity()
        {
            return _openQuantity;
        }

        public M_Article GetArticle()
        {
            return _article;
        }

        public Id GetId()
        {
            return _openNode.GetId();
        }

        public string AsString()
        {
            return $"{_openQuantity} open of '{_openNode}'";
        }
    }
}
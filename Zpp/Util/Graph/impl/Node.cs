using Master40.DB.Data.WrappersForPrimitives;

namespace Zpp.Util.Graph.impl
{
    /**
     * Just a wrapper around an entity inheriting INode (but NOT "Node" (this class)),
     * do not try to use it without an real entity.
     */
    public class Node : INode
    {
        private Id _id;
        private IScheduleNode _entity;

        /**
         * put one of the other classes inheriting INode in it like ProductionOrder...
         */
        public Node(IScheduleNode entity)
        {
            _entity = entity;
            _id = entity.GetId();
        }

        public Id GetId()
        {
            return _id;
        }

        public NodeType GetNodeType()
        {
            return _entity.GetNodeType();
        }

        public IScheduleNode GetEntity()
        {
            return _entity;
        }

        public override bool Equals(object obj)
        {
            INode otherObject = (INode) obj;
            // return _id.Equals(otherObject.GetId()) && _entity.GetNodeType().Equals(otherObject.GetNodeType());
            return _id.Equals(otherObject.GetId());
        }

        public override int GetHashCode()
        {
            // return HashCode.Combine(_id.GetHashCode(), _entity.GetEntity().GetNodeType().GetHashCode());
            return _id.GetHashCode();
        }

        public override string ToString()
        {
            return $"{_entity.ToString()}";
        }
    }
}
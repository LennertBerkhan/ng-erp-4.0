using System;
using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Zpp.DataLayer;
using Zpp.DataLayer.impl.DemandDomain;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.DataLayer.impl.ProviderDomain;
using Zpp.Util.StackSet;

namespace Zpp.Util.Graph.impl
{
    public class DemandToProviderGraph : DirectedGraph
    {
        public DemandToProviderGraph() : base()
        {
            IDbTransactionData dbTransactionData =
                ZppConfiguration.CacheManager.GetDbTransactionData();

            CreateGraph(dbTransactionData);
            if (IsEmpty())
            {
                return;
            }
            
        }

        private void CreateGraph(IDbTransactionData dbTransactionData)
        {
            foreach (var demandToProvider in dbTransactionData.DemandToProviderGetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(new Id(demandToProvider.DemandId));
                Provider provider =
                    dbTransactionData.ProvidersGetById(new Id(demandToProvider.ProviderId));
                if (demand == null || provider == null)
                {
                    throw new MrpRunException("Demand/Provider should not be null.");
                }

                INode fromNode = new Node(demand);
                INode toNode = new Node(provider);
                AddEdge(new Edge(demandToProvider, fromNode, toNode));
            }

            foreach (var providerToDemand in dbTransactionData.ProviderToDemandGetAll())
            {
                Demand demand = dbTransactionData.DemandsGetById(providerToDemand.GetDemandId());
                Provider provider =
                    dbTransactionData.ProvidersGetById(providerToDemand.GetProviderId());
                if (demand == null || provider == null)
                {
                    throw new MrpRunException("Demand/Provider should not be null.");
                }

                INode fromNode = new Node(provider);
                INode toNode = new Node(demand);
                AddEdge(new Edge(providerToDemand, fromNode, toNode));
            }
        }
        
        

        /**
         * overriden, because we need the quantity on the arrows
         */
        public override string ToString()
        {
            string mystring = "";
            IStackSet<IEdge> edges = GetEdges();

            if (edges == null)
            {
                return mystring;
            }

            foreach (var edge in edges)
            {
                // foreach (var edge in GetAllEdgesFromTailNode(fromNode))
                // {
                // <Type>, <Menge>, <ItemName> and on edges: <Menge>
                Quantity quantity = null;
                if (edge.GetLinkDemandAndProvider() != null)
                {
                    quantity = edge.GetLinkDemandAndProvider().GetQuantity();
                }

                string tailsGraphvizString =
                    Graphviz.GetGraphizString(edge.GetTailNode().GetEntity());
                string headsGraphvizString =
                    Graphviz.GetGraphizString(edge.GetHeadNode().GetEntity());
                mystring += $"\"{tailsGraphvizString}\" -> " + $"\"{headsGraphvizString}\"";
                // if (quantity.IsNull() == false)
                if (quantity != null && quantity.IsNull() == false)
                {
                    mystring += $" [ label=\" {quantity}\" ]";
                }

                mystring += ";" + Environment.NewLine;
                // }
            }

            return mystring;
        }
    }
}
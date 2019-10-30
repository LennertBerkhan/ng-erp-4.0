using System;
using System.Collections.Generic;
using System.Linq;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.Interfaces;
using Zpp.DataLayer.impl.DemandDomain.Wrappers;
using Zpp.GraphicalRepresentation;
using Zpp.GraphicalRepresentation.impl;
using Zpp.Util.StackSet;

namespace Zpp.Util.Graph.impl
{
    /**
     * An impl for a directed graph. It's important to always return null if aggregation is empty
     * (simplify error detecting, since no empty collections should pass through the program).
     * A directed graph is stored/read in by its edges, from that edges a list of nodes is built up
     * where every node has:
     * - a list of successors
     * - a list of predecessors
     * --> so that O(1) can be realized for most operations
     */
    public class DirectedGraph : IDirectedGraph<INode>
    {
        protected IStackSet<INode> Nodes = new StackSet<INode>();
        private int _edgeCount = 0;
        
        
        protected readonly IGraphviz Graphviz = new Graphviz();

        public DirectedGraph()
        {
        }
        
        public DirectedGraph(List<IEdge> edges)
        {
            AddEdges(edges);
        }

        public INodes GetSuccessorNodes(INode node)
        {
            INodes successors = node.GetSuccessors();

            if (successors.Any() == false)
            {
                return null;
            }

            return successors;
        }

        public INodes GetPredecessorNodes(INode node)
        {
            INodes predecessors = node.GetPredecessors();

            if (predecessors.Any() == false)
            {
                return null;
            }

            return predecessors;
        }

        public void AddEdges(IEnumerable<IEdge> edges)
        {
            foreach (var edge in edges)
            {
                AddEdge(edge);
            }
        }

        public void AddEdges(INode fromNode, INodes nodes)
        {
            foreach (var toNode in nodes)
            {
                AddEdge(new Edge(fromNode, toNode));
            }
        }

        public void AddEdge(IEdge edge)
        {
            INode tail = edge.TailNode;
            INode head = edge.HeadNode;
            if (Nodes.Contains(tail) == false)
            {
                Nodes.Push(tail);
            }
            tail.AddSuccessor(head);
                
            if (Nodes.Contains(head) == false)
            {
                Nodes.Push(head);
            }
            head.AddPredecessor(tail);

            _edgeCount++;
        }

        public int CountEdges()
        {
            return _edgeCount;
        }

        public override string ToString()
        {
            string mystring = "";
            List<IEdge> edges = GetAllEdges();

            if (edges == null)
            {
                return mystring;
            }

            foreach (var edge in edges)
            {
                string tailsGraphvizString =
                    Graphviz.GetGraphizString(edge.GetTailNode().GetEntity());
                string headsGraphvizString =
                    Graphviz.GetGraphizString(edge.GetHeadNode().GetEntity());
                mystring += $"\"{tailsGraphvizString}\" -> " + $"\"{headsGraphvizString}\"";

                mystring += ";" + Environment.NewLine;
                // }
            }

            return mystring;
        }

        /// 
        ///
        /// <summary>
        ///     A depth-first-search (DFS) traversal of given tree
        /// </summary>
        /// <returns>
        ///    The List of the traversed nodes in exact order
        /// </returns>
        public INodes TraverseDepthFirst(Action<INode, INodes, INodes> action,
            CustomerOrderPart startNode)
        {
            var stack = new Stack<INode>();

            Dictionary<INode, bool> discovered = new Dictionary<INode, bool>();
            INodes traversed = new Nodes();

            stack.Push(new Node(startNode));
            INode parentNode;

            while (stack.Any())
            {
                INode poppedNode = stack.Pop();

                // init dict if node not yet exists
                if (!discovered.ContainsKey(poppedNode))
                {
                    discovered[poppedNode] = false;
                }

                // if node is not discovered
                if (!discovered[poppedNode])
                {
                    traversed.Add(poppedNode);
                    discovered[poppedNode] = true;
                    INodes childNodes = GetSuccessorNodes(poppedNode);
                    action(poppedNode, childNodes, traversed);

                    if (childNodes != null)
                    {
                        foreach (INode node in childNodes)
                        {
                            stack.Push(node);
                        }
                    }
                }
            }

            return traversed;
        }

        public IStackSet<INode> GetAllUniqueNodes()
        {
            IStackSet<INode> uniqueNodes = new StackSet<INode>();
            uniqueNodes.PushAll(Nodes);

            if (uniqueNodes.Any() == false)
            {
                return null;
            }

            return uniqueNodes;
        }

        public void RemoveNode(INode node)
        {
            // e.g. A -> B --> C, B is removed
            
            // holds A
            INodes predecessors = node.GetPredecessors();
            // holds C
            INodes successors = node.GetSuccessors();
            
            
            foreach (var predecessor in predecessors)
            {
                // predecessor is A
                
                // remove edge A -> B
                predecessor.RemoveSuccessor(node);
                // add edge A -> C
                predecessor.AddSuccessors(successors);
            }
            foreach (var successor in successors)
            {
                // successor is C
                
                // remove edge B -> C
                successor.RemovePredecessor(node);
                // add edge A -> C
                successor.AddPredecessors(predecessors);
            }
            // remove node
            Nodes.Remove(node);
        }

        public INodes GetLeafNodes()
        {
            List<INode> leafs = new List<INode>();
            
            foreach (var node in Nodes)
            {
                INodes successors = GetSuccessorNodes(node);
                if (successors == null || successors.Any() == false)
                {
                    leafs.Add(node);
                }
            }

            if (leafs.Any() == false)
            {
                return null;
            }

            return new Nodes(leafs);
        }

        public bool IsEmpty()
        {
            return Nodes == null || Nodes.Any() == false;
        }

        public INodes GetRootNodes()
        {
            INodes roots = new Nodes();
            
            foreach (var node in Nodes)
            {
                INodes predecessors = GetPredecessorNodes(node);
                if (predecessors == null)
                {
                    roots.Add(node);
                }
            }

            if (roots.Any() == false)
            {
                return null;
            }

            return roots;
        }

        public void ReplaceNodeByDirectedGraph(INode node, IDirectedGraph<INode> graphToInsert)
        {
            INodes predecessors = GetPredecessorNodes(node);
            INodes successors = GetSuccessorNodes(node);
            RemoveNode(node);
            // predecessors --> roots
            if (predecessors != null)
            {
                foreach (var predecessor in predecessors)
                {
                    foreach (var rootNode in graphToInsert.GetRootNodes())
                    {
                        AddEdge(new Edge(predecessor, rootNode));
                    }
                }
            }

            // leafs --> successors 
            if (successors != null)
            {
                foreach (var leaf in graphToInsert.GetLeafNodes())
                {
                    foreach (var successor in successors)
                    {
                        AddEdge(new Edge(leaf, successor));
                    }
                }
            }

            // add all edges from graphToInsert
            AddEdges(graphToInsert.GetEdges().GetAll());
        }

        public INodes GetNodes()
        {
            INodes nodes = new Nodes();
            nodes.AddAll(Nodes);
            return nodes;
        }

        public List<IEdge> GetAllEdges()
        {
            return GetEdges().ToList();
        }

        public IStackSet<IEdge> GetEdges()
        {
            IStackSet<IEdge> edges = new StackSet<IEdge>();

            // one is enough either all successors or all predecessors
            foreach (var node in Nodes)
            {
                foreach (var successor in node.GetSuccessors())
                {
                    edges.Push(new Edge(node, successor));
                }
            }
            if (edges.Any() == false)
            {
                return null;
            }
            return edges;
        }

        public void Clear()
        {
            Nodes.Clear();
        }

        public void RemoveTopDown(INode node)
        {
            INodes successors = GetSuccessorNodes(node);
            RemoveNode(node);
            if (successors == null)
            {
                return;
            }

            foreach (var successor in successors)
            {
                RemoveTopDown(successor);
            }
        }

        public void AddNodes(INodes nodes)
        {
            foreach (var node in nodes)
            {
                AddNode(node);
            }
        }

        public void AddNode(INode node)
        {
            Nodes.Push(node);
        }
    }
}
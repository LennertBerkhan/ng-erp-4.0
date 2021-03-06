﻿using Akka.Actor;
using AkkaSim.Definitions;
using Zpp.Common.ProviderDomain.Wrappers;

namespace Master40.SimulationMrp.Simulation.Agents.Resource.Skills
{
    public class FinishWork : SimulationMessage
    {
        public static FinishWork Create(ProductionOrderOperation operation, IActorRef target)
        {
            return new FinishWork(operation, target);
        }
        private FinishWork(object Message, IActorRef target) : base(Message, target)
        { }
        public ProductionOrderOperation GetOperation => this.Message as ProductionOrderOperation;
    }
}

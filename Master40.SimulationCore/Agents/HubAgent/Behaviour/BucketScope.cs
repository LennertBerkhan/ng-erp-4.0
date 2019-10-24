﻿using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Master40.DB.Enums;
using Master40.SimulationCore.Agents.HubAgent.Types;
using Master40.SimulationCore.Agents.ResourceAgent;
using static FBuckets;
using static FOperations;
using static IJobs;
using static FUpdateStartConditions;
using static FProposals;
using static IJobResults;

namespace Master40.SimulationCore.Agents.HubAgent.Behaviour
{
    public class BucketScope : DefaultSetup
    {
        public BucketScope(SimulationType simulationType = SimulationType.BucketScope)
            : base(simulationType: simulationType) { }

        private BucketManager _bucketManager { get; } = new BucketManager();

        public override bool Action(object message)
        {
            var success = true;
            switch (message)
            {
                case Hub.Instruction.Default.EnqueueJob msg: AssignJob(msg.GetObjectFromMessage); break;
                case Hub.Instruction.BucketScope.EnqueueOperation msg: EnqueueOperation(msg.GetObjectFromMessage); break;
                case Hub.Instruction.BucketScope.EnqueueBucket msg: EnqueueBucket(msg.GetObjectFromMessage); break;
                case Hub.Instruction.BucketScope.ResetBucket msg: ResetBucket(msg.GetObjectFromMessage); break;
                case Hub.Instruction.BucketScope.SetBucketFix msg: SetBucketFix(msg.GetObjectFromMessage); break;
                case BasicInstruction.WithdrawRequiredArticles msg: WithdrawRequiredArticles(operationKey: msg.GetObjectFromMessage); break;
                case BasicInstruction.FinishJob msg: FinishJob(msg.GetObjectFromMessage); break;
                default:
                    success = base.Action(message);
                    break;
            }
            return success;
        }

        private void ResetBucket(Guid bucketKey)
        {
            var bucket = _bucketManager.GetBucketById(bucketKey);

            var successRemove =  _bucketManager.Remove(bucket);

            if (successRemove)
            {
                RequeueOperations(bucket.Operations.OrderBy(x => x.ForwardStart).ToList());
            }

        }

        private void AssignJob(IJob job)
        {
            var operation = (FOperation)job;

            System.Diagnostics.Debug.WriteLine($"Enqueue Operation {operation.Operation.Name} {operation.Key} ");
            Agent.DebugMessage(msg: $"Got New Item to Enqueue: {operation.Operation.Name} | with start condition: {operation.StartConditions.Satisfied} with Id: {operation.Key}");

            operation.UpdateHubAgent(hub: Agent.Context.Self);

            EnqueueOperation(operation);

        }

        internal void EnqueueOperation(FOperation operation)
        {
            var bucketsToModify = _bucketManager.FindAllBucketsLaterForwardStart(operation);

            if (bucketsToModify != null)
            {
                foreach (var modBucket in bucketsToModify)
                {
                    if (!modBucket.ResourceAgent.IsNobody())
                    {
                        Agent.Send(Resource.Instruction.BucketScope.RequeueBucket.Create(modBucket.Key, modBucket.ResourceAgent));
                    }
                    else
                    {
                        RequeueOperations(modBucket.Operations.ToList());
                    }
                    
                }

            }

            //if no bucket has to be modified try to add
            var bucket = _bucketManager.AddToBucket(operation, Agent.Context.Self, Agent.CurrentTime);

            if (bucket != null)
            {

                System.Diagnostics.Debug.WriteLine($"Add {operation.Operation.Name} to {bucket.Name}");
                if (!bucket.ResourceAgent.IsNobody())
                {
                    Agent.Send(instruction: Resource.Instruction.BucketScope.UpdateBucket.Create(bucket, bucket.ResourceAgent));
                }
                return;
            }

            //if no bucket to add exists create a new one
            bucket = _bucketManager.CreateBucket(fOperation: operation, Agent.Context.Self, Agent.CurrentTime);
            System.Diagnostics.Debug.WriteLine($"Create {bucket.Name} with scope of {bucket.Scope} from {bucket.ForwardStart} to {bucket.BackwardStart}");
            EnqueueBucket(bucket);

        }

        internal void EnqueueBucket(FBucket bucket)
        {
            var bucketExits = _bucketManager.GetBucketById(bucket.Key);

            if (bucketExits != null)
            {
                bucket = bucketExits;
            }

            System.Diagnostics.Debug.WriteLine($"Enqueue {bucket.Name} with {bucket.Operations.Count} operations");
            foreach (var operation in bucket.Operations)
            {
                System.Diagnostics.Debug.WriteLine($"{bucket.Name} has operation {operation.Operation.Name} {operation.Key}");
            }
            var resourceToRequest = _resourceManager.GetResourceByTool(bucket.Tool);

            foreach (var actorRef in resourceToRequest)
            {
                Agent.DebugMessage(msg: $"Ask for proposal at resource {actorRef.Path.Name}");
                Agent.Send(instruction: Resource.Instruction.Default.RequestProposal.Create(message: bucket, target: actorRef));
            }

        }

        internal override void ProposalFromResource(FProposal fProposal)
        {
            // get related operation and add proposal.
            var bucket = _bucketManager.GetBucketById(fProposal.JobKey);
            bucket.Proposals.RemoveAll(x => x.ResourceAgent.Equals(fProposal.ResourceAgent));
            // add New Proposal
            bucket.Proposals.Add(item: fProposal);

            Agent.DebugMessage(msg: $"Proposal for {bucket.Name} with Schedule: {fProposal.PossibleSchedule} Id: {fProposal.JobKey} from: {fProposal.ResourceAgent}!");

            // if all Machines Answered
            if (bucket.Proposals.Count == _resourceManager.GetResourceByTool(bucket.Tool).Count)
            {

                // item Postponed by All Machines ? -> requeue after given amount of time.
                if (bucket.Proposals.TrueForAll(match: x => x.Postponed.IsPostponed))
                {
                    var postPonedFor = bucket.Proposals.Min(x => x.Postponed.Offset);
                    Agent.DebugMessage(msg: $"{bucket.Name} {bucket.Key} postponed to {postPonedFor}");
                    // Call Hub Agent to Requeue
                    bucket = bucket.UpdateResourceAgent(r: ActorRefs.NoSender);
                    _bucketManager.Replace(bucket);
                    Agent.Send(instruction: Hub.Instruction.BucketScope.EnqueueBucket.Create(bucket: bucket, target: Agent.Context.Self), waitFor: postPonedFor);
                    return;
                }

                // acknowledge Machine -> therefore get Machine -> send acknowledgement
                var earliestPossibleStart = bucket.Proposals.Where(predicate: y => y.Postponed.IsPostponed == false)
                                                               .Min(selector: p => p.PossibleSchedule);

                var acknowledgement = bucket.Proposals.First(predicate: x => x.PossibleSchedule == earliestPossibleStart
                                                                        && x.Postponed.IsPostponed == false);

                bucket = ((IJob)bucket).UpdateEstimations(acknowledgement.PossibleSchedule, acknowledgement.ResourceAgent) as FBucket;

                Agent.DebugMessage(msg: $"Start AcknowledgeProposal for {bucket.Name} {bucket.Key} on resource {acknowledgement.ResourceAgent}");

                // set Proposal Start for Machine to Requeue if time slot is closed.
                _bucketManager.Replace(bucket);
                Agent.Send(instruction: Resource.Instruction.Default.AcknowledgeProposal.Create(message: bucket, target: acknowledgement.ResourceAgent));
            }
        }



        /// <summary>
        /// Source: ResourceAgent 
        /// </summary>
        /// <param name="bucketKey"></param>
        internal void SetBucketFix(Guid bucketKey)
        {
            var bucket = _bucketManager.SetBucketFix(bucketKey);
            var notSatisfiedOperations = _bucketManager.RemoveAllNotSatisfiedOperations(bucket);
            bucket = _bucketManager.GetBucketById(bucketKey);
            bucket = _bucketManager.SetBucketSatisfied(bucket);

            Agent.DebugMessage(msg: $"{bucket.Name} has been set fix");
            //Send fix bucket
            Agent.Send(Resource.Instruction.BucketScope.AcknowledgeJob.Create(bucket, bucket.ResourceAgent));

            //Requeue all unsatisfied operations
            Agent.DebugMessage(msg: $"{bucket.Name} has {notSatisfiedOperations.Count} operations to requeue");
            RequeueOperations(notSatisfiedOperations);
        }

        internal override void UpdateAndForwardStartConditions(FUpdateStartCondition startCondition)
        {
            var bucket = _bucketManager.SetOperationStartCondition(startCondition.OperationKey, startCondition);
            
            if (bucket.ResourceAgent.IsNobody())
                return;

            if (bucket.Operations.Any(x => x.StartConditions.Satisfied))
            {

                Agent.DebugMessage(msg: $"Update and forward start condition: {startCondition.OperationKey} in {bucket.Name}" +
                                    $"| ArticleProvided: {startCondition.ArticlesProvided} " +
                                    $"| PreCondition: {startCondition.PreCondition} " +
                                    $"to resource {bucket.ResourceAgent}");

                Agent.Send(instruction: Resource.Instruction.BucketScope.UpdateBucket.Create(message: bucket, target: bucket.ResourceAgent));
            }
        }

        internal override void WithdrawRequiredArticles(Guid operationKey)
        {
            var operation = _bucketManager.GetOperationByKey(operationKey);
            System.Diagnostics.Debug.WriteLine($"WithdrawRequiredArticles for operation {operation.Operation.Name} {operationKey} on {Agent.Context.Self.Path.Name}");
                Agent.Send(instruction: BasicInstruction.WithdrawRequiredArticles
                .Create(message: operation.Key
                    , target: operation.ProductionAgent));

        }

        internal void RequeueOperations(List<FOperation> operations)
        {
            foreach (var operation in operations.OrderBy(x => x.ForwardStart).ToList())
            {
                Agent.DebugMessage(msg: $"Requeue operation {operation.Operation.Name} {operation.Key}");
                EnqueueOperation(operation);
            }

        }

        /// <summary>
        /// Job = Bucket
        /// </summary>
        /// <param name="jobResult"></param>
        internal override void FinishJob(IJobResult jobResult)
        {
            var bucket = _bucketManager.GetBucketById(jobResult.Key);
            System.Diagnostics.Debug.WriteLine($"Resource called Item  {bucket.Name}  {jobResult.Key} finished");
            Agent.DebugMessage(msg: $"Resource called Item {bucket.Name} {jobResult.Key} finished.");

            _bucketManager.Remove(bucket);

        }

    }
}

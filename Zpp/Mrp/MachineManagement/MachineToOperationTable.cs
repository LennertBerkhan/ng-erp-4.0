using System.Collections.Generic;
using Zpp.Common.ProviderDomain.Wrappers;
using Zpp.DbCache;

namespace Zpp.Mrp.MachineManagement
{
    public class MachineToOperationTable
    {
        private IDbMasterDataCache _dbMasterDataCache;
        private IDbTransactionData _dbTransactionData;
        private Dictionary<Resource, StackSet<ProductionOrderOperation>> _machineToOperations =
            new Dictionary<Resource, StackSet<ProductionOrderOperation>>();

        public MachineToOperationTable(IDbMasterDataCache dbMasterDataCache, IDbTransactionData dbTransactionData)
        {
            _dbTransactionData = dbTransactionData;
            _dbMasterDataCache = dbMasterDataCache;
            Init();
        }

        public List<ProductionOrderOperation> GetOperationsOfMachine(Resource resource)
        {
            return _machineToOperations[resource].GetAll();
        }

        private void Init()
        {
            List<ProductionOrderOperation> productionOrderOperations = _dbTransactionData.ProductionOrderOperationGetAll();

            foreach (var productionOrderOperation in productionOrderOperations)
            {
                foreach (var machine in productionOrderOperation.GetMachines(_dbTransactionData))
                {
                    if (_machineToOperations.ContainsKey(machine) == false)
                    {
                        _machineToOperations.Add(machine, new StackSet<ProductionOrderOperation>());
                    }
                    _machineToOperations[machine].Push(productionOrderOperation);
                }
            }
        }
    }
}
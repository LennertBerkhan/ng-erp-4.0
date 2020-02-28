using System.Collections.Generic;
using Newtonsoft.Json;

namespace Master40.DB.DataModel
{
    public class M_Resource : BaseEntity
    {
        // TODO: why is this required ?
        // public int ResourceId { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        /*
         * Defines a list of Skills that can be 
         */
        public virtual ICollection<M_ResourceSetup> ResourceSetups { get; set; }
        public virtual ICollection<M_ResourceSkill> ResourceSkills { get; set; }

        public int Capacity { get; set; }
        [JsonIgnore]
        private ICollection<T_ProductionOrderOperation> productionOrderOperations { get; set; }
        public virtual ICollection<T_ProductionOrderOperation> ProductionOrderOperations
        {
            get
            {
                return productionOrderOperations;
            }
            set
            {
                productionOrderOperations = value;
            }
        }

        private string _s;
        public string YourProperty
        {
            get { return _s; }
            set { _s = value; }
        }
        public override string ToString()
        {
            return Name;
        }
    }
}

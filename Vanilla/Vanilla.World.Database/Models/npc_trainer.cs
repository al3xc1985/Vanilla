using System;
using System.Collections.Generic;

using System.ComponentModel.DataAnnotations.Schema;

namespace Vanilla.Database.World.Models
{

	    [Table("npc_trainer", Schema="mangos")]

    public partial class npc_trainer
    {
 
        [Column("entry")] 
		        public int entry { get; set; }
 
        [Column("spell")] 
		        public int spell { get; set; }
 
        [Column("spellcost")] 
		        public long spellcost { get; set; }
 
        [Column("reqskill")] 
		        public int reqskill { get; set; }
 
        [Column("reqskillvalue")] 
		        public int reqskillvalue { get; set; }
 
        [Column("reqlevel")] 
		        public byte reqlevel { get; set; }
    }
}

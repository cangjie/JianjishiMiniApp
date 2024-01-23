using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models
{
    [Table("mini_user")]
    public class MiniUser
    {
        public MiniUser()
        {

        }

        [Key]
        public int id { get; set; } = 0;
        public string original_id { get; set; } = "";
        public string open_id { get; set; } = "";



        public string? union_id { get; set; } 
        public string? nick { get; set; }
        public string? avatar { get; set; }
        public string? cell_number { get; set; }
        public string? gender { get; set; }
        public string? city { get; set; }
        public string? province { get; set; }
        public string? country { get; set; }
        public string? language { get; set; }
        public string real_name { get; set; } = "";
        public int staff { get; set; } = 0;

        [NotMapped]
        public int user_id { get; set; } = 0;
    }
}

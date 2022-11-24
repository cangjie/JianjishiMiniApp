using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniApp.Models
{
    [Table("mini_session")]
    public class MiniSession
    {
        public string original_id { get; set; } = "";
        public string open_id { get; set; } = "";
        public string session_key { get; set; } = "";
        public string union_id { get; set; } = "";
    }
}

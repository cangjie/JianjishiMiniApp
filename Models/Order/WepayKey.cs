using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniApp.Models.Order
{
    [Table("wepay_key")]
    public class WepayKey
    {
        public int id { get; set; }
        public string mch_id { get; set; }
        public string mch_name { get; set; }
        public string key_serial { get; set; }
        public string private_key { get; set; }
        public string api_key { get; set; }
    }
}


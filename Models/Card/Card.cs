using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models.Card
{
    [Table("card")]
	public class Card
	{
        public int id {get; set;}
        public string open_id { get; set; } = "";
        public int? order_id { get; set; }
        public int avaliable { get; set; } = 0;
        public int product_id {get; set;}
        public DateTime? start_date { get; set; }
        public DateTime? end_date {get; set;}
        public int? total_times {get; set;}
        public double? total_amount {get; set;}
        public int? used_times { get; set; }
        public double? used_amount {get; set;}
        public DateTime update_date {get; set;}
        public string title { get; set; } = "";
        public string desc { get; set; } = "";

    }
}


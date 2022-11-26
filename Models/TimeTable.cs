using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models
{
    [Table("shop_time_table")]
    public class TimeTable
	{
		[Key]
		public int id { get; set; }
		public int shop_id { get; set; }
		public string shop_name { get; set; } = "";
		public string description { get; set; } = "";
		public int count { get; set; }
		public int in_use { get; set; }

		[NotMapped]
		public int avaliableCount { get; set; } = 0;
	}
}


using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models
{
	[Table("reserve")]
	public class Reserve
	{
		[Key]
		public int id { get; set; }
		public string open_id { get; set; } = "";
		public DateTime reserve_date { get; set; }
		public int time_table_id { get; set; }
		public string time_table_description { get; set; } = "";
		public int cancel { get; set; } = 0;
		public int therapeutist_time_id {get; set;} = 0;

		[NotMapped]
		public string shop_name { get; set; } = "";
		[NotMapped]
		public bool valid {get; set;} = true;


	}
}


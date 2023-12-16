using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models
{
	[Table("shop_list")]
	public class Shop
	{
		[Key]
		public int id { get; set; }
		public string name { get; set; } = "";
		public int sort { get; set; }
		public string close_dates { get; set; } = "";
		public string address { get; set; } = "";
		public int hidden { get; set; } = 0;
		public string region {get; set;} = "";
		public double lat_from { get; set; } = 0;
		public double lat_to { get; set; } = 0;
		public double long_from { get; set; } = 0;
		public double long_to { get; set; } = 0;
	}
}


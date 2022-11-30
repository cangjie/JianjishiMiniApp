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

	}
}


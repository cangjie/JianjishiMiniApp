using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models
{
	[Table("shop_list")]
	public class Shop
	{
		public int id { get; set; }
		public string name { get; set; } = "";
		public int sort { get; set; }
		
	}
}


using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models
{
	[Table("users")]
	public class User
	{
		public int id { get; set; }
		public string oa_union_id { get; set; }
		public int is_admin { get; set; }
	}
}


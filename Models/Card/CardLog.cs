using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models.Card
{
	public class CardLog
	{
		public int id { get; set; }
		public int card_id { get; set; }
		public string open_id { get; set; } = "";
		public int? times { get; set; }
		public double? amount { get; set; }
		public DateTime use_date { get; set; } = DateTime.Now;
		public string? staff_open_id { get; set; }

	}
}


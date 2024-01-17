using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models.Card
{
	[Table("card_product_associate")]
	public class CardProductAssociate
	{
		[Key]
		public int common_product_id { get; set; }
		public int card_product_id { get; set; }

    }
}


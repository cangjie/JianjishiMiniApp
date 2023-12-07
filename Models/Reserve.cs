using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MiniApp.Models.Order;
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
		public string cancel_memo {get; set;} = "";
		public int therapeutist_time_id {get; set;} = 0;
		public string therapeutist_name {get; set;} = "";
		public int product_id {get; set;} = 0;
		
		public string product_name {get; set;} = "";
		public int order_id {get; set;}

		[NotMapped]
		public DateTime? startTime
		{
			get
			{
				try
				{
					return DateTime.Parse(time_table_description.Split('-')[0].Trim());
				}
				catch
				{
					return null;
				}
			}
		}

		[NotMapped]
		public MiniUser? reserveUser { get; set; } = null;

		[NotMapped]
		public OrderOnline? order {get; set;} = null;
		public string shop_name { get; set; } = "";
		[NotMapped]
		public bool valid {get; set;} = true;
		[NotMapped]
		public string status
		{
			get
			{
				string s = "待支付";
				if (order_id != 0 && order != null)
				{
					if (order.pay_state == 1)
					{
						s = "已支付";
						if (order.refunds != null && order.refunds.Length > 0)
						{
							s = "已退款";
						}
					}
					if (s.Trim().Equals("已支付"))
					{
						if (reserve_date.Date == DateTime.Now.Date)
						{
							s = "即将开始";
							if (startTime <= DateTime.Now)
							{
								s = "已过时";
							}
						}
						else if (reserve_date.Date <= DateTime.Now.Date)
						{
                            s = "已过时";
                        }
						else
						{
							s = "已预约";
						}
					}

				}
				return s.Trim();
			}
		}


	}
}


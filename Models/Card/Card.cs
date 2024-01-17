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
        public DateTime update_date { get; set; } = DateTime.Now;
        public string title { get; set; } = "";
        public string desc { get; set; } = "";

        [NotMapped]
        public List<CardLog> cardLogs { get; set; } = new List<CardLog>();


        public string timeStatus
        {
            get
            {
                string s = "无时限";
                if (start_date != null && ((DateTime)start_date).Date > DateTime.Now.Date)
                {
                    s = "未开始";
                }
                else
                {
                    s = "使用期内";
                }
                if (end_date != null && ((DateTime)end_date).Date < DateTime.Now.Date)
                {
                    s = "已过期";
                }
                else
                {
                    s = "使用期内";
                }
                return s;
            }
        }


    }
}


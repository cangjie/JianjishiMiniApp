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
		public DateTime start_date {get; set;}
		public DateTime end_date {get; set;}
		[NotMapped]
		public DateTime? startTime
		{
			get
			{
				try
				{
					return DateTime.Parse(description.Split('-')[0].Trim());
				}
				catch
				{
					return null;
				}
			}
		}
		[NotMapped]
		public DateTime? endTime
		{
            get
            {
                try
                {
                    return DateTime.Parse(description.Split('-')[1].Trim());
                }
                catch
                {
                    return null;
                }
            }
        }
		[NotMapped]
		public int avaliableCount { get; set; } = 0;

		[NotMapped]
		public List<Reserve> reserveList {get; set;}

		[NotMapped]
		public List<TherapeutistTimeTable> therapeutistTimeList {get; set;}
	}
}


using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OA.Models
{
    [Table("channel_follow")]
    public class ChannelFollow
    {
        [Key]
        public int id { get; set; }
        public int user_id { get; set; }
        public int channel_user_id { get; set; }
        public string scene { get; set; }
        public DateTime create_date { get; set; }

    }
}
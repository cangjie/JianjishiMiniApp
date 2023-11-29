using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models
{
            
    [Table("therapeutist_time_table")]
    public class TherapeutistTimeTable
    {
        [Key]
        public  int             id                  {get; set;}
        public  int             shop_time_id        {get; set;}
        public  int             therapeutist_id     {get; set;}
        public  int             in_use              {get; set;}
        public  DateTime        start_date          {get; set;}
        public  DateTime        end_date            {get; set;}

        [NotMapped]
        public  bool            avaliable           {get; set;} = true;
        [NotMapped]
        public  Therapeutist    therapeutist        {get; set;}
        [NotMapped]
        public  TimeTable?       shopTimeTable       { get; set; }


    }

}
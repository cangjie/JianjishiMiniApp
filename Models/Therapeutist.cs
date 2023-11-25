using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models
{
    [Table("therapeutist_list")]
    public class Therapeutist
    {
        [Key]
        public  int     id          {get; set;}
        public  string  name        {get; set;}
        public  string  desc        {get; set;}
        public  string  image_url   {get; set;}
        [NotMapped]
        public  bool    valid       {get; set;}

    }

}
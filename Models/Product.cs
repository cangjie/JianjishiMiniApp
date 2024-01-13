using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models
{
    [Table("product")]
    public class Product
    {
        [Key]
        public  int     id                  {get; set;}
        public  string  type                {get; set;}
        public  string  name                {get; set;}
        public  string  sub_title           {get; set;}   
        public  string  desc                {get; set;}
        public  string  region              {get; set;}
        public  double  sale_price          {get; set;}
        public  int     need_therapeutist   {get; set;}
        public  int     duration            {get; set;}
        public int? times { get; set; }
        public int? days { get; set; }
        public double? amount { get; set; }

    }

}
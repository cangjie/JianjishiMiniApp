using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MiniApp.Models
{
    public class InformList
    {
        [Key]
        public int id { get; set; }
        public string unionid { get; set; }
        public int active { get; set; }

    }
}


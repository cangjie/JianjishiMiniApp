using System;
namespace MiniApp.Models
{
    public class ShopDailyTimeList
    {
        public  Shop                shop                {get; set;}
        public  DateTime            queryDate           {get; set;}
        public  List<TimeTable>     timeList            {get; set;}
        public  List<Therapeutist>  therapeutistList    {get; set;}
        

    }


}
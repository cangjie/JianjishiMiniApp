using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp;
using MiniApp.Models;

namespace MiniApp.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ReserveController : ControllerBase
    {
        private readonly SqlServerContext _context;
        private readonly IConfiguration _config;

        private readonly OrderController _orderHelper;
        public ReserveController(SqlServerContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _orderHelper = new OrderController(context, config);
        }

        
        [NonAction]
        public async Task<ShopDailyTimeList> GetShopDailyTimeTable(int shopId, DateTime date)
        {
            ShopDailyTimeList list = new ShopDailyTimeList();
            list.shop = await _context.Shop.FindAsync(shopId);
            list.queryDate = date.Date;

            var shopTimeList = await _context.timeTable.Where(t => (t.shop_id == list.shop.id 
                && t.start_date <= date.Date && t.end_date >= date.Date && t.in_use == 1))
                .OrderBy(t => t.id).AsNoTracking().ToListAsync();
            
            List<TimeTable> newList = new List<TimeTable>();
            for(int i = 0; i < shopTimeList.Count; i++)
            {
                TimeTable tt = await GetTimeTable(shopTimeList[i].id, date);
                newList.Add(tt);

            }
            list.timeList = newList;

            return list;
        }

        [NonAction]
        public async Task<Reserve> GetReserve(int reserveId)
        {
            Reserve r = await _context.reserve.FindAsync(reserveId);
            if (r == null)
            {
                return null;
            }
            bool valid = r.cancel == 0? true : false;
            if (r.order_id != 0)
            {
                r.order = await _orderHelper.GetOrder(r.order_id);
                if (r.cancel == 0 && r.order.pay_state != 1 
                    && r.order.create_date < DateTime.Now.AddMinutes(-30) )
                {
                    valid = false;
                    r.cancel = 1;
                    r.cancel_memo = "超时未支付，自动取消。";
                    _context.Entry(r).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
            }
            return r;
        }

        [NonAction]
        public async Task<TimeTable> GetTimeTable(int id, DateTime date)
        {
            TimeTable t = await _context.timeTable.FindAsync(id);
            var rList = await _context.reserve.Where(r => (r.time_table_id == id) )
                .AsNoTracking().ToListAsync();
            List<Reserve> avalReserveList = new List<Reserve>();
            for(int i = 0; i < rList.Count; i++)
            {
                Reserve r = await GetReserve(rList[i].id);
                if (r.valid)
                {
                    avalReserveList.Add(r);
                }

            }



            t.reserveList = avalReserveList;
            t.avaliableCount = t.count - t.reserveList.Count;

            var therapeutistTimeList = await _context.therapeutistTimeTable
                .Where(tt => (tt.shop_time_id == id && tt.in_use == 1
                && tt.start_date <= date.Date && tt.end_date >= date.Date))
                .AsNoTracking().ToListAsync();
            

            for(int i = 0; i < therapeutistTimeList.Count; i++)
            {
                bool avaliable = true;
                int ttId = therapeutistTimeList[i].id;
                for(int j = 0; j < t.reserveList.Count; j++)
                {
                    if (t.reserveList[j].therapeutist_time_id == ttId)
                    {
                        avaliable = false;
                        break;
                    }

                }
                therapeutistTimeList[i].avaliable = avaliable;
                therapeutistTimeList[i].therapeutist = await _context.therapuetist.FindAsync(therapeutistTimeList[i].therapeutist_id);
                
            }
            t.therapeutistTimeList = therapeutistTimeList;
            
            return t;

        }
        
        [HttpGet("{shopId}")]
        public async Task<ActionResult<ShopDailyTimeList>> GetShopDailyTimeList(int shopId, DateTime date, string sessionKey)
        {

            return Ok(await GetShopDailyTimeTable(shopId, date));
        }

    }

}
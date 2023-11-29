using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp;
using MiniApp.Models;
using MiniApp.Models.Order;

namespace MiniApp.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ReserveController : ControllerBase
    {
        private readonly SqlServerContext _context;
        private readonly IConfiguration _config;

        private readonly OrderController _orderHelper;
        private readonly MiniUserController _userHelper;
        public ReserveController(SqlServerContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _orderHelper = new OrderController(context, config);
            _userHelper = new MiniUserController(context, config);
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

        [NonAction]
        public async Task<List<Reserve>> GetAvaliableReserveList(string openId)
        {
            var originList = await _context.reserve.Where(r => r.open_id.Trim().Equals(openId.Trim()))
                .AsNoTracking().ToListAsync();
            List<Reserve> l = new List<Reserve>();
            for (int i = 0; i < originList.Count; i++)
            {
                Reserve r = await GetReserve(originList[i].id);
                if (r.valid)
                {
                    l.Add(r);
                }
            }
            return l;
        }

        [HttpGet]
        public async Task<ActionResult<List<Reserve>>> GetMyReserveList(string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;
            return Ok(await GetAvaliableReserveList(user.open_id));
        }
        
        [HttpGet("{shopId}")]
        public async Task<ActionResult<ShopDailyTimeList>> GetShopDailyTimeList(int shopId, DateTime date, string sessionKey)
        {

            return Ok(await GetShopDailyTimeTable(shopId, date));
        }

        [HttpGet("{shopId}")]
        public async Task<ActionResult<Reserve>> Reserve(int shopId, int productId, int timeId, int therapeutistTimeId, DateTime date, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;
            date = date.Date;

            bool dup = false;
            List<Reserve> myReserveList = await GetAvaliableReserveList(user.open_id);
            for (int i = 0; i < myReserveList.Count; i++)
            {
                Reserve r = myReserveList[i];
                if (r.reserve_date.Date == date && r.time_table_id == timeId)
                {
                    dup = true;
                    break;
                }
            }

            if (dup)
            {
                return NoContent();
            }



            
            Product product = await _context.product.FindAsync(productId);
            Shop shop = await _context.Shop.FindAsync(shopId);
            TimeTable rTimeTable = await _context.timeTable.FindAsync(timeId);



            TherapeutistTimeTable? theraTimeTable;
            Therapeutist? therapeutist;
            string therapeutistName = "";
            if (therapeutistTimeId > 0)
            {
                theraTimeTable = await _context.therapeutistTimeTable.FindAsync(therapeutistTimeId);
                if (theraTimeTable == null)
                {
                    return BadRequest();
                }
                therapeutist = await _context.therapuetist.FindAsync(theraTimeTable.therapeutist_id);
                if (therapeutist == null)
                {
                    return BadRequest();
                }
                therapeutistName = therapeutist.name;
            }

            
            
            if (product == null || user == null || shop == null || rTimeTable == null )
            {
                return BadRequest();
            }
             
            
            ShopDailyTimeList l = await GetShopDailyTimeTable(shopId, date);
            bool avaliable = false;
            for(int i = 0; i < l.timeList.Count; i++)
            {
                if (l.timeList[i].id == timeId)
                {
                    TimeTable timeTable = l.timeList[i];
                    if (timeTable.avaliableCount > 0)
                    {
                        if (product.need_therapeutist == 1)
                        {
                            for(int j = 0; j < timeTable.therapeutistTimeList.Count; j++)
                            {
                                if (timeTable.therapeutistTimeList[j].id == therapeutistTimeId && timeTable.therapeutistTimeList[j].avaliable)
                                {
                                    avaliable = true;
                                    break;
                                }
                                
                            }
                        }
                        else
                        {
                            avaliable = true;
                        }

                    }
                    break;

                }

            }
            if (!avaliable)
            {
                return BadRequest();
            }
            OrderOnline order = new OrderOnline()
            {
                type = "门店预约",
                open_id = user.open_id,
                cell_number = user.cell_number.Trim(),
                name = user.real_name.Trim(),
                pay_method = "微信支付",
                order_price = product.sale_price,
                order_real_pay_price = product.sale_price,
                pay_state = 0,
                pay_memo = "预约订单",
                code = "",
                memo = "",
                shop = shop.name.Trim(),
                final_price = product.sale_price
            };
            await _context.OrderOnline.AddAsync(order);
            await _context.SaveChangesAsync();
            
            Reserve reserve = new Reserve()
            {
                open_id = user.open_id,
                reserve_date = date,
                time_table_id = timeId,
                time_table_description = rTimeTable.description,
                therapeutist_time_id = therapeutistTimeId,
                therapeutist_name = therapeutistName,
                shop_name = shop.name,
                product_id = product.id,
                product_name = product.name,
                order_id = order.id,
                cancel = 0

            };
            await _context.reserve.AddAsync(reserve);
            await _context.SaveChangesAsync();
            return Ok(await GetReserve(reserve.id));
        }

        [HttpGet("{timeTableId}")]
        public async Task<ActionResult<TimeTable>> GetTimeTableItem(int timeTableId)
        {
            TimeTable item = await _context.timeTable.FindAsync(timeTableId);
            item.therapeutistTimeList = await _context.therapeutistTimeTable
                .Where(t => t.shop_time_id == timeTableId).ToListAsync();
            return Ok(item);
        }

        [HttpGet("{therapeutistTimeId}")]
        public async Task<ActionResult<TherapeutistTimeTable>> GetTherapeutistTime(int therapeutistTimeId)
        {
            TherapeutistTimeTable item = await _context.therapeutistTimeTable.FindAsync(therapeutistTimeId);
            if (item.shop_time_id > 0)
            {
                item.shopTimeTable = await _context.timeTable.FindAsync(item.shop_time_id);
            }
            item.therapeutist = await _context.therapuetist.FindAsync(item.therapeutist_id);
            return Ok(item);
        }

    }

    

}
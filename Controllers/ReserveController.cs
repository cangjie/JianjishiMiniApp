using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp;
using MiniApp.Models;
using MiniApp.Models.Card;
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

        [HttpGet("{id}")]
        public async Task<ActionResult<Reserve>> Use(int id, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            MiniUser? user = (MiniUser?)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;
            
            if (user == null || user.staff == 0)
            {
                return BadRequest();
            }
            Reserve? r = await _context.reserve.FindAsync(id);
            if (r == null)
            {
                return NotFound();
            }
            r.used = 1;
            r.use_date = DateTime.Now;
            r.use_oper_open_id = user.open_id;
            _context.reserve.Entry(r).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(r);

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
            var ul = await _context.miniUser
                .Where(u => u.open_id.Trim().Equals(r.open_id.Trim()))
                .AsNoTracking().ToListAsync();
            if (ul != null && ul.Count > 0)
            {
                r.reserveUser = ul[0];
            }
            bool valid = r.cancel == 0? true : false;
            if (r.order_id != 0)
            {
                r.order = await _orderHelper.GetWholeOrder(r.order_id);
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
            var rList = await _context.reserve.Where(r => (r.time_table_id == id)
                && r.reserve_date.Date == date.Date )
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
            t.startTime = (DateTime?)date.Date.AddHours(((DateTime)t.startTime).Hour).AddMinutes(((DateTime)t.startTime).Minute);
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
                .OrderByDescending(r => r.id).AsNoTracking().ToListAsync();
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

        [HttpGet("{id}")]
        public async Task<ActionResult<Reserve>> GetReserve(int id, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            MiniUser? user = (MiniUser?)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;
            if (user == null || user.staff == 0)
            {
                return BadRequest();
            }
            Reserve? r = await GetReserve(id);

            if (r == null)
            {
                return NotFound();
            }
            var ul = await _context.miniUser
                .Where(u => u.open_id.Trim().Equals(r.open_id.Trim()))
                .AsNoTracking().ToListAsync();
            if (ul != null && ul.Count > 0)
            {
                r.reserveUser = ul[0];
            }
            return Ok(r);
        }

        [HttpGet]
        public async Task<ActionResult<List<Reserve>>> GetReserveListByStaff(string shop, DateTime date, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            shop = Util.UrlDecode(shop);
            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;
            if (user.staff == 0)
            {
                return BadRequest();
            }
            var rList = await _context.reserve.Where(r => r.shop_name.Trim().Equals(shop)
                && r.reserve_date.Date == date.Date).AsNoTracking().ToListAsync();
            List<Reserve> avaliableList = new List<Reserve>();
            for (int i = 0; i < rList.Count; i++)
            {
                Reserve r = await GetReserve(rList[i].id);
                if (r.valid)
                {
                    avaliableList.Add(r);
                }
            }
            return Ok(avaliableList);
        }

        [HttpGet]
        public async Task<ActionResult<List<Reserve>>> GetMyReserveList(string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;
            List<Reserve> rList = await GetAvaliableReserveList(user.open_id);
            for (int i = 0; i < rList.Count; i++)
            {
                rList[i].open_id = "";
                if (rList[i].order != null)
                {
                    OrderOnline order = rList[i].order;
                    order.open_id = "";
                    for (int j = 0; order.payments != null && j < order.payments.Length; j++)
                    {
                        order.payments[j].open_id = "";
                    }
                }
                
            }
            return Ok(rList);


            //return Ok(await GetAvaliableReserveList(user.open_id));
        }



        
        [HttpGet("{shopId}")]
        public async Task<ActionResult<ShopDailyTimeList>> GetShopDailyTimeList(int shopId, DateTime date, string sessionKey)
        {

            return Ok(await GetShopDailyTimeTable(shopId, date));
        }

        [HttpGet("{productId}")]
        public async Task<ActionResult<Reserve>> Reserve(int productId, int timeId, int therapeutistTimeId, DateTime date, int  cardId, string sessionKey)
        {
            string payMethod = "微信支付";




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
            
            TimeTable rTimeTable = await _context.timeTable.FindAsync(timeId);
            Shop shop = await _context.Shop.FindAsync(rTimeTable.shop_id);


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
             
            
            ShopDailyTimeList l = await GetShopDailyTimeTable(shop.id, date);
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
                pay_method = payMethod.Trim(),
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

        [NonAction]
        public async Task<Reserve> PayOrderWithCard(int cardId, Reserve reserve)
        {
            Card? card = await _context.Card.FindAsync(cardId);
            if (card.start_date != null && ((DateTime)card.start_date).Date > reserve.reserve_date.Date)
            {
                return null;
            }
            if (card.end_date != null && ((DateTime)card.end_date).Date < reserve.reserve_date.Date)
            {
                return null;
            }
            if (card == null)
            {
                return null;
            }
            if (!card.open_id.Trim().Equals(reserve.open_id.Trim()))
            {
                return null;
            }
            OrderOnline? order = await _context.OrderOnline.FindAsync(reserve.order_id);
            if (order == null)
            {
                return null;
            }
            bool productMatchCard = false;
            var pList = await _context.cardAssociateProduct.Where(p => p.card_product_id == card.product_id)
                .AsNoTracking().ToListAsync();
            for (int i = 0; i < pList.Count; i++)
            {
                if (pList[i].common_product_id == reserve.product_id)
                {
                    productMatchCard = true;
                    break;
                }
            }
            if (!productMatchCard)
            {
                return null;
            }
            Product? cardProd = await _context.product.FindAsync(card.product_id);
            if (cardProd == null)
            {
                return null;
            }
            Product? reserveProd = await _context.product.FindAsync(reserve.product_id);
            if (reserveProd == null)
            {
                return null;
            }
            CardLog log = new CardLog();
            log.id = 0;
            log.card_id = cardId;
            log.open_id = reserve.open_id;

            switch (cardProd.type.Trim())
            {
                case "储值卡":
                    if (card.total_amount - card.used_amount >= reserveProd.sale_price)
                    {
                        log.amount = reserveProd.sale_price;
                        card.used_amount = card.used_amount + reserveProd.sale_price;
                    }
                    else
                    {
                        return null;
                    }
                    
                    break;
                case "季卡":
                    log.times = 1;
                    break;
                case "次卡":
                    log.times = 1;
                    if (card.total_times - card.used_times >= 1)
                    {
                        card.used_times++;
                        log.times = 1;
                    }
                    else
                    {
                        return null;
                    }
                    break;
                default:
                    break;
            }

            await _context.cardLog.AddAsync(log);
            _context.Card.Entry(card).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            if (log.id == 0)
            {
                return null;
            }
            order.card_log_id = log.id;
            _context.OrderOnline.Entry(order).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return reserve;
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

        [HttpGet("{reserveId}")]
        public async Task<ActionResult<Reserve>> Refund(int reserveId, double amount,  string sessionKey, string memo = "")
        {
            memo = Util.UrlDecode(memo);
            Reserve r = await GetReserve(reserveId);
            sessionKey = Util.UrlDecode(sessionKey);
            MiniUser? user = (MiniUser?)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;
            if (user == null || (user.staff == 0 && !r.status.Equals("已预约")
                && r.open_id.Trim().Equals(user.open_id.Trim())))
            {
                return BadRequest();
            }
            try
            {
                await _orderHelper.TenpayRefund(r.order.payments[0].id, amount, memo, user);
                return Ok(await GetReserve(reserveId));
            }
            catch
            {
                return BadRequest();
            }
        }

    }

    

}
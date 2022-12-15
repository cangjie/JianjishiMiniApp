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
    public class ShopController : ControllerBase
    {
        private readonly SqlServerContext _context;
        private readonly IConfiguration _config;

        public ShopController(SqlServerContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: api/Shop
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Shop>>> GetShop()
        {
            if (_context.Shop == null)
            {
                return NotFound();
            }
            return await _context.Shop.OrderBy(s => s.sort)
                .OrderBy(s => s.id).ToListAsync();
        }

        [HttpGet("{shopId}")]
        public async Task<ActionResult<IEnumerable<TimeTable>>> GetTimeTable(int shopId, DateTime date)
        {

            List<TimeTable> timeList = await _context.timeTable.Where(t => (t.shop_id == shopId))
                .OrderBy(t => t.id).ToListAsync();
            for (int i = 0; i < timeList.Count; i++)
            {
                TimeTable t = timeList[i];
                List<Reserve> rList = await _context.reserve.Where(r => (r.time_table_id == t.id))
                    .ToListAsync();
                t.avaliableCount = t.count - rList.Count;
            }

            Shop shop = await _context.Shop.FindAsync(shopId);
            bool isAvaliable = true;
            string[] closeDatesArr = shop.close_dates.Split(',');
            for (int i = 0; i < closeDatesArr.Length; i++)
            {
                switch (closeDatesArr[i].Trim())
                {
                    case "0":
                        if (date.DayOfWeek == DayOfWeek.Sunday)
                        {
                            isAvaliable = false;
                        }
                        break;
                    case "1":
                        if (date.DayOfWeek == DayOfWeek.Monday)
                        {
                            isAvaliable = false;
                        }
                        break;
                    case "2":
                        if (date.DayOfWeek == DayOfWeek.Tuesday)
                        {
                            isAvaliable = false;
                        }
                        break;
                    case "3":
                        if (date.DayOfWeek == DayOfWeek.Wednesday)
                        {
                            isAvaliable = false;
                        }
                        break;
                    case "4":
                        if (date.DayOfWeek == DayOfWeek.Thursday)
                        {
                            isAvaliable = false;
                        }
                        break;
                    case "5":
                        if (date.DayOfWeek == DayOfWeek.Friday)
                        {
                            isAvaliable = false;
                        }
                        break;
                    case "6":
                        if (date.DayOfWeek == DayOfWeek.Saturday)
                        {
                            isAvaliable = false;
                        }
                        break;
                    default:
                        try
                        {
                            if (DateTime.Parse(closeDatesArr[i].Trim()).Date == date.Date)
                            {
                                isAvaliable = false;
                            }
                        }
                        catch
                        {

                        }
                        
                        break;

                }
            }
            if (!isAvaliable)
            {
                timeList.Clear();
            }
            return timeList;
        }

        [HttpGet("{shopId}")]
        public async Task<ActionResult<Reserve>> Reserve(int shopId, int timeTableId, DateTime date, string name, string cell, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey).Trim();
            MiniUserController userHelper = new MiniUserController(_context, _config);
            MiniUser user = (await userHelper.GetBySessionKey(sessionKey)).Value;
            if (user == null)
            {
                return BadRequest();
            }
            user.real_name = name;
            user.cell_number = cell;
            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            List<Reserve> rList = await _context.reserve
                .Where(r => (r.open_id.Trim().Equals(user.open_id.Trim()) && r.reserve_date.Date == date.Date))
                .ToListAsync();
            if (rList.Count > 0)
            {
                return BadRequest();
            }
            TimeTable t = await _context.timeTable.FindAsync(timeTableId);
            Reserve r = new Reserve()
            {
                open_id = user.open_id.Trim(),
                time_table_id = t.id,
                time_table_description = t.description.Trim(),
                reserve_date = date

            };
            await _context.reserve.AddAsync(r);
            await _context.SaveChangesAsync();

            if (r.id > 0)
            {
                Shop shop = await _context.Shop.FindAsync(shopId);
                string msg = "预约提醒 姓名：" + name + " 手机：" + cell + " 门店：" + shop.name + "日期：" + r.reserve_date.ToShortDateString() +  " 时间：" + t.description
                    + " <a data-miniprogram-appid='" + _config.GetSection("Settings").GetSection("AppId").Value.Trim()
                    + "' data-miniprogram-path='pages/reserve/admin?date=" + r.reserve_date.ToShortDateString() + "' >查看详情</a>";

                List<InformList> list = await _context.informList.Where(l => l.active == 1).ToListAsync();

                for (int i = 0; i < list.Count; i++)
                {
                    try
                    {
                        string unionId = list[i].unionid;
                        Util.GetWebContent("http://weixin.spineguard.cn/api/OfficialAccountApi/SendTextServiceMessage?unionId="
                              + unionId.Trim()   , "POST", msg, "text/plain");
                        

                    }
                    catch
                    {

                    }
                }

                
            }

            return r;
        }

        [HttpGet("{sessionKey}")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyReserve(string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            MiniUserController userHelper = new MiniUserController(_context, _config);
            MiniUser user = (await userHelper.GetBySessionKey(sessionKey)).Value;
            if (user == null)
            {
                return BadRequest();
            }
            var reserveList = await _context.reserve
                .Where(r => (r.open_id.Trim().Equals(user.open_id.Trim()) && r.cancel == 0))
                .Join(_context.timeTable, r => r.time_table_id, t => t.id, (r, t) => new {r.id, t.shop_id,  t.shop_name, r.reserve_date, r.time_table_description})
                .Join(_context.Shop, rr => rr.shop_id, s => s.id, (rr, s) => new {rr.id, rr.shop_id, rr.shop_name, rr.reserve_date, rr.time_table_description, s.address })
                .ToListAsync();
            return Ok(reserveList);

        }

        [HttpGet("{sessionKey}")]
        public async Task<ActionResult<IEnumerable<object>>> GetReserveByStaff(string sessionKey, int shopId, DateTime start, DateTime end)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            MiniUserController userHelper = new MiniUserController(_context, _config);
            MiniUser user = (await userHelper.GetBySessionKey(sessionKey)).Value;
            if (user == null || user.staff == 0)
            {
                return BadRequest();
            }
            var reserveList = await _context.reserve.Where(r => r.cancel == 0)
                .Join(_context.timeTable, r => r.time_table_id, t => t.id, (r, t) => new {r.id, r.open_id, t.shop_name, r.time_table_description, r.reserve_date, t.shop_id })
                .Where(t => ((shopId == 0 || t.shop_id == shopId) && t.reserve_date >= start.Date && t.reserve_date <= end.Date  ))
                .Join(_context.miniUser, tt => tt.open_id, u => u.open_id, (tt, u) => new {tt.id, u.real_name, u.cell_number, tt.shop_name, tt.time_table_description, tt.reserve_date })
                
                .ToListAsync();
            return Ok(reserveList);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Reserve>> GetReserve(int id, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey.Trim());
            MiniUserController userHelper = new MiniUserController(_context, _config);
            MiniUser user = (await userHelper.GetBySessionKey(sessionKey)).Value;
            if (user == null)
            {
                return BadRequest();
            }
            Reserve r = await _context.reserve.FindAsync(id);
            if (user.staff == 1 || user.open_id.Trim().Equals(r.open_id.Trim()))
            {
                return r;
            }
            else
            {
                return BadRequest();
            }
        }

            
        /*

        // GET: api/Shop/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Shop>> GetShop(int id)
        {
          if (_context.Shop == null)
          {
              return NotFound();
          }
            var shop = await _context.Shop.FindAsync(id);

            if (shop == null)
            {
                return NotFound();
            }

            return shop;
        }

        // PUT: api/Shop/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutShop(int id, Shop shop)
        {
            if (id != shop.id)
            {
                return BadRequest();
            }

            _context.Entry(shop).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ShopExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Shop
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Shop>> PostShop(Shop shop)
        {
          if (_context.Shop == null)
          {
              return Problem("Entity set 'SqlServerContext.Shop'  is null.");
          }
            _context.Shop.Add(shop);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetShop", new { id = shop.id }, shop);
        }

        // DELETE: api/Shop/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShop(int id)
        {
            if (_context.Shop == null)
            {
                return NotFound();
            }
            var shop = await _context.Shop.FindAsync(id);
            if (shop == null)
            {
                return NotFound();
            }

            _context.Shop.Remove(shop);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        */
        private bool ShopExists(int id)
        {
            return (_context.Shop?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}

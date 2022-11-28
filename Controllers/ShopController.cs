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
            return timeList;
        }

        [HttpGet("{shopId}")]
        public async Task<ActionResult<Reserve>> Reserve(int shopId, int timeTableId, DateTime date, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey).Trim();
            MiniUserController userHelper = new MiniUserController(_context, _config);
            MiniUser user = (await userHelper.GetBySessionKey(sessionKey)).Value;
            if (user == null)
            {
                return BadRequest();
            }
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
            return r;
        }

        [HttpGet("sessionKey")]
        public async Task<ActionResult<IEnumerable<Reserve>>> GetMyReserve(string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            MiniUserController userHelper = new MiniUserController(_context, _config);
            MiniUser user = (await userHelper.GetBySessionKey(sessionKey)).Value;
            if (user == null)
            {
                return BadRequest();
            }
            List<Reserve> reserveList = await _context.reserve.Where(r => r.open_id.Trim().Equals(user.open_id.Trim())).ToListAsync();
            return reserveList;

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

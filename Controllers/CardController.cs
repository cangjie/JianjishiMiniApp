using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp;
using MiniApp.Models.Card;
using MiniApp.Models;
namespace MiniApp.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CardController : ControllerBase
    {
        private readonly SqlServerContext _db;
        private readonly IConfiguration _config;
        private readonly MiniUserController _userHelper;
        private readonly string _appId = "";

        
        public CardController(SqlServerContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _userHelper = new MiniUserController(context, config);
            _appId = _config.GetSection("Settings").GetSection("AppId").Value.Trim();
        }

        

        [NonAction]
        public async Task<Card> CreateCard(int productId)
        {
            Product? p = await _db.product.FindAsync(productId);
            if (p == null)
            {
                return null;
            }
            
            Card card = new Card()
            {
                id = 0,
                product_id = productId,
                title = p.name.Trim(),
                desc = p.desc.Trim()
            };
            if (p.days != null)
            {
                card.start_date = DateTime.Now.Date;
                card.end_date = DateTime.Now.Date.AddDays((int)p.days);
            }
            if (p.amount != null)
            {
                card.total_amount = (double)p.amount;
            }
            if (p.times != null)
            {
                card.total_times = (int)p.times;
            }
            await _db.Card.AddAsync(card);
            await _db.SaveChangesAsync();
            return card;
        }
        
        [HttpGet("{cardId}")]
        public async Task<ActionResult<CardLog>> Use(int cardId, double amount, int times, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;

            Card? card = await _db.Card.FindAsync(cardId);
            if (card == null)
            {
                return NotFound();
            }
            string openId = "";
            string staffOpenId = "";
            if (user.staff != 1 && !user.open_id.Trim().Equals(card.open_id.Trim()))
            {
                return NoContent();
            }

            if (user.staff == 1)
            {
                staffOpenId = user.open_id.Trim();
                openId = card.open_id;
            }
            else
            {
                staffOpenId = "";
                openId = user.open_id.Trim();
            }

            bool valid = true;

            if (card.start_date != null && ((DateTime)card.start_date).Date > DateTime.Now.Date)
            {
                valid = false;
            }

            if (card.end_date != null && ((DateTime)card.end_date) < DateTime.Now.Date)
            {
                valid = false;
            }

            if (card.total_times != null && (card.used_times + times) > card.total_times)
            {
                valid = false;
            }
            if (card.total_amount != null && (card.used_amount + amount) > card.total_amount)
            {
                valid = false;
            }
            if (!valid)
            {
                return BadRequest();
            }


            CardLog log = new CardLog()
            {
                id = 0,
                card_id = card.id,
                amount = amount,
                times = times,
                use_date = DateTime.Now,
                open_id = openId.Trim(),
                staff_open_id = staffOpenId.Trim()
            };

            if (amount > 0)
            {
                if (card.used_amount == null)
                {
                    card.used_amount = amount;
                }
                else
                {
                    card.used_amount += amount;
                }
            }

            if (times > 0)
            {
                if (card.used_times == null)
                {
                    card.used_times = times;
                }
                else
                {
                    card.used_times += times;
                }
            }
            try
            {
                await _db.cardLog.AddAsync(log);
                _db.Card.Entry(card).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return Ok(log);
            }
            catch
            {
                return BadRequest();
            }
            
        }
        
        [HttpGet("{cardId}")]
        public async Task<ActionResult<Card>> GetWholeCard(int cardId, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);

            Card? card = await _db.Card.FindAsync(cardId);
            if (card == null)
            {
                return NotFound();
            }

            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;

            if (user.staff == 0 && !user.open_id.Trim().Equals(user.open_id.Trim()))
            {
                return BadRequest();
            }

            var log = await _db.cardLog.Where(log => log.card_id == card.id)
                .OrderByDescending(log => log.id).AsNoTracking().ToListAsync();
            for (int i = 0; log != null && i < log.Count; i++)
            {
                card.cardLogs.Add(log[i]);
            }

            Models.Order.OrderOnline? order = await _db.OrderOnline.FindAsync(card.order_id);
            card.order = order;

            if (order != null)
            {
               
                Product? prod = await _db.product.FindAsync(order.product_id);
                card.product = prod;
                var assoProductIdList = await _db.cardAssociateProduct.Where(p => p.card_product_id == prod.id)
                    .AsNoTracking().ToListAsync();
                List<Product> pList = new List<Product>();
                for (int i = 0; assoProductIdList != null && i < assoProductIdList.Count; i++)
                {
                    Product? p = await _db.product.FindAsync(assoProductIdList[i].common_product_id);
                    if (p != null)
                    {
                        pList.Add(p);
                    }
                }
                card.associateProdct = pList;
            }

            

            return Ok(card);
        }
        
        [HttpGet]
        public async Task<ActionResult<List<Card>>> GetAllCustomerCards(string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;

            var list = await _db.Card.Where(card => card.open_id.Trim().Equals(user.open_id.Trim()))
                .OrderByDescending(card => card.id).AsNoTracking().ToListAsync();
            List<Card> cardList = new List<Card>();
            for (int i = 0; list != null && i < list.Count; i++)
            {
                Card card = (Card)((OkObjectResult)(await GetWholeCard(list[i].id, sessionKey)).Result).Value;
                cardList.Add(card);
            }
            return Ok(cardList);
        }

        /*

        // GET: api/Card
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Card>>> GetCard()
        {
          if (_context.Card == null)
          {
              return NotFound();
          }
            return await _context.Card.ToListAsync();
        }

        // GET: api/Card/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Card>> GetCard(int id)
        {
          if (_context.Card == null)
          {
              return NotFound();
          }
            var card = await _context.Card.FindAsync(id);

            if (card == null)
            {
                return NotFound();
            }

            return card;
        }

        // PUT: api/Card/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCard(int id, Card card)
        {
            if (id != card.id)
            {
                return BadRequest();
            }

            _context.Entry(card).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CardExists(id))
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

        // POST: api/Card
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Card>> PostCard(Card card)
        {
          if (_context.Card == null)
          {
              return Problem("Entity set 'SqlServerContext.Card'  is null.");
          }
            _context.Card.Add(card);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCard", new { id = card.id }, card);
        }

        // DELETE: api/Card/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCard(int id)
        {
            if (_context.Card == null)
            {
                return NotFound();
            }
            var card = await _context.Card.FindAsync(id);
            if (card == null)
            {
                return NotFound();
            }

            _context.Card.Remove(card);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        */
        [NonAction]
        private bool CardExists(int id)
        {
            return (_db.Card?.Any(e => e.id == id)).GetValueOrDefault();
        }

        
    }
}

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
    [Route("api/[controller]")]
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
        private bool CardExists(int id)
        {
            return (_db.Card?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}

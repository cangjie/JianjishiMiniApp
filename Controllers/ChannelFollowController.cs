using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp;
using MiniApp.Models;
using OA.Models;
using System.Data;

namespace MiniApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChannelFollowController : ControllerBase
    {
        private readonly SqlServerContext _db;
        private readonly IConfiguration _config;
        private readonly MiniUserController _userHelper;
        private readonly string _appId = "";


        public ChannelFollowController(SqlServerContext context, IConfiguration config)
        {
            _db = context;
            _config = config;
            _userHelper = new MiniUserController(context, config);
            _appId = _config.GetSection("Settings").GetSection("AppId").Value.Trim();

        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<List<KeyValuePair<DateTime, int>>>> GetSingleUserFollowList(int userId, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey.Trim());

            MiniUser sessionKeyUser = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;

            Models.User? user = await _db.user.FindAsync(userId);

            if (sessionKeyUser.staff != 1 && !sessionKeyUser.union_id.Trim().Equals(user.oa_union_id.Trim()))
            {
                return BadRequest();
            }
            DataTable dt = await GetFollowList(userId);
            List<KeyValuePair<DateTime, int>> list = new List<KeyValuePair<DateTime, int>>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                KeyValuePair<DateTime, int> item
                    = new KeyValuePair<DateTime, int>((DateTime)dt.Rows[i][0], (int)dt.Rows[i][1]);
                list.Add(item);
            }
            return Ok(list);
        }


        [NonAction]
        public async Task<DataTable> GetFollowList(int userId)
        {
            /*
            sessionKey = Util.UrlDecode(sessionKey.Trim());
        
            MiniUser sessionKeyUser = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;

            Models.User? user = await _db.user.FindAsync(userId);

            if (sessionKeyUser.staff != 1 && !sessionKeyUser.union_id.Trim().Equals(user.oa_union_id.Trim()))
            {
                return BadRequest();
            }
            */
            var followList = await _db.ChannelFollow
                .Where(f => (f.scene.Trim().Equals("follow") && f.channel_user_id == userId))
                .OrderByDescending(l => l.create_date).AsNoTracking().ToListAsync();

            DataTable dt = new DataTable();
            dt.Columns.Add("date", typeof(System.DateTime));
            dt.Columns.Add("count", typeof(System.Int32));

            for (int i = 0; i < followList.Count; i++)
            {
                bool find = false;
                for (int j = 0; j < dt.Rows.Count; j++)
                {
                    if (((DateTime)dt.Rows[j][0]).Date == followList[i].create_date.Date)
                    {
                        dt.Rows[j][1] = (int)dt.Rows[j][1] + 1;
                        find = true;
                        break;
                    }
                }
                if (!find)
                {
                    DataRow dr = dt.NewRow();
                    dr[0] = followList[i].create_date.Date;
                    dr[1] = 1;
                    dt.Rows.Add(dr);
                }
            }
            /*
            List<KeyValuePair<DateTime, int>> list = new List<KeyValuePair<DateTime, int>>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                KeyValuePair<DateTime, int> item
                    = new KeyValuePair<DateTime, int>((DateTime)dt.Rows[i][0], (int)dt.Rows[i][1]);
                list.Add(item);
            }
            */
            return dt;
        }

        /*
        // GET: api/ChannelFollow
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChannelFollow>>> GetChannelFollow()
        {
          if (_context.ChannelFollow == null)
          {
              return NotFound();
          }
            return await _context.ChannelFollow.ToListAsync();
        }

        // GET: api/ChannelFollow/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ChannelFollow>> GetChannelFollow(int id)
        {
          if (_context.ChannelFollow == null)
          {
              return NotFound();
          }
            var channelFollow = await _context.ChannelFollow.FindAsync(id);

            if (channelFollow == null)
            {
                return NotFound();
            }

            return channelFollow;
        }

        // PUT: api/ChannelFollow/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChannelFollow(int id, ChannelFollow channelFollow)
        {
            if (id != channelFollow.id)
            {
                return BadRequest();
            }

            _context.Entry(channelFollow).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ChannelFollowExists(id))
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

        // POST: api/ChannelFollow
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ChannelFollow>> PostChannelFollow(ChannelFollow channelFollow)
        {
          if (_context.ChannelFollow == null)
          {
              return Problem("Entity set 'SqlServerContext.ChannelFollow'  is null.");
          }
            _context.ChannelFollow.Add(channelFollow);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetChannelFollow", new { id = channelFollow.id }, channelFollow);
        }

        // DELETE: api/ChannelFollow/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChannelFollow(int id)
        {
            if (_context.ChannelFollow == null)
            {
                return NotFound();
            }
            var channelFollow = await _context.ChannelFollow.FindAsync(id);
            if (channelFollow == null)
            {
                return NotFound();
            }

            _context.ChannelFollow.Remove(channelFollow);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ChannelFollowExists(int id)
        {
            return (_context.ChannelFollow?.Any(e => e.id == id)).GetValueOrDefault();
        }
        */
    }
}

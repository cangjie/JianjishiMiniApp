using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using MiniApp.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MiniApp.Controllers
{
    [Route("api/[controller]/[action]")]
    public class MiniAppLoginController : Controller
    {

        private IConfiguration config;

        private readonly SqlServerContext _context;

        //public string _originalId = "";

        public MiniAppLoginController(SqlServerContext context, IConfiguration config)
        {
            this.config = config.GetSection("Settings");
            _context = context;
            //_originalId = config.GetSection("OriginalId").Value.Trim();
        }

       

 
        // GET api/values/5
        [HttpGet]
        public string GetSessionKey(string code)
        {
            code = Util.UrlDecode(code.Trim());
            string appId = config.GetSection("AppId").Value.Trim();
            string appSecret = config.GetSection("AppSecret").Value.Trim();
            string originalId = config.GetSection("OriginalId").Value.Trim();

            string sessionKeyJson = Util.GetWebContent("https://api.weixin.qq.com/sns/jscode2session?appid="
                + appId.Trim() + "&secret=" + appSecret.Trim() + "&js_code=" + code.Trim() + "&grant_type=authorization_code");
            Newtonsoft.Json.Linq.JObject resultObj = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(sessionKeyJson);
            Newtonsoft.Json.Linq.JToken result;

            string openId = "";
            string unionId = "";
            string sessionKey = "";

            if (resultObj.TryGetValue("errcode", out result))
            {
                return "";
            }

            if (resultObj.TryGetValue("openid", out result))
            {
                openId = result.ToString();
            }
            if (resultObj.TryGetValue("unionid", out result))
            {
                unionId = result.ToString();
            }
            if (unionId == null)
            {
                unionId = "";
            }
            if (resultObj.TryGetValue("session_key", out result))
            {
                sessionKey = result.ToString();
            }

            if (_context.miniSession.Find(originalId, openId) != null)
            {
                return sessionKey.Trim();
            }

            MiniSession miniSession = new MiniSession();
            miniSession.open_id = openId.Trim();
            miniSession.original_id = originalId.Trim();
            miniSession.session_key = sessionKey.Trim();
            miniSession.union_id = unionId.Trim();
            try
            {
                _context.miniSession.Add(miniSession);
                _context.SaveChanges();
               
                
            }
            catch(Exception err)
            {
                Console.WriteLine(err.ToString());
            }

            List<MiniUser> userList = _context.miniUser.Where<MiniUser>(u => u.original_id == originalId.Trim() && u.open_id == openId.Trim()).ToList<MiniUser>();

            if (userList.Count == 0)
            {
                MiniUser user = new MiniUser();
                user.original_id = originalId.Trim();
                user.open_id = openId.Trim();
                user.union_id = unionId.Trim();
                _context.miniUser.Add(user);
                _context.SaveChanges();
            }


            return sessionKey.Trim();
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

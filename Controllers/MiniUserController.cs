using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiniApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace MiniApp.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MiniUserController : ControllerBase
    {
        private IConfiguration _config;

        private readonly SqlServerContext _context;

        public string _originalId = "";

        public MiniUserController(SqlServerContext context, IConfiguration config)
        {
            _config = config.GetSection("Settings");
            _context = context;
            _originalId = _config.GetSection("OriginalId").Value.Trim();
        }

        // GET: api/MiniUser
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/MiniUser/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }


        [HttpGet("{sessionKey}")]
        public async Task<ActionResult<MiniUser>> GetBySessionKey(string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey);
            MiniSession mSession = await _context.miniSession.FindAsync(_originalId, sessionKey);
            if (mSession == null)
            {
                return NotFound();
            }
            string openId = mSession.open_id.Trim();
            //string unionId = mSession.union_id.Trim();

            if (openId.Trim().Equals(""))
            {
                return NotFound();
            }

            List<MiniUser> userList = await  _context.miniUser.Where(u => u.open_id == openId && u.original_id == _originalId).ToListAsync<MiniUser>();

            if (userList.Count == 0)
            {
                return NotFound();
            }
            else
            {
                return userList[0];
            }
            
        }

        [HttpPost]
        public async Task<ActionResult<MiniUser>> UpdateCellNumber(object postData)
        {
            JObject resultObj = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(postData.ToString());
            JToken result;
            string encryptedData = "";
            string sessionKey = "";
            string iv = "";

            if (resultObj.TryGetValue("encryptedData", out result))
            {
                encryptedData = result.ToString();
            }

            if (resultObj.TryGetValue("iv", out result))
            {
                iv = result.ToString();
            }

            if (resultObj.TryGetValue("sessionKey", out result))
            {
                sessionKey = result.ToString();
            }

            string cellNumber = "";
            string json = AES_decrypt(encryptedData, sessionKey, iv);
            resultObj = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(json);
            
            if (resultObj.TryGetValue("phoneNumber", out result))
            {
                cellNumber = result.ToString();
            }


            MiniUser user = (await GetBySessionKey(sessionKey)).Value;
            if (user == null)
            {
                user = new MiniUser();
                user.original_id = _originalId;
                MiniSession miniSession = await _context.miniSession.FindAsync(_originalId, sessionKey);
                if (miniSession == null)
                {
                    throw new Exception("Session key is not valid.");
                }
                user.open_id = miniSession.open_id;
                user.union_id = miniSession.union_id;
                user.nick = "";
                user.avatar = "";
                user.gender = null;
                user.city = "";
                user.province = "";
                user.country = "";
                user.language = "";
                user.id = 0;
                user.cell_number = cellNumber.Trim();
                await _context.miniUser.AddAsync(user);
            }
            else
            {
                user.cell_number = cellNumber.Trim();
                _context.Entry(user).State = EntityState.Modified;
            }
            await _context.SaveChangesAsync();
            user.open_id = "";
            return user;
        }

        //public async Task<ActionResult<SchoolLesson>> PostSchoolLesson(SchoolLesson schoolLesson, string sessionKey)
        // POST: api/MiniUser
        [HttpPost("{sessionKey}")]
        public ActionResult<MiniUser> PostMiniUser(MiniUser user, string sessionKey)
        {

            MiniSession mSession = _context.miniSession.Find(_originalId, sessionKey);
            if (mSession == null)
            {
                return NotFound();
            }
            string openId = mSession.open_id.Trim();
            string unionId = mSession.union_id.Trim();
            if (openId.Trim().Equals(""))
            {
                return NotFound();
            }

            List<MiniUser> userList = _context.miniUser.Where<MiniUser>(u => openId == u.open_id && u.original_id == _originalId).ToList<MiniUser>();

            if (userList.Count > 0)
            {
                return NotFound();
            }

            user.open_id = openId;
            user.original_id = _originalId;
            user.union_id = unionId;

            try
            {
                _context.miniUser.Add(user);
                _context.SaveChanges();
                user.open_id = "";
                user.union_id = "";
                return CreatedAtAction("Get", new { id = user.id }, user);
                //return CreatedAtAction("GetSchoolLesson", new { id = schoolLesson.id }, schoolLesson);

            }
            catch
            {
                return NotFound();
            }

            //return NotFound();
        }

        // PUT: api/MiniUser/5
        [HttpPut("{sessionKey}")]
        public async Task<ActionResult<MiniUser>> PutMiniUser(MiniUser user, string sessionKey)
        {
            


            bool isStaff = false;


            MiniUser operUser = (await GetBySessionKey(sessionKey)).Value;

            
        
            //_context.Entry(user).State = EntityState.Modified;



            if (operUser == null)
            {
                return NotFound();
            }


            if (operUser.staff == 1)
            {
                isStaff = true;
            }

            if (isStaff || operUser.id == user.id)
            {
                if (operUser.id == user.id)
                {
                    user.open_id = operUser.open_id.Trim();
                    user.union_id = operUser.union_id.Trim();
                    user.original_id = _originalId;
                }
                _context.Entry(user).State = EntityState.Modified;

                try
                {
                    _context.SaveChanges();
                    return CreatedAtAction("Get", new { id = user.id }, user);

                }
                catch
                {
                    return NotFound();
                }
                
            }

            return null;
        }

        // DELETE: api/MiniUser/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        public static string AES_decrypt(string encryptedDataStr, string key, string iv)
        {
            RijndaelManaged rijalg = new RijndaelManaged();
            //-----------------    
            //设置 cipher 格式 AES-128-CBC    

            rijalg.KeySize = 128;

            rijalg.Padding = PaddingMode.PKCS7;
            rijalg.Mode = CipherMode.CBC;

            rijalg.Key = Convert.FromBase64String(key);
            rijalg.IV = Convert.FromBase64String(iv);


            byte[] encryptedData = Convert.FromBase64String(encryptedDataStr);
            //解密    
            ICryptoTransform decryptor = rijalg.CreateDecryptor(rijalg.Key, rijalg.IV);

            string result = "";

            using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {

                        result += srDecrypt.ReadToEnd();
                    }
                }
            }

            return result;
        }

        public static MiniUser GetMiniUserBySessionKey(string originalId, string sessionKey, SqlServerContext context)
        {
            MiniSession session = context.miniSession.Find(originalId, sessionKey);
            if (session == null)
            {
                return null;
            }
            List<MiniUser> userList = context.miniUser.AsNoTracking<MiniUser>().Where<MiniUser>(u => u.original_id == originalId && u.open_id.Trim() == session.open_id.Trim()).ToList<MiniUser>();
            if (userList.Count == 0)
            {
                return null;
            }
            return (MiniUser)userList[0];
        }

    }
}

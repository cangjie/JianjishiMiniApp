using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp.Models;
using MiniApp.Models.Order;
using SKIT.FlurlHttpClient.Wechat.TenpayV2;
using SKIT.FlurlHttpClient.Wechat.TenpayV2.Models;
using static MiniApp.Controllers.OrderController;

namespace MiniApp.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TenpaySharingController : ControllerBase
    {
        private readonly SqlServerContext _db;
        private readonly IConfiguration _config;
        private readonly MiniUserController _userHelper;
        private readonly string _appId = "";

        public TenpaySharingController(SqlServerContext context, IConfiguration config)
		{
            _db = context;
            _config = config;
            _userHelper = new MiniUserController(context, config);
            _appId = _config.GetSection("Settings").GetSection("AppId").Value.Trim();
        }

        [HttpGet("{paymentId}")]
        public async Task<ActionResult<TenpaySet>> TenpayRequest(int paymentId, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey.Trim());
            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;
            if (user == null)
            {
                return BadRequest();
            }
            OrderPayment payment = await _db.orderPayment.FindAsync(paymentId);
            if (payment == null)
            {
                return BadRequest();
            }
            OrderOnline order = await _db.OrderOnline.FindAsync(payment.order_id);
            if (order == null)
            {
                return BadRequest();
            }
            if (!payment.pay_method.Trim().Equals("微信支付") || !payment.status.Trim().Equals("待支付"))
            {
                return BadRequest();
            }
            if (order.status.Trim().Equals("支付完成") || order.status.Trim().Equals("订单关闭"))
            {
                return BadRequest();
            }
            string timeStamp = Util.getTime13().ToString();
            int mchid = (int)payment.mch_id;
            WepayKey key = await _db.WepayKeys.FindAsync(mchid);
        
            //var certManager = new WechatTenpayClientOptions();
            
            var options = new WechatTenpayClientOptions()
            {
                MerchantId = key.mch_id.Trim(),
                MerchantSecret = ""
                //MerchantCertificateSerialNumber = key.key_serial.Trim(),
                //MerchantCertificatePrivateKey = key.private_key.Trim(),
                //PlatformCertificateManager = certManager
            };
            
            string desc = "未知商品";

            
            var req = new CreatePayUnifiedOrderRequest()
            {
                
            };


            return BadRequest();
        }

    }
}


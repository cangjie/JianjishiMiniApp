using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp;
using MiniApp.Models.Order;
using SKIT.FlurlHttpClient.Wechat.TenpayV3;
using SKIT.FlurlHttpClient.Wechat.TenpayV3.Models;
using SKIT.FlurlHttpClient.Wechat.TenpayV3.Settings;
using MiniApp.Models;

namespace MiniApp.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly SqlServerContext _context;
        private readonly IConfiguration _config;
        private readonly MiniUserController _userHelper;
        private readonly string _appId = "";

        public OrderController(SqlServerContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _userHelper = new MiniUserController(context, config);
            _appId = _config.GetSection("Settings").GetSection("AppId").Value.Trim();
        }


        public class TenpayResource
        {
            public string original_type { get; set; }
            public string algorithm { get; set; }
            public string ciphertext { get; set; }
            public string associated_data { get; set; }
            public string nonce { get; set; }
        }

        public class TenpayCallBackStruct
        {
            public string id { get; set; }
            public DateTimeOffset create_time { get; set; }
            public string resource_type { get; set; }
            public string event_type { get; set; }
            public string summary { get; set; }
            public TenpayResource resource { get; set; }
        }

        public class TenpaySet
        {
            public string nonce { get; set; }
            public string prepay_id { get; set; }
            public string sign { get; set; }
            public string timeStamp { get; set; }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderOnline>> GetWholeOrder(int id, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey.Trim());
            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;

            OrderOnline order = await _context.OrderOnline.FindAsync(id);
            if (user.staff != 1 && !order.open_id.Trim().Equals(user.open_id.Trim()))
            {
                return BadRequest();
            }

            order.payments = await _context.orderPayment.Where(p => p.order_id == id).ToArrayAsync();
            return Ok(order);

        }

        [HttpGet]
        public async Task<ActionResult<OrderOnline>> PlaceWepayOrderSimple(double amount, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey.Trim());
            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;
            OrderOnline order = new OrderOnline()
            {
                open_id = user.open_id.Trim(),
                shop = "",
                order_price = amount,
                order_real_pay_price = amount,
                final_price = amount,
                staff_open_id = "",
                type = ""
            };
            await _context.OrderOnline.AddAsync(order);
            await _context.SaveChangesAsync();
            OrderPayment payment = new OrderPayment()
            {
                order_id = order.id,
                mch_id = 1,
                amount = order.final_price,
                open_id = user.open_id,
                pay_method = "微信支付"
            };
            await _context.orderPayment.AddAsync(payment);
            await _context.SaveChangesAsync();
            return await GetWholeOrder(order.id, sessionKey);
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
            OrderPayment payment = await _context.orderPayment.FindAsync(paymentId);
            if (payment == null)
            {
                return BadRequest();
            }
            OrderOnline order = await _context.OrderOnline.FindAsync(payment.order_id);
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
            WepayKey key = await _context.WepayKeys.FindAsync(mchid);

            var certManager = new InMemoryCertificateManager();
            var options = new WechatTenpayClientOptions()
            {
                MerchantId = key.mch_id.Trim(),
                MerchantV3Secret = "",
                MerchantCertificateSerialNumber = key.key_serial.Trim(),
                MerchantCertificatePrivateKey = key.private_key.Trim(),
                PlatformCertificateManager = certManager
            };

            string desc = "未知商品";

            

            order.open_id = user.open_id.Trim();
            _context.Entry(order).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            string notifyUrl = "https://mini.snowmeet.top/core/OrderPayment/TenpayPaymentCallBack/" + mchid.ToString();
            string? outTradeNo = payment.out_trade_no;
            if (outTradeNo == null || outTradeNo.Length != 20)
            {
                outTradeNo = order.id.ToString().PadLeft(6, '0') + payment.id.ToString().PadLeft(2, '0') + timeStamp.Substring(3, 10);
            }

            var client = new WechatTenpayClient(options);
            var request = new CreatePayTransactionJsapiRequest()
            {
                OutTradeNumber = outTradeNo,
                AppId = _appId,
                Description = desc.Trim(),//wepayOrder.description.Trim().Equals("") ? "测试商品" : wepayOrder.description.Trim(),
                ExpireTime = DateTimeOffset.Now.AddMinutes(30),
                NotifyUrl = notifyUrl,//wepayOrder.notify.Trim() + "/" + mchid.ToString(),
                Amount = new CreatePayTransactionJsapiRequest.Types.Amount()
                {
                    Total = (int)Math.Round(payment.amount * 100, 0)
                },
                Payer = new CreatePayTransactionJsapiRequest.Types.Payer()
                {
                    OpenId = user.open_id.Trim()
                },
                GoodsTag = "testing goods tag"
            };


            var response = await client.ExecuteCreatePayTransactionJsapiAsync(request);
            var paraMap = client.GenerateParametersForJsapiPayRequest(request.AppId, response.PrepayId);
            if (response != null && response.PrepayId != null && !response.PrepayId.Trim().Equals(""))
            {
                TenpaySet set = new TenpaySet()
                {
                    prepay_id = response.PrepayId.Trim(),
                    timeStamp = paraMap["timeStamp"].Trim(),
                    nonce = paraMap["nonceStr"].Trim(),
                    sign = paraMap["paySign"].Trim()

                };

                payment.mch_id = mchid;
                payment.open_id = user.open_id.Trim();
                payment.app_id = _appId;
                payment.notify = notifyUrl.Trim();
                payment.nonce = set.nonce.Trim();
                payment.sign = set.sign.Trim();
                payment.out_trade_no = outTradeNo;
                payment.prepay_id = set.prepay_id.Trim();
                payment.timestamp = set.timeStamp.Trim();
                _context.Entry(payment).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return Ok(set);
            }
            return BadRequest();
        }


        private bool OrderOnlineExists(int id)
        {
            return (_context.OrderOnline?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}

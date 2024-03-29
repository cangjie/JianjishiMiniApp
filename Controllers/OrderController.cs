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
//using SKIT.FlurlHttpClient.Wechat.TenpayV2;
using MiniApp.Models;
using System.Text;
using MiniApp.Models.Card;
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
        private readonly CardController _cardHelper;

        public OrderController(SqlServerContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _userHelper = new MiniUserController(context, config);
            _appId = _config.GetSection("Settings").GetSection("AppId").Value.Trim();
            _cardHelper = new CardController(context, config);
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

        [NonAction]
        public async Task<OrderOnline> GetOrder(int orderId)
        {
            OrderOnline order = await _context.OrderOnline.FindAsync(orderId);
            order.payments = await _context.orderPayment.Where(p => p.order_id == orderId).ToArrayAsync();
            return order;
        }

        [NonAction]
        public async Task<OrderOnline> GetWholeOrder(int id)
        {
            
            OrderOnline order = await _context.OrderOnline.FindAsync(id);
            
            order.payments = await _context.orderPayment.Where(p => p.order_id == id).ToArrayAsync();
            order.refunds = await _context.orderPaymentRefund.Where(r => r.order_id == order.id)
                .AsNoTracking().ToArrayAsync();
            return order;

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
            return await GetWholeOrder(order.id);
        }

        [HttpGet("{orderId}")]
        public async Task<ActionResult<TenpaySet>> PayOrder(int orderId, string sessionKey)
        {
            sessionKey = Util.UrlDecode(sessionKey.Trim());
            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;
            OrderOnline order = await _context.OrderOnline.FindAsync(orderId);
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
            return await TenpayRequest(payment.id, sessionKey, true);
        }

        
        [HttpGet("{paymentId}")]
        public async Task<ActionResult<TenpaySet>> TenpayRequest(int paymentId, string sessionKey, bool profitShare)
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

            string notifyUrl = "https://mini.spineguard.cn/api/Order/TenpayCallBack/" + mchid.ToString();
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
                GoodsTag = "testing goods tag",
                Settlement = new CreatePayTransactionJsapiRequest.Types.Settlement()
                {
                    IsProfitSharing = profitShare
                }
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

        [HttpPost("{mchid}")]
        public async Task<ActionResult<string>> TenpayCallback(int mchid,
            [FromHeader(Name = "Wechatpay-Timestamp")] string timeStamp,
            [FromHeader(Name = "Wechatpay-Nonce")] string nonce,
            [FromHeader(Name = "Wechatpay-Signature")] string paySign,
            [FromHeader(Name = "Wechatpay-Serial")] string serial)
        {
            var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string postJson = await reader.ReadToEndAsync();

            string apiKey = "";
            WepayKey key = _context.WepayKeys.Find(mchid);

            if (key == null)
            {
                return NotFound();
            }

            apiKey = key.api_key.Trim();

            if (apiKey == null || apiKey.Trim().Equals(""))
            {
                return NotFound();
            }
            string path = $"{Environment.CurrentDirectory}";
    
            if (path.StartsWith("/"))
            {
                path = path + "/WepayCertificate/";
            }
            else
            {
                path = path + "\\WepayCertificate\\";
            }
            string dateStr = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString().PadLeft(2, '0')
                + DateTime.Now.Day.ToString().PadLeft(2, '0');
            //path = path + "callback_" +  + ".txt";
            // 此文本只添加到文件一次。
            using (StreamWriter fw = new StreamWriter(path + "callback_origin_" + dateStr + ".txt", true))
            {
                fw.WriteLine(DateTimeOffset.Now.ToString());
                fw.WriteLine(serial);
                fw.WriteLine(timeStamp);
                fw.WriteLine(nonce);
                fw.WriteLine(paySign);
                fw.WriteLine(postJson);
                fw.WriteLine("");
                fw.WriteLine("--------------------------------------------------------");
                fw.WriteLine("");
                fw.Close();
            }

            try
            {
                string cerStr = "";
                using (StreamReader sr = new StreamReader(path + serial.Trim() + ".pem", true))
                {
                    cerStr = sr.ReadToEnd();
                    sr.Close();
                }
                CertificateEntry ce = new CertificateEntry("RSA", cerStr);

                //CertificateEntry ce = new CertificateEntry()

                var manager = new InMemoryCertificateManager();
                manager.AddEntry(ce);
                var options = new WechatTenpayClientOptions()
                {
                    MerchantId = key.mch_id.Trim(),
                    MerchantV3Secret = apiKey,
                    MerchantCertificateSerialNumber = key.key_serial,
                    MerchantCertificatePrivateKey = key.private_key,
                    PlatformCertificateManager = manager

                };

                var client = new WechatTenpayClient(options);
                Exception? verifyErr;
                bool valid = client.VerifyEventSignature(timeStamp, nonce, postJson, paySign, serial, out verifyErr);
        
                if (valid)
                {
                    var callbackModel = client.DeserializeEvent(postJson);
                    if ("TRANSACTION.SUCCESS".Equals(callbackModel.EventType))
                    {
                        /* 根据事件类型，解密得到支付通知敏感数据 */

                        var callbackResource = client.DecryptEventResource<SKIT.FlurlHttpClient.Wechat.TenpayV3.Events.TransactionResource>(callbackModel);
                        string outTradeNumber = callbackResource.OutTradeNumber;
                        string transactionId = callbackResource.TransactionId;
                        string callbackStr = Newtonsoft.Json.JsonConvert.SerializeObject(callbackResource);
                        try
                        {
                            using (StreamWriter sw = new StreamWriter(path + "callback_decrypt_" + dateStr + ".txt", true))
                            {
                                sw.WriteLine(DateTimeOffset.Now.ToString());
                                sw.WriteLine(callbackStr);
                                sw.WriteLine("");
                                sw.Close();
                            }
                        }
                        catch
                        {

                        }
                        await SetTenpayPaymentSuccess(outTradeNumber);

                        //Console.WriteLine("订单 {0} 已完成支付，交易单号为 {1}", outTradeNumber, transactionId);
                    }
                }

            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
            return "{ \r\n \"code\": \"SUCCESS\", \r\n \"message\": \"成功\" \r\n}";
        }

        [NonAction]
        public async  Task SetTenpayPaymentSuccess(string outTradeNumber)
        {
            var pl = await _context.orderPayment.Where(p => p.out_trade_no.Trim().Equals(outTradeNumber.Trim())).ToListAsync();
            if (pl == null || pl.Count == 0)
            {
                return;
            }
            OrderPayment payment = pl[0];
            payment.status = "支付成功";
            _context.orderPayment.Entry(payment).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            OrderOnline order = await _context.OrderOnline.FindAsync(payment.order_id);
            if (order == null)
            {
                return;
            }
            order.pay_state = 1;
            order.pay_time = DateTime.Now;
            _context.OrderOnline.Entry(order).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            if (order.product_id != null)
            {
                Card? card = await _cardHelper.CreateCard((int)order.product_id);
                if (card != null)
                {
                    card.open_id = order.open_id;
                    card.order_id = order.id;
                    card.avaliable = 1;
                    _context.Card.Entry(card).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
            }
        }

        /*
        [NonAction]
        public async Task CreateCard(OrderOnline order)
        {
            Product p = await _context.product.FindAsync(order.product_id);
            Card card = new Card()
            {
                id = 0,
                open_id = order.open_id.Trim(),
                order_id 
            }
        }
        */
        //[HttpGet("{paymentId}")]
        //public async Task<ActionResult<OrderPaymentRefund>> TenpayRefund(int paymentId, double amount, string sessionKey)
        [NonAction]
        public async Task<OrderPaymentRefund> TenpayRefund(int paymentId, double amount, string memo, MiniUser user)
        {

            OrderPayment payment = await _context.orderPayment.FindAsync(paymentId);
            if (!payment.pay_method.Trim().Equals("微信支付"))
            {

                return null;
            }


            string notify = payment.notify.Trim().Replace("TenpayCallBack", "TenpayRefundCallback");

            
            double refundedAmount = await _context.orderPaymentRefund.Where(r => (r.payment_id == paymentId && r.state == 1)).SumAsync(s => s.amount);


            if (refundedAmount >= payment.amount || refundedAmount + amount > payment.amount)
            {

                return null;
            }


            OrderPaymentRefund refund = new OrderPaymentRefund()
            {
                order_id = payment.order_id,
                payment_id = payment.id,
                amount = amount,
                oper = user.open_id.Trim(),
                state = 0,
                memo = memo,
                notify_url = notify.Trim()


            };

            await _context.orderPaymentRefund.AddAsync(refund);
            await _context.SaveChangesAsync();
            //var client = new WechatTenpayClient(options);
            // orderHelper = new OrderOnlinesController(_db, _originConfig);
            OrderOnline order = (OrderOnline)await GetWholeOrder(payment.order_id);

            WepayKey key = await _context.WepayKeys.FindAsync(payment.mch_id);

            var certManager = new InMemoryCertificateManager();
            var options = new WechatTenpayClientOptions()
            {
                MerchantId = key.mch_id.Trim(),
                MerchantV3Secret = "",
                MerchantCertificateSerialNumber = key.key_serial.Trim(),
                MerchantCertificatePrivateKey = key.private_key.Trim(),
                PlatformCertificateManager = certManager
            };

            var client = new WechatTenpayClient(options);
            var request = new CreateRefundDomesticRefundRequest()
            {
                OutTradeNumber = payment.out_trade_no.Trim(),
                OutRefundNumber = refund.id.ToString(),
                Amount = new CreateRefundDomesticRefundRequest.Types.Amount()
                {
                    Total = (int)Math.Round(payment.amount * 100, 0),
                    Refund = (int)(Math.Round(amount * 100))
                },
                Reason = user.open_id.Trim(),

                NotifyUrl = refund.notify_url.Trim()
            };
            var response = await client.ExecuteCreateRefundDomesticRefundAsync(request);
            try
            {
                string refundId = response.RefundId.Trim();
                if (refundId == null || refundId.Trim().Equals(""))
                {
                    return refund;
                }
                //refund.status = refundId;
                refund.refund_id = refundId;
                _context.Entry<OrderPaymentRefund>(refund).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                //await Response.WriteAsync("SUCCESS");
                return refund;
            }
            catch
            {
                refund.memo = response.ErrorMessage.Trim();
                _context.Entry<OrderPaymentRefund>(refund).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return refund;
            }


        }
        

        [HttpPost("{mchid}")]
        public async Task<ActionResult<string>> TenpayRefundCallback(int mchid,
            [FromHeader(Name = "Wechatpay-Timestamp")] string timeStamp,
            [FromHeader(Name = "Wechatpay-Nonce")] string nonce,
            [FromHeader(Name = "Wechatpay-Signature")] string paySign,
            [FromHeader(Name = "Wechatpay-Serial")] string serial,
            [FromBody] object postData)
        {
            /*
            string paySign = _httpContextAccessor.HttpContext.Request.Headers["Wechatpay-Signature"].ToString();
            string nonce = _httpContextAccessor.HttpContext.Request.Headers["Wechatpay-Nonce"].ToString();
            string serial = _httpContextAccessor.HttpContext.Request.Headers["Wechatpay-Serial"].ToString();
            string timeStamp = _httpContextAccessor.HttpContext.Request.Headers["Wechatpay-Timestamp"].ToString();
            */

            string path = $"{Environment.CurrentDirectory}";
            if (path.StartsWith("/"))
            {
                path = path + "/WepayCertificate/";
            }
            else
            {
                path = path + "\\WepayCertificate\\";
            }
            string dateStr = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString().PadLeft(2, '0')
                + DateTime.Now.Day.ToString().PadLeft(2, '0');
            //string postJson = Newtonsoft.Json.JsonConvert.SerializeObject(postData);
            //path = path + "callback_" +  + ".txt";
            // 此文本只添加到文件一次。
            using (StreamWriter fw = new StreamWriter(path + "callback_origin_refund_" + dateStr + ".txt", true))
            {
                await fw.WriteLineAsync(DateTimeOffset.Now.ToString());


                await fw.WriteLineAsync(paySign);
                await fw.WriteLineAsync(nonce);
                await fw.WriteLineAsync(serial);
                await fw.WriteLineAsync(timeStamp);
                await fw.WriteLineAsync(postData.ToString());
                await fw.WriteLineAsync("--------------------------------------------------------");
                await fw.WriteLineAsync("");
                fw.Close();
            }




            string cerStr = "";
            using (StreamReader sr = new StreamReader(path + serial.Trim() + ".pem", true))
            {
                cerStr = sr.ReadToEnd();
                sr.Close();
            }


            string apiKey = "";
            WepayKey key = _context.WepayKeys.Find(mchid);

            if (key == null)
            {
                return NotFound();
            }

            apiKey = key.api_key.Trim();

            var certManager = new InMemoryCertificateManager();

            CertificateEntry ce = new CertificateEntry("RSA", serial, cerStr, DateTimeOffset.MinValue, DateTimeOffset.MaxValue);


            certManager.AddEntry(ce);
            //certManager.SetCertificate(serial, cerStr);
            var options = new WechatTenpayClientOptions()
            {
                MerchantV3Secret = apiKey,
                PlatformCertificateManager = certManager

            };

            var client = new WechatTenpayClient(options);
            bool valid = client.VerifyEventSignature(timeStamp, nonce, postData.ToString(), paySign, serial);

            if (valid)
            {
                var callbackModel = client.DeserializeEvent(postData.ToString());
                if ("REFUND.SUCCESS".Equals(callbackModel.EventType))
                {
                    try
                    {
                        var callbackResource = client.DecryptEventResource<SKIT.FlurlHttpClient.Wechat.TenpayV3.Events.TransactionResource>(callbackModel);
                        OrderPayment payment = await _context.orderPayment
                            .Where(p => p.out_trade_no.Trim().Equals(callbackResource.OutTradeNumber)
                            //&& p.pay_method.Trim().Equals("微信支付") && p.status.Trim().Equals("支付成功")
                            )
                            .OrderByDescending(p => p.id).FirstAsync();


                        double refundAmount = Math.Round(((double)callbackResource.Amount.Total) / 100, 2);
                        OrderPaymentRefund refund = await _context.orderPaymentRefund.Where(r => (r.payment_id == payment.id
                         && r.amount == refundAmount && r.state == 0)).FirstAsync();

                        refund.state = 1;
                        refund.memo = callbackResource.TransactionId;
                        _context.Entry(refund).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                    catch(Exception err)
                    {
                        Console.WriteLine(err);
                    }




                }

            }
            return "{ \r\n \"code\": \"SUCCESS\", \r\n \"message\": \"成功\" \r\n}";
        }

        [HttpGet]
        public async Task<ActionResult<int>> TestProfitShare(string outTradeNo)
        {
            WepayKey key = await _context.WepayKeys.FindAsync(1);
            string serial = "4F039BEC7B97A18FCC904EE339F37AFFE2B93BDD";
            string path = $"{Environment.CurrentDirectory}";
            if (path.StartsWith("/"))
            {
                path = path + "/WepayCertificate/";
            }
            else
            {
                path = path + "\\WepayCertificate\\";
            }

            string cerStr = "";
            using (StreamReader sr = new StreamReader(path + serial.Trim() + ".pem", true))
            {
                cerStr = sr.ReadToEnd();
                sr.Close();
            }

            var certManager = new InMemoryCertificateManager();
            CertificateEntry ce = new CertificateEntry("RSA", serial, cerStr, DateTimeOffset.MinValue, DateTimeOffset.MaxValue);


            certManager.AddEntry(ce);

            var options = new WechatTenpayClientOptions()
            {
                MerchantId = key.mch_id.Trim(),
                MerchantV3Secret = "",
                MerchantCertificateSerialNumber = key.key_serial.Trim(),
                MerchantCertificatePrivateKey = key.private_key.Trim()
                //PlatformCertificateManager = certManager,
               
            };
            List<CreateProfitSharingOrderRequest.Types.Receiver> rl = new List<CreateProfitSharingOrderRequest.Types.Receiver>();
            CreateProfitSharingOrderRequest.Types.Receiver r = new CreateProfitSharingOrderRequest.Types.Receiver()
            {
                Type = "PERSONAL_OPENID",
                Account = "o5anv5cQovtIVp4Cz6T2Gp2KB4Po",
                Amount = 1,
                Description = "测试"
            };


            rl.Add(r);
            var req = new CreateProfitSharingOrderRequest()
            {
                AppId = _appId,
                TransactionId = "4200002099202401057821533358",
                OutOrderNumber = "2401052334",
                ReceiverList = rl,
                WechatpayCertificateSerialNumber = serial
            };

            var addRecReq = new AddProfitSharingReceiverRequest()
            {
                AppId = _appId,
                Type = "PERSONAL_OPENID",
                Account = "o5anv5cQovtIVp4Cz6T2Gp2KB4Po",
                RelationType = "USER"
            };

            var client = new WechatTenpayClient(options);
            var addres = await client.ExecuteAddProfitSharingReceiverAsync(addRecReq);
            
            var res = await client.ExecuteCreateProfitSharingOrderAsync(req);

            
            return Ok(0);
        }


        private bool OrderOnlineExists(int id)
        {
            return (_context.OrderOnline?.Any(e => e.id == id)).GetValueOrDefault();
        }
    }
}

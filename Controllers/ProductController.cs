using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp.Models;
using MiniApp.Models.Order;

namespace MiniApp.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductController:ControllerBase
	{
        private readonly SqlServerContext _context;
        private readonly IConfiguration _config;
        private readonly MiniUserController _userHelper;
        private readonly OrderController _orderHelper;

        public ProductController(SqlServerContext context, IConfiguration config)
		{
            _context = context;
            _config = config;
            _userHelper = new MiniUserController(context, config);
        }

        [HttpGet("{productId}")]
        public async Task<ActionResult<OrderOnline>> PlaceOrder(int productId, string sessionKey)
        {
            Product p = await _context.product.FindAsync(productId);
            if (p == null)
            {
                return NotFound();
            }
            sessionKey = Util.UrlDecode(sessionKey.Trim());
            MiniUser user = (MiniUser)((OkObjectResult)(await _userHelper.GetBySessionKey(sessionKey)).Result).Value;
            OrderOnline order = new OrderOnline()
            {
                open_id = user.open_id.Trim(),
                shop = "",
                order_price = p.sale_price,
                order_real_pay_price = p.sale_price,
                final_price = p.sale_price,
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
            return await _orderHelper.GetWholeOrder(order.id);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetReserveProductByRegion(string region)
        {
            return Ok(await _context.product
                .Where(p => p.type.Trim().Equals("单次预约") && p.region.Trim().Equals(region.Trim()))
                .AsNoTracking().ToListAsync());
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetCardProductByRegion(string region)
        {
            return Ok(await _context.product
                .Where(p =>(p.type.Trim().Equals("次卡") || p.type.Trim().Equals("季卡")
                || p.type.Trim().Equals("储值卡")) && p.region.Trim().Equals(region.Trim()))
                .AsNoTracking().ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetSingleProduct(int id)
        {
            return Ok(await _context.product.FindAsync(id));
        }
	}
}


using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniApp.Models;

namespace MiniApp.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductController:ControllerBase
	{
        private readonly SqlServerContext _context;
        private readonly IConfiguration _config;

        public ProductController(SqlServerContext context, IConfiguration config)
		{
            _context = context;
            _config = config;
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


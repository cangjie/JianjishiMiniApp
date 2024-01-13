using System;
using Microsoft.EntityFrameworkCore;
using MiniApp.Models;
using MiniApp.Models.Order;
using MiniApp.Models.Card;


namespace MiniApp
{
	public class SqlServerContext: DbContext
	{
        public SqlServerContext(DbContextOptions<SqlServerContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MiniSession>().HasKey(c => new { c.original_id, c.session_key });
        }

        public DbSet<MiniUser> miniUser { get; set; }
        public DbSet<MiniSession> miniSession { get; set; }
        public DbSet<MiniApp.Models.Shop>? Shop { get; set; }
        public DbSet<MiniApp.Models.TimeTable>? timeTable { get; set; }
        public DbSet<MiniApp.Models.Reserve>? reserve { get; set; }
        public DbSet<InformList> informList { get; set; }
        public DbSet<MiniApp.Models.Order.OrderOnline> OrderOnline { get; set; } 
        public DbSet<OrderPayment> orderPayment { get; set; }
        public DbSet<OrderPaymentRefund> orderPaymentRefund { get; set; }
        public DbSet<WepayKey> WepayKeys { get; set; }
        public DbSet<Product> product {get; set;}
        public DbSet<Therapeutist> therapuetist {get; set;}
        public DbSet<TherapeutistTimeTable> therapeutistTimeTable {get; set;}
        public DbSet<MiniApp.Models.Card.Card> Card { get; set; } = default!;

    }
}


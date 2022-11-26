using System;
using Microsoft.EntityFrameworkCore;
using MiniApp.Models;
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
    }
}


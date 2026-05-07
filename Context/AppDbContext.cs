using eCertify.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace eCertify.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}

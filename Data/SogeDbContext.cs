using Microsoft.EntityFrameworkCore;
using eCertify.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace eCertify.Data
{
    public class SogeDbContext : IdentityDbContext<IdentityUser>
    {
        public SogeDbContext(DbContextOptions<SogeDbContext> options)
            : base(options)
        {
        }

        public DbSet<Empresa> Empresas { get; set; }
        public DbSet<User> AppUsers { get; set; }
        public DbSet<Provincia> Provincias { get; set; }
        public DbSet<Municipio> Municipios { get; set; }
        public DbSet<HistorialPruebasExcel> HistorialPruebasExcel { get; set; }
        public DbSet<HistorialFacturas> HistorialFacturas { get; set; }
        public DbSet<PasoCompletado> PasosCompletados { get; set; }
        public DbSet<HistorialPago> HistorialPagos { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Mapa explícito para evitar colisión con Identity's DbSet<IdentityUser> Users
            modelBuilder.Entity<User>().ToTable("Users").HasKey(u => u.ID);
            modelBuilder.Entity<Municipio>()
                .HasOne(m => m.Provincia)
                .WithMany(p => p.Municipios)
                .HasForeignKey(m => m.Prov_Id);


            // Relación HistorialPago → User
            modelBuilder.Entity<HistorialPago>()
                .HasOne(h => h.User)
                .WithMany() 
                .HasForeignKey(h => h.UserId)
                .HasPrincipalKey(u => u.ID)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}

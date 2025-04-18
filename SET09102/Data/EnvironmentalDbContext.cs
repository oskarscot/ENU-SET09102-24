using Microsoft.EntityFrameworkCore;
using SET09102.Models;
using System.IO;

namespace SET09102.Data;

public class EnvironmentalDbContext : DbContext
{
    public DbSet<SensorData> SensorData { get; set; }
    public DbSet<InvalidDataLog> InvalidDataLog { get; set; }

    public EnvironmentalDbContext()
    {
    }

    public EnvironmentalDbContext(DbContextOptions<EnvironmentalDbContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SET09102.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SensorData>()
            .HasKey(s => s.SensorID);

        modelBuilder.Entity<InvalidDataLog>()
            .HasKey(l => l.LogID);
    }
} 
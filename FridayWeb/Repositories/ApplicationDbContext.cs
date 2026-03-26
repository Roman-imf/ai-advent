using Microsoft.EntityFrameworkCore;

namespace FridayWeb.Repositories;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Примеры DbSet для сущностей
    public DbSet<ChatMessage> Messages { get; set; }
    public DbSet<FridaySettings> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Настройка моделей (опционально)
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("message_history", "friday");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Input).HasColumnName("input").IsRequired();
            entity.Property(e => e.Output).HasColumnName("output").IsRequired();
            entity.Property(e => e.InputTokens).HasColumnName("token_input").IsRequired();
            entity.Property(e => e.OutputTokens).HasColumnName("token_output").IsRequired();
            entity.Property(e => e.Model).HasConversion<string>().HasColumnName("model").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at")
                .HasConversion(x => x.ToUniversalTime(), x => x).IsRequired();
        });
        
        modelBuilder.Entity<FridaySettings>(entity =>

        {
            entity.ToTable("friday_settings", "friday");
            
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(x => x.Model).HasColumnName("model").HasConversion<string>().IsRequired();
            entity.Property(x => x.Temperature).HasColumnName("temperature").IsRequired();
            entity.Property(x => x.MaxTokens).HasColumnName("max_tokens").IsRequired();
        });
        
        
    }
}
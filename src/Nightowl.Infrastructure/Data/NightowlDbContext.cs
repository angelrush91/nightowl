using Microsoft.EntityFrameworkCore;
using Nightowl.Domain.Entities;
using Nightowl.Domain.ValueObjects;

namespace Nightowl.Infrastructure.Data;

public class NightowlDbContext : DbContext
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<ReadingSession> ReadingSessions => Set<ReadingSession>();

    public NightowlDbContext(DbContextOptions<NightowlDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Book>(builder =>
        {
            builder.ToTable("Books");
            builder.HasKey(b => b.Id);

            builder.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(b => b.Subtitle)
                .HasMaxLength(500);

            builder.Property(b => b.Authors)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(b => b.Isbn)
                .HasConversion(
                    isbn => isbn.Value,
                    str => new Isbn(str))
                .HasMaxLength(13)
                .IsRequired();

            builder.HasIndex(b => b.Isbn)
                .IsUnique();

            builder.ComplexProperty(b => b.Progress, progress =>
            {
                progress.Property(p => p.CurrentPage).HasColumnName("CurrentPage");
                progress.Property(p => p.TotalPages).HasColumnName("TotalPages");
            });

            builder.Property(b => b.Status)
                .IsRequired();

            builder.Property(b => b.DateAdded)
                .IsRequired();

            builder.Property(b => b.Publisher)
                .HasMaxLength(250);

            builder.Property(b => b.PublishedDate)
                .HasMaxLength(50);

            builder.Property(b => b.CoverUrl)
                .HasMaxLength(1000);

            builder.Property(b => b.Description)
                .HasMaxLength(4000);

            builder.Property(b => b.Review)
                .HasMaxLength(4000);

            builder.HasMany(b => b.ReadingSessions)
                .WithOne()
                .HasForeignKey(s => s.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(b => b.ReadingSessions)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ReadingSession>(builder =>
        {
            builder.ToTable("ReadingSessions");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.BookId)
                .IsRequired();

            builder.Property(s => s.SessionDate)
                .IsRequired();

            builder.Property(s => s.StartPage)
                .IsRequired();

            builder.Property(s => s.EndPage)
                .IsRequired();

            builder.Property(s => s.Notes)
                .HasMaxLength(2000);

            builder.Ignore(s => s.PagesRead);
        });
    }
}

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetRoll.Models;

namespace NetRoll.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

    public DbSet<EmailSettings> EmailSettings { get; set; } = default!;
    public DbSet<CategoryTree> CategoryTrees { get; set; } = default!;
    public DbSet<Category> Categories { get; set; } = default!;
    public DbSet<CategoryTranslation> CategoryTranslations { get; set; } = default!;
    public DbSet<Language> Languages { get; set; } = default!;
    public DbSet<ImageAsset> ImageAssets { get; set; } = default!;
    public DbSet<UserImageSettings> UserImageSettings { get; set; } = default!;
    public DbSet<UserSetting> UserSettings { get; set; } = default!;
    public DbSet<UserUsage> UserUsages { get; set; } = default!;
    public DbSet<NetRoll.Models.PlanChangeRequest> PlanChangeRequests { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<NetRoll.Models.Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<NetRoll.Models.CategoryTranslation>()
                .HasIndex(t => new { t.CategoryId, t.LanguageCode })
                .IsUnique();

            builder.Entity<NetRoll.Models.Language>()
                .HasIndex(l => new { l.OwnerUserId, l.Code })
                .IsUnique();

            builder.Entity<NetRoll.Models.ImageAsset>()
                .HasIndex(i => new { i.OwnerUserId, i.FileName })
                .IsUnique();

            builder.Entity<NetRoll.Models.UserImageSettings>()
                .HasIndex(s => s.OwnerUserId)
                .IsUnique();

            builder.Entity<NetRoll.Models.UserSetting>()
                .HasIndex(s => new { s.OwnerUserId, s.Key })
                .IsUnique();

            // UserUsage: OwnerUserId primary key (one aggregate row per user)
            builder.Entity<UserUsage>()
                .HasKey(u => u.OwnerUserId);

            builder.Entity<NetRoll.Models.PlanChangeRequest>()
                .HasIndex(r => new { r.UserId, r.Status, r.CreatedUtc });
        }
    }
}

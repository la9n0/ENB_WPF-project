using Microsoft.EntityFrameworkCore;

namespace ENB_project
{
    public class AppDbContext : DbContext
    {
        public DbSet<UserEntity>           Users           { get; set; }
        public DbSet<FileSystemNodeEntity> FileSystemNodes { get; set; }
        public DbSet<NoteContentEntity>    NoteContents    { get; set; }
        public DbSet<CategoryEntity>       Categories      { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlServer(
                "Server=localhost;Database=ENB;Trusted_Connection=True;TrustServerCertificate=True;");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserEntity>(e =>
            {
                e.ToTable("Users");
                e.HasKey(u => u.Id);
                e.HasIndex(u => u.Login).IsUnique();
                e.Property(u => u.Login).HasMaxLength(25).IsRequired();
                e.Property(u => u.Password).HasMaxLength(30).IsRequired();
                e.Property(u => u.Email).HasMaxLength(255).IsRequired();
                e.Property(u => u.Theme).HasMaxLength(20).IsRequired().HasDefaultValue("Dark");
                e.Property(u => u.Language).HasMaxLength(10).IsRequired().HasDefaultValue("ru");
            });

            modelBuilder.Entity<FileSystemNodeEntity>(e =>
            {
                e.ToTable("FileSystemNodes");
                e.HasKey(n => n.Id);
                e.Property(n => n.Name).HasMaxLength(255).IsRequired();
                e.Property(n => n.ItemType).HasMaxLength(10).IsRequired().HasDefaultValue("File");
                e.Property(n => n.SortOrder).IsRequired().HasDefaultValue(0);
                e.Property(n=> n.CreateTime).IsRequired();
                e.Property(n=> n.EditTime).IsRequired();

                e.HasOne(n => n.User)
                 .WithMany(u => u.Nodes)
                 .HasForeignKey(n => n.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(n => n.Parent)
                 .WithMany(n => n.Children)
                 .HasForeignKey(n => n.ParentId)
                 .OnDelete(DeleteBehavior.NoAction)
                 .IsRequired(false);

                e.HasOne(n => n.Category)
                 .WithMany(c => c.Nodes)
                 .HasForeignKey(n => n.CategoryId)
                 .OnDelete(DeleteBehavior.SetNull)
                 .IsRequired(false);
            });

            modelBuilder.Entity<NoteContentEntity>(e =>
            {
                e.ToTable("NoteContents");
                e.HasKey(c => c.Id);
                e.HasIndex(c => c.NodeId).IsUnique();
                e.Property(c => c.Content).IsRequired().HasDefaultValue("");

                e.HasOne(c => c.Node)
                 .WithOne(n => n.NoteContent)
                 .HasForeignKey<NoteContentEntity>(c => c.NodeId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CategoryEntity>(e =>
            {
                e.ToTable("Categories");
                e.HasKey(c => c.Id);
                e.Property(c => c.Name).HasMaxLength(50).IsRequired();
                e.Property(c => c.Color).HasMaxLength(7).IsRequired().HasDefaultValue("#7C6FCD");

                e.HasOne(c => c.User)
                 .WithMany(u => u.Categories)
                 .HasForeignKey(c => c.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

    public class UserEntity
    {
        public int    Id       { get; set; }
        public string Login    { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email    { get; set; } = string.Empty;
        public string Theme    { get; set; } = "Dark";
        public string Language { get; set; } = "ru";

        public List<FileSystemNodeEntity> Nodes      { get; set; } = new();
        public List<CategoryEntity>       Categories { get; set; } = new();
    }

    public class FileSystemNodeEntity
    {
        public int    Id         { get; set; }
        public int    UserId     { get; set; }
        public int?   ParentId   { get; set; }
        public int?   CategoryId { get; set; }
        public string Name       { get; set; } = string.Empty;
        public string ItemType   { get; set; } = "File";
        public int    SortOrder  { get; set; } = 0;
        public DateOnly EditTime { get; set; }
        public DateOnly CreateTime { get; set; }

        public UserEntity                 User        { get; set; } = null!;
        public FileSystemNodeEntity?      Parent      { get; set; }
        public List<FileSystemNodeEntity> Children    { get; set; } = new();
        public NoteContentEntity?         NoteContent { get; set; }
        public CategoryEntity?            Category    { get; set; }
    }

    public class NoteContentEntity
    {
        public int    Id      { get; set; }
        public int    NodeId  { get; set; }
        public string Content { get; set; } = string.Empty;

        public FileSystemNodeEntity Node { get; set; } = null!;
    }

    public class CategoryEntity
    {
        public int    Id     { get; set; }
        public int    UserId { get; set; }
        public string Name   { get; set; } = string.Empty;
        public string Color  { get; set; } = "#7C6FCD";

        public UserEntity                 User  { get; set; } = null!;
        public List<FileSystemNodeEntity> Nodes { get; set; } = new();
    }
}
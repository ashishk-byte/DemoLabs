using DemoWebApiDB.Auth.Entities;

namespace DemoWebApiDB.Data.Data;


/// <summary>
///     Primary EF Core DbContext for the application.
///     Integrates ASP.NET Identity and domain entities.
/// </summary>
/// <remarks>
///     This DataContext class is configured to provide support to work with SQLite (used for "Testing" Environment)
/// </remarks>
/// 


public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{

    /// <summary>
    ///     Flag indicating if the ApplicationDbContext is running SqlLite in "Testing" Environment
    /// </summary>
    public bool IsSqlite 
        => Database.ProviderName?.Contains("Sqlite") == true;           // used by "Testing" Environment

    /// <summary>
    ///     Flag indicating if the ApplicationDbContext is running in non-Testing Environment
    /// </summary>
    public bool IsSqlServer 
        => Database.ProviderName?.Contains("SqlServer") == true;        // used by all other Environments


    // Register the Identity models to be exposed as properties from the DataContext
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }


    public DbSet<Permission> Permissions { get; set; }       // for seeding permissions to the database, used by "Testing" Environment
    public DbSet<RolePermission> RolePermissions { get; set; }       // for seeding role-permission relationships to the database, used by "Testing" Environment

    // Register the models to be exposed as properties from the DataContext
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductWithCategoryReadModel> ProductsWithCategoryView { get; set; }       // mapped to a VIEW


    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        /************* INSTEAD OF DEFINING THE [ForeignKey] Attribute IN the Product model
            // Fluent API version
            modelBuilder
                .Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CtgryId)
                        .OnDelete(DeleteBehavior.Cascade);
         ***********/

        //----------- Define relationships for Identity models

        // Define the Composite Primary Key for RolePermission
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // Define the many-to-many relationship between ApplicationRole and Permission through RolePermission
        //  ------ a role can have many permissions
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany()
            .HasForeignKey(rp => rp.RoleId);
        //  ------ a permission can be assigned to many roles
        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId);


        //----------- Define RowVersion policy for both SQL Server and SQLite

        DefineRowVersionPolicy(modelBuilder);


        //----------- Check Constraints
        modelBuilder.Entity<RolePermission>()
            .HasKey(x => new { x.RoleId, x.PermissionId });


        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(x => x.PermissionId);
        




        if (IsSqlServer)
        {
            modelBuilder.Entity<Category>()
                .ToTable(t => t.HasCheckConstraint(
                    name: "CK_Categories_Name_NotBlank",
                    sql: "LEN(LTRIM(RTRIM(Name))) > 0"));
        }
        else if( IsSqlite )
        {
            modelBuilder.Entity<Category>()
                .ToTable(t => t.HasCheckConstraint(
                    name: "CK_Categories_Name_NotBlank",
                    sql: "LENGTH(TRIM(Name)) > 0"));
        }

        modelBuilder.Entity<Product>()
            .ToTable(t => t.HasCheckConstraint(
                name: "CK_Products_Price_NonNegative",
                sql: "Price >= 0"));

        modelBuilder.Entity<Product>()
            .ToTable(t => t.HasCheckConstraint(
                name: "CK_Products_Qty_NonNegative",
                sql: "QtyInStock IS NULL OR QtyInStock >= 0"));


        //----------- Configure Views

        modelBuilder.Entity<ProductWithCategoryReadModel>()
            .ToView("vw_ProductsWithCategory")
            .HasNoKey();
    }


    public override int SaveChanges()
    {
        ApplyAuditInformation();
        ApplySqliteRowVersionFix();
        return base.SaveChanges();
    }


    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        ApplySqliteRowVersionFix();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        ApplySqliteRowVersionFix();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, 
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        ApplySqliteRowVersionFix();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }


    private void ApplySqliteRowVersionFix()
    {
        // Since SQLite does not have timestamp support for RowVersion, manually generate it.
        if (IsSqlite)
        {
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    entry.Property(nameof(AuditableEntity.RowVersion)).CurrentValue
                        = Guid.NewGuid().ToByteArray();
                }
            }
        }
    }

    private void DefineRowVersionPolicy(ModelBuilder modelBuilder)
    {
        // SQLite cannot auto-generate rowversion
        if (IsSqlite)
        {
            modelBuilder.Entity<Category>()
                        .Property(e => e.RowVersion)
                        .IsRequired()
                        .ValueGeneratedNever();

            modelBuilder.Entity<Product>()
                .Property(e => e.RowVersion)
                .IsRequired()
                .ValueGeneratedNever();
        }
        else if (IsSqlServer)
        {
            // SQL Server behavior (real rowversion)
            modelBuilder.Entity<Category>()
                .Property(e => e.RowVersion)
                .IsRowVersion();

            modelBuilder.Entity<Product>()
                .Property(e => e.RowVersion)
                .IsRowVersion();
        }
    }


    /******************************
     *  Running tests, revealed that that CREATED AT and MODIFIED AT fields were not getting populated automatically.
     *  This helper method called by SaveChanges() mitigates this issue.
     ************/
    private void ApplyAuditInformation()
    {
        var utcNow = DateTime.UtcNow;
        var entries = ChangeTracker.Entries<AuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = utcNow;
                entry.Entity.ModifiedAtUtc = utcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = utcNow;
            }
        }
    }

}

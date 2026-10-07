using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<USERS> USERS => Set<USERS>();
    public DbSet<USER_PASSWORD_HISTORY> USER_PASSWORD_HISTORY => Set<USER_PASSWORD_HISTORY>();
    public DbSet<PASSWORD_POLICY> PASSWORD_POLICY => Set<PASSWORD_POLICY>();
    public DbSet<ROLE_PASSWORD_POLICY> ROLE_PASSWORD_POLICY => Set<ROLE_PASSWORD_POLICY>();
    public DbSet<ROLES> ROLES => Set<ROLES>();
    public DbSet<COMPANY> COMPANY => Set<COMPANY>();
    public DbSet<USER_LOG> USER_LOG => Set<USER_LOG>();
    public DbSet<USER_SESSION> USER_SESSION => Set<USER_SESSION>();
    public DbSet<SECURITY_QUESTION> SECURITY_QUESTION => Set<SECURITY_QUESTION>();
    public DbSet<USER_SECURITY_QUESTION> USER_SECURITY_QUESTION => Set<USER_SECURITY_QUESTION>();
    public DbSet<PASSWORD_RESET_REQUEST> PASSWORD_RESET_REQUEST => Set<PASSWORD_RESET_REQUEST>();
    public DbSet<SECURITY_CONFIG> SECURITY_CONFIG => Set<SECURITY_CONFIG>();

    // roles and rights
    public DbSet<PAGES> PAGES => Set<PAGES>();
    public DbSet<PAGE_ACTIONS> PAGE_ACTIONS => Set<PAGE_ACTIONS>();
    public DbSet<ROLE_RIGHTS> ROLE_RIGHTS => Set<ROLE_RIGHTS>();
    public DbSet<USER_ROLES> USER_ROLES => Set<USER_ROLES>();
    public DbSet<USER_RIGHTS> USER_RIGHTS => Set<USER_RIGHTS>();

    // masters
    public DbSet<EXCISE> EXCISE => Set<EXCISE>();
    public DbSet<LIQUOR_CATEGORY> LIQUOR_CATEGORY => Set<LIQUOR_CATEGORY>();
    public DbSet<SUPPLIER_CODE> SUPPLIER_CODE => Set<SUPPLIER_CODE>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

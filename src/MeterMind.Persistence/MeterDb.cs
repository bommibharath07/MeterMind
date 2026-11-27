using System.Linq.Expressions;
using MeterMind.Application;
using MeterMind.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MeterMind.Persistence;

public sealed class FixedTenant(Guid utilityId) : ITenantContext { public Guid UtilityId => utilityId; public string UserId => "system"; public bool CanOperate => true; }
public sealed class MeterDb(DbContextOptions<MeterDb> options, ITenantContext tenant) : DbContext(options)
{
    public Guid UtilityId => tenant.UtilityId;
    public DbSet<Utility> Utilities => Set<Utility>();
    public DbSet<Meter> Meters => Set<Meter>(); public DbSet<Telemetry> Telemetry => Set<Telemetry>();
    public DbSet<ProfileReading> Readings => Set<ProfileReading>(); public DbSet<Neighbor> Neighbors => Set<Neighbor>();
    public DbSet<RouteChange> Routes => Set<RouteChange>(); public DbSet<Gateway> Gateways => Set<Gateway>();
    public DbSet<Sink> Sinks => Set<Sink>(); public DbSet<Simulation> Simulations => Set<Simulation>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Utility>().HasQueryFilter(u => u.Id == UtilityId);
        foreach (var type in typeof(TenantEntity).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(TenantEntity))))
        {
            var e = b.Entity(type); e.HasKey("Id"); if (type != typeof(Telemetry) && type != typeof(ProfileReading)) e.HasAlternateKey("UtilityId", "Id");
            e.Property("Id").ValueGeneratedNever();
            var p = Expression.Parameter(type, "e");
            e.HasQueryFilter(Expression.Lambda(Expression.Equal(Expression.Property(p, "UtilityId"), Expression.Property(Expression.Constant(this), nameof(UtilityId))), p));
            e.HasOne(typeof(Utility)).WithMany().HasForeignKey("UtilityId").OnDelete(DeleteBehavior.Restrict);
        }
        b.Entity<Meter>().HasIndex(x => new { x.UtilityId, x.Serial }).IsUnique();
        b.Entity<Meter>().HasIndex(x => new { x.UtilityId, x.NodeAddress, x.NetworkId }).IsUnique();
        b.Entity<Meter>().HasIndex(x => new { x.UtilityId, x.Rssi }); b.Entity<Meter>().HasIndex(x => new { x.UtilityId, x.Latitude, x.Longitude });
        Link<Meter, Sector>(b, "SectorId"); Link<Meter, MeshNetwork>(b, "NetworkId"); Link<Meter, Sink>(b, "SinkId");
        Link<Sink, Gateway>(b, "GatewayId"); Link<Sink, MeshNetwork>(b, "NetworkId");
        foreach (var type in new[] { typeof(Telemetry), typeof(ProfileReading), typeof(Neighbor), typeof(RouteChange), typeof(MeterEvent), typeof(CommunicationAttempt), typeof(DiagnosticRecord), typeof(Incident), typeof(WorkOrder) })
        {
            b.Entity(type).HasOne(typeof(Meter)).WithMany().HasForeignKey("UtilityId", "MeterId").HasPrincipalKey("UtilityId", "Id").OnDelete(DeleteBehavior.Restrict);
        }
        Link<Neighbor, Meter>(b, "NeighborId");
        b.Entity<Neighbor>().HasIndex(x => new { x.UtilityId, x.MeterId, x.NeighborId }).IsUnique();
        b.Entity<Telemetry>().HasKey(x => new { x.UtilityId, x.MeterId, x.Timestamp });
        b.Entity<Telemetry>().HasIndex(x => new { x.UtilityId, x.Timestamp });
        b.Entity<ProfileReading>().HasKey(x => new { x.UtilityId, x.MeterId, x.Profile, x.Timestamp });
        b.Entity<RouteChange>().HasIndex(x => new { x.UtilityId, x.MeterId, x.Timestamp }).IsUnique();
        b.Entity<MeterEvent>().HasIndex(x => new { x.UtilityId, x.MeterId, x.Profile, x.Timestamp }).IsUnique();
        b.Entity<CommunicationAttempt>().HasIndex(x => new { x.UtilityId, x.MeterId, x.Profile, x.Timestamp }).IsUnique();
        b.Entity<GeneratedReport>().HasIndex(x => new { x.UtilityId, x.Status });
        b.Entity<Simulation>().HasIndex(x => x.UtilityId).IsUnique();
        b.Entity<ProfileDefinition>().HasIndex(x => new { x.UtilityId, x.Key }).IsUnique();
        b.Entity<ObisObject>().HasIndex(x => new { x.UtilityId, x.Code }).IsUnique();
        b.Entity<ToolExecution>().HasOne<Conversation>().WithMany().HasForeignKey(x => new { x.UtilityId, x.ConversationId }).HasPrincipalKey(x => new { x.UtilityId, x.Id });
        // Id is descriptive on partitioned measurements, never a cross-table foreign key.
    }
    static void Link<T, P>(ModelBuilder b, string property) where T : TenantEntity where P : TenantEntity => b.Entity<T>().HasOne<P>().WithMany().HasForeignKey("UtilityId", property).HasPrincipalKey("UtilityId", "Id").OnDelete(DeleteBehavior.Restrict);
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<TenantEntity>().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Entity.UtilityId != UtilityId) throw new UnauthorizedAccessException("Utility mismatch.");
            if (entry.State == EntityState.Modified && entry.Property(x => x.UtilityId).IsModified) throw new UnauthorizedAccessException("Utility identity is immutable.");
        }
        return base.SaveChangesAsync(ct);
    }
}
public sealed class DesignFactory : IDesignTimeDbContextFactory<MeterDb>
{
    public MeterDb CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MeterDb>().UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__MeterDb") ?? "Host=localhost;Port=55432;Database=metermind;Username=metermind;Password=local-only").Options, new FixedTenant(Demo.UtilityId));
}

using Microsoft.EntityFrameworkCore;
using YMS.Server.Models;

namespace YMS.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<OpenLabAuth> OpenLabAuths => Set<OpenLabAuth>();
    public DbSet<ApprovalNotification> ApprovalNotifications => Set<ApprovalNotification>();
    public DbSet<UiSearchHistory> UiSearchHistories => Set<UiSearchHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var isSqlite = Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;
        var currentTimestampSql = isSqlite ? "CURRENT_TIMESTAMP" : "SYSTIMESTAMP";

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.ToTable("DDB_EQUIPMENT_MST");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LineId).HasMaxLength(20).IsRequired();
            entity.Property(e => e.LargeClass).HasMaxLength(20).IsRequired();
            entity.Property(e => e.EqpType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EqpId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.EqpGroupName).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.EqpId).IsUnique();
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.ToTable("DDB_EQUIPMENT_RESV");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.IssueNo).HasMaxLength(50).IsRequired();
            entity.Property(r => r.EqpId).HasMaxLength(30).IsRequired();
            entity.Property(r => r.LineId).HasMaxLength(20).IsRequired();
            entity.Property(r => r.LargeClass).HasMaxLength(20).IsRequired();
            entity.Property(r => r.EmpName).HasMaxLength(30).IsRequired();
            entity.Property(r => r.EmpNum).HasMaxLength(20).IsRequired();
            entity.Property(r => r.Purpose).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Status).HasMaxLength(20).IsRequired();
            entity.Property(r => r.CreatedAt).HasDefaultValueSql(currentTimestampSql);

            entity.HasOne(r => r.Equipment)
                .WithMany(e => e.Reservations)
                .HasForeignKey(r => r.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => r.IssueNo).IsUnique();
            entity.HasIndex(r => new { r.LineId, r.LargeClass, r.ReservedDate });
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("MST_EMPLOYEE");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).HasMaxLength(30).IsRequired();
            entity.Property(e => e.EmpNo).HasMaxLength(20).IsRequired();
            entity.Property(e => e.HName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DeptCode).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DeptName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SingleId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SingleMailAddr).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Site).HasMaxLength(20).IsRequired();

            entity.HasAlternateKey(e => e.UserId);
            entity.HasAlternateKey(e => e.EmpNo);
        });

        modelBuilder.Entity<OpenLabAuth>(entity =>
        {
            entity.ToTable("DDB_OPENLAB_AUTH");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Site).HasMaxLength(20).IsRequired();
            entity.Property(a => a.EqpName).HasMaxLength(30).IsRequired();
            entity.Property(a => a.AuthType).HasMaxLength(20).IsRequired();
            entity.Property(a => a.EmpNo).HasMaxLength(20).IsRequired();

            entity.HasOne(a => a.Employee)
                .WithMany(e => e.OpenLabAuths)
                .HasPrincipalKey(e => e.EmpNo)
                .HasForeignKey(a => a.EmpNo)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(a => new { a.Site, a.EqpName, a.AuthType, a.EmpNo }).IsUnique();
        });

        modelBuilder.Entity<ApprovalNotification>(entity =>
        {
            entity.ToTable("DDB_APPROVAL_NOTI");
            entity.HasKey(n => n.Id);
            entity.Property(n => n.IssueNo).HasMaxLength(50).IsRequired();
            entity.Property(n => n.ApprovalSeq).HasMaxLength(10).IsRequired();
            entity.Property(n => n.ApprovalReq).HasMaxLength(10).IsRequired();
            entity.Property(n => n.NotiUserId).HasMaxLength(30).IsRequired();
            entity.Property(n => n.NotiUserName).HasMaxLength(50).IsRequired();
            entity.Property(n => n.NotiUserDeptCode).HasMaxLength(20).IsRequired();
            entity.Property(n => n.NotiUserDeptName).HasMaxLength(50).IsRequired();
            entity.Property(n => n.NotiSingleMailAddr).HasMaxLength(150).IsRequired();
            entity.Property(n => n.LastUpdateTime).HasDefaultValueSql(currentTimestampSql);

            entity.HasOne(n => n.Employee)
                .WithMany(e => e.ApprovalNotifications)
                .HasPrincipalKey(e => e.UserId)
                .HasForeignKey(n => n.NotiUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(n => new { n.IssueNo, n.ApprovalSeq, n.NotiUserId }).IsUnique();
        });

        modelBuilder.Entity<UiSearchHistory>(entity =>
        {
            entity.ToTable("TSP_YMS_UI_SEARCH_HISTORY");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.AppId).HasMaxLength(100).IsRequired();
            entity.Property(h => h.ControlId).HasMaxLength(100).IsRequired();
            entity.Property(h => h.UserId).HasMaxLength(30).IsRequired();
            entity.Property(h => h.SearchValue).HasMaxLength(1000).IsRequired();
            entity.Property(h => h.SearchTime).HasDefaultValueSql(currentTimestampSql);

            entity.HasIndex(h => new { h.AppId, h.ControlId, h.UserId, h.SearchTime });
        });
    }
}

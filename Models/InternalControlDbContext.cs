using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace InternalControlApp.Models;

public partial class InternalControlDbContext : DbContext
{
    public InternalControlDbContext()
    {
    }

    public InternalControlDbContext(DbContextOptions<InternalControlDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdministrativeUnit> AdministrativeUnits { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<ControlElementsPtci> ControlElementsPtcis { get; set; }

    public virtual DbSet<Delivery> Deliveries { get; set; }

    public virtual DbSet<ImprovementActionsPtci> ImprovementActionsPtcis { get; set; }

    public virtual DbSet<RiskFactorsPtar> RiskFactorsPtars { get; set; }

    public virtual DbSet<RisksPtar> RisksPtars { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdministrativeUnit>(entity =>
        {
            entity.HasKey(e => e.UnitId).HasName("PK__Administ__44F5ECB50DAB0549");

            entity.HasIndex(e => e.UnitName, "UQ__Administ__B5EE6678967F95E3").IsUnique();

            entity.Property(e => e.UnitName).HasMaxLength(150);
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("PK__Attachme__442C64BE506199C5");

            entity.Property(e => e.OriginalFileName).HasMaxLength(255);
            entity.Property(e => e.StoragePath).HasMaxLength(500);

            entity.HasOne(d => d.Delivery).WithMany(p => p.Attachments)
                .HasForeignKey(d => d.DeliveryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Attachments_Deliveries");
        });

        modelBuilder.Entity<ControlElementsPtci>(entity =>
        {
            entity.HasKey(e => e.ElementId).HasName("PK__ControlE__A429721A5F20653B");

            entity.ToTable("ControlElements_PTCI");

            entity.Property(e => e.ControlNumber).HasMaxLength(20);
            entity.Property(e => e.Ngci)
                .HasMaxLength(100)
                .HasColumnName("NGCI");
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.HasKey(e => e.DeliveryId).HasName("PK__Deliveri__626D8FCEFB38C7ED");

            entity.Property(e => e.ActionIdPtci).HasColumnName("ActionId_PTCI");
            entity.Property(e => e.FactorIdPtar).HasColumnName("FactorId_PTAR");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.SubmissionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.ActionIdPtciNavigation).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.ActionIdPtci)
                .HasConstraintName("FK_Deliveries_ImprovementActionsPTCI");

            entity.HasOne(d => d.FactorIdPtarNavigation).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.FactorIdPtar)
                .HasConstraintName("FK_Deliveries_RiskFactorsPTAR");

            entity.HasOne(d => d.User).WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Deliveries_Users");
        });

        modelBuilder.Entity<ImprovementActionsPtci>(entity =>
        {
            entity.HasKey(e => e.ActionId).HasName("PK__Improvem__FFE3F4D91C27E938");

            entity.ToTable("ImprovementActions_PTCI");

            entity.Property(e => e.ActionNumber).HasMaxLength(20);
            entity.Property(e => e.Process).HasMaxLength(150);
            entity.Property(e => e.Quarter1Grade).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Quarter1GradeOic)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Quarter1Grade_OIC");
            entity.Property(e => e.Quarter2Grade).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Quarter2GradeOic)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Quarter2Grade_OIC");
            entity.Property(e => e.Quarter3Grade).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Quarter3GradeOic)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Quarter3Grade_OIC");
            entity.Property(e => e.Quarter4Grade).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Quarter4GradeOic)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Quarter4Grade_OIC");

            entity.HasOne(d => d.Element).WithMany(p => p.ImprovementActionsPtcis)
                .HasForeignKey(d => d.ElementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ImprovementActions_Elements");

            entity.HasOne(d => d.ResponsibleUser).WithMany(p => p.ImprovementActionsPtcis)
                .HasForeignKey(d => d.ResponsibleUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ImprovementActions_Users");

            entity.HasOne(d => d.Unit).WithMany(p => p.ImprovementActionsPtcis)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ImprovementActions_Units");
        });

        modelBuilder.Entity<RiskFactorsPtar>(entity =>
        {
            entity.HasKey(e => e.FactorId).HasName("PK__RiskFact__E733AADD1B1C6FAA");

            entity.ToTable("RiskFactors_PTAR");

            entity.Property(e => e.FactorNumber).HasMaxLength(20);
            entity.Property(e => e.ProgressPercentage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Quarter1Grade).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Quarter1GradeOic)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Quarter1Grade_OIC");
            entity.Property(e => e.Quarter2Grade).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Quarter2GradeOic)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Quarter2Grade_OIC");
            entity.Property(e => e.Quarter3Grade).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Quarter3GradeOic)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Quarter3Grade_OIC");
            entity.Property(e => e.Quarter4Grade).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Quarter4GradeOic)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Quarter4Grade_OIC");

            entity.HasOne(d => d.ResponsibleUser).WithMany(p => p.RiskFactorsPtars)
                .HasForeignKey(d => d.ResponsibleUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RiskFactors_Users");

            entity.HasOne(d => d.Risk).WithMany(p => p.RiskFactorsPtars)
                .HasForeignKey(d => d.RiskId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RiskFactors_RisksPTAR");

            entity.HasOne(d => d.Unit).WithMany(p => p.RiskFactorsPtars)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RiskFactors_Units");
        });

        modelBuilder.Entity<RisksPtar>(entity =>
        {
            entity.HasKey(e => e.RiskId).HasName("PK__Risks_PT__435363F6BB7AFDE8");

            entity.ToTable("Risks_PTAR");

            entity.HasIndex(e => e.RiskNumber, "UQ__Risks_PT__40A5999A471909DF").IsUnique();

            entity.Property(e => e.Quadrant).HasMaxLength(20);
            entity.Property(e => e.RiskClassification).HasMaxLength(100);
            entity.Property(e => e.RiskNumber).HasMaxLength(20);
            entity.Property(e => e.Strategy).HasMaxLength(100);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A775946C8");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B616019F68474").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C702E8C1D");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D1053402DE0EFC").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

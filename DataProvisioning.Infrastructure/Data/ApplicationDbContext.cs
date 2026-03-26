using DataProvisioning.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DataProvisioning.Domain.Enums;
using DataProvisioning.Application.Interfaces;
using System.Collections.Generic;

namespace DataProvisioning.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<VirtualGroup> VirtualGroups { get; set; } = null!;
    public DbSet<VirtualGroupMember> VirtualGroupMembers { get; set; } = null!;
    public DbSet<Dataset> Datasets { get; set; } = null!;
    public DbSet<DatasetColumn> Columns { get; set; } = null!;
    public DbSet<AccessRequest> AccessRequests { get; set; } = null!;
    public DbSet<Report> Reports { get; set; } = null!;
    public DbSet<AssetPolicyGroup> AssetPolicyGroups { get; set; } = null!;
    public DbSet<AssetPolicyCondition> AssetPolicyConditions { get; set; } = null!;
    public DbSet<AssetPolicyColumn> AssetPolicyColumns { get; set; } = null!;
    public DbSet<InitialAdmin> InitialAdmins { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var roleConverter = new EnumToStringConverter<UserRole>();
        var typeConverter = new EnumToStringConverter<DatasetType>();
        var statusConverter = new EnumToStringConverter<RequestStatus>();

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Role).HasColumnName("role").HasConversion(roleConverter);
            entity.Property(e => e.Avatar).HasColumnName("avatar");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<InitialAdmin>(entity =>
        {
            entity.ToTable("initial_admins");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username").IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<VirtualGroup>(entity =>
        {
            entity.ToTable("virtual_groups");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Owner)
                .WithMany(u => u.OwnedGroups)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VirtualGroupMember>(entity =>
        {
            entity.ToTable("virtual_group_members");
            entity.HasKey(e => new { e.GroupId, e.UserId });
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AddedAt).HasColumnName("added_at");

            entity.HasOne(e => e.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.GroupMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Dataset>(entity =>
        {
            entity.ToTable("datasets");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Type).HasColumnName("type").HasConversion(typeConverter);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.OwnerGroupId).HasColumnName("owner_group_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.OwnerGroup)
                .WithMany(g => g.Datasets)
                .HasForeignKey(e => e.OwnerGroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<DatasetColumn>(entity =>
        {
            entity.ToTable("columns");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DatasetId).HasColumnName("dataset_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.DataType).HasColumnName("data_type");
            entity.Property(e => e.Definition).HasColumnName("definition");
            entity.Property(e => e.IsPii).HasColumnName("is_pii");
            entity.Property(e => e.SampleData).HasColumnName("sample_data");

            entity.HasOne(e => e.Dataset)
                .WithMany(d => d.Columns)
                .HasForeignKey(e => e.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccessRequest>(entity =>
        {
            entity.ToTable("access_requests");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.DatasetId).HasColumnName("dataset_id");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion(statusConverter);
            entity.Property(e => e.RequestedRlsFilters).HasColumnName("requested_rls_filters");
            entity.Property(e => e.Justification).HasColumnName("justification");
            entity.Property(e => e.ReviewedById).HasColumnName("reviewed_by");
            entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.PolicyGroupId).HasColumnName("policy_group_id");

            entity.HasOne(e => e.User)
                .WithMany(u => u.AccessRequests)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Dataset)
                .WithMany(d => d.AccessRequests)
                .HasForeignKey(e => e.DatasetId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReviewedBy)
                .WithMany(u => u.ReviewedRequests)
                .HasForeignKey(e => e.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.PolicyGroup)
                .WithMany(g => g.AccessRequests)
                .HasForeignKey(e => e.PolicyGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssetPolicyGroup>(entity =>
        {
            entity.ToTable("asset_policy_groups");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DatasetId).HasColumnName("dataset_id");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Dataset)
                .WithMany(d => d.PolicyGroups)
                .HasForeignKey(e => e.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AssetPolicyCondition>(entity =>
        {
            entity.ToTable("asset_policy_conditions");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PolicyGroupId).HasColumnName("policy_group_id");
            entity.Property(e => e.ColumnName).HasColumnName("column_name");
            entity.Property(e => e.Operator).HasColumnName("operator");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(e => e.PolicyGroup)
                .WithMany(g => g.Conditions)
                .HasForeignKey(e => e.PolicyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssetPolicyColumn>(entity =>
        {
            entity.ToTable("asset_policy_columns");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PolicyGroupId).HasColumnName("policy_group_id");
            entity.Property(e => e.ColumnName).HasColumnName("column_name");
            entity.Property(e => e.IsHidden).HasColumnName("is_hidden");

            entity.HasOne(e => e.PolicyGroup)
                .WithMany(g => g.HiddenColumns)
                .HasForeignKey(e => e.PolicyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("reports");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Url).HasColumnName("url");
            entity.Property(e => e.Description).HasColumnName("description");

            entity.HasMany(e => e.Datasets)
                .WithMany(d => d.Reports)
                .UsingEntity<Dictionary<string, object>>(
                    "report_datasets",
                    j => j.HasOne<Dataset>().WithMany().HasForeignKey("dataset_id").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Report>().WithMany().HasForeignKey("report_id").OnDelete(DeleteBehavior.Cascade));
        });
    }
}

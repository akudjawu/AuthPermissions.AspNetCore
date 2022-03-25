﻿// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using Example6.SingleLevelSharding.EfCoreClasses;
using Microsoft.EntityFrameworkCore;

namespace Example6.SingleLevelSharding.EfCoreCode;

/// <summary>
/// This is a DBContext that supports sharding 
/// </summary>
public class ShardingSingleDbContext : DbContext, IDataKeyFilterReadOnly
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="shardingDataKeyAndConnect">This uses a service that obtains the DataKey and database connection string
    /// via the claims in the logged in users.</param>
    public ShardingSingleDbContext(DbContextOptions<ShardingSingleDbContext> options,
        IGetShardingDataFromUser shardingDataKeyAndConnect)
        : base(options)
    {
        // The DataKey is null when: no one is logged in, its a background service, or user hasn't got an assigned tenant
        // In these cases its best to set the data key that doesn't match any possible DataKey 
        DataKey = shardingDataKeyAndConnect?.DataKey ?? "stop any user without a DataKey to access the data";

        if (shardingDataKeyAndConnect?.ConnectionString != null)
            //NOTE: If no connection string is provided the DbContext will use the connection it was provided when it was registered
            //If you don't want that to happen, then remove the if above and the connection will be set to null (and fail) 
            Database.SetConnectionString(shardingDataKeyAndConnect.ConnectionString);
    }

    public DbSet<CompanyTenant> Companies { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<LineItem> LineItems { get; set; }
    public string DataKey { get; }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.MarkWithDataKeyIfNeeded(DataKey);
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        this.MarkWithDataKeyIfNeeded(DataKey);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("invoice");

        // You could manually set up the Query Filter, but there is a easier approach
        //modelBuilder.Entity<Invoice>().HasQueryFilter(x => x.DataKey == DataKey);
        //modelBuilder.Entity<LineItem>().HasQueryFilter(x => x.DataKey == DataKey);
        //modelBuilder.Entity<CompanyTenant>().HasQueryFilter(x => x.DataKey == DataKey);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IDataKeyFilterReadWrite).IsAssignableFrom(entityType.ClrType))
            {
                entityType.AddSingleTenantShardingQueryFilter(this);
            }
            else
            {
                throw new Exception(
                    $"You haven't added the {nameof(IDataKeyFilterReadWrite)} to the entity {entityType.ClrType.Name}");
            }

            foreach (var mutableProperty in entityType.GetProperties())
            {
                if (mutableProperty.ClrType == typeof(decimal))
                {
                    mutableProperty.SetPrecision(9);
                    mutableProperty.SetScale(2);
                }
            }
        }
    }
}
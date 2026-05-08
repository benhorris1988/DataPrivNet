using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DataProvisioning.Application.DTOs;
using DataProvisioning.Application.Interfaces;
using DataProvisioning.Domain.Entities;
using DataProvisioning.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace DataProvisioning.Application.Services;

public class CatalogService : ICatalogService
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public CatalogService(IApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<List<DatasetCatalogDto>> GetCatalogAsync(int currentUserId, string? searchQuery = null)
    {
        var query = _context.Datasets
            .Include(d => d.OwnerGroup)
            .ThenInclude(g => g != null ? g.Members : null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(d =>
                d.Name.Contains(searchQuery) ||
                (d.Description != null && d.Description.Contains(searchQuery)));
        }

        var results = await query.Select(d => new DatasetCatalogDto
        {
            Id = d.Id,
            Name = d.Name,
            Type = d.Type.ToString(),
            Description = d.Description,
            GroupName = d.OwnerGroup != null ? d.OwnerGroup.Name : "Unassigned",
            GroupOwnerId = d.OwnerGroup != null ? d.OwnerGroup.OwnerId : null,
            IsMember = d.OwnerGroup != null && d.OwnerGroup.Members.Any(m => m.UserId == currentUserId),
            AccessStatus = _context.AccessRequests
                .Where(ar => ar.DatasetId == d.Id && ar.UserId == currentUserId)
                .OrderBy(ar => ar.Status == RequestStatus.Approved ? 1 : ar.Status == RequestStatus.Pending ? 2 : 3)
                .Select(ar => ar.Status.ToString())
                .FirstOrDefault()
        }).ToListAsync();

        return results.OrderBy(d => d.Name).ToList();
    }

    public async Task<string> SyncCatalogAsync()
    {
        try
        {
            var dwTableInfo = await GetDataWarehouseTablesAsync();

            int tablesAdded = 0;
            int columnsAdded = 0;
            int columnsUpdated = 0;

            foreach (var table in dwTableInfo)
            {
                var existingDataset = await _context.Datasets
                    .Include(d => d.Columns)
                    .FirstOrDefaultAsync(d => d.Name == table.TableName);

                if (existingDataset == null)
                {
                    var newDataset = new Dataset
                    {
                        Name = table.TableName,
                        Type = DatasetType.Fact,
                        Description = null,
                        CreatedAt = DateTime.UtcNow,
                        Columns = new List<DatasetColumn>()
                    };

                    foreach (var col in table.Columns)
                    {
                        newDataset.Columns.Add(new DatasetColumn
                        {
                            Name = col.ColumnName,
                            DataType = col.DataType,
                            Definition = col.Definition,
                            IsPii = false,
                            SampleData = null
                        });
                    }

                    _context.Datasets.Add(newDataset);
                    tablesAdded++;
                    columnsAdded += table.Columns.Count;
                }
                else
                {
                    var existingColumnNames = existingDataset.Columns.ToDictionary(c => c.Name);
                    var dwColumnNames = new HashSet<string>(table.Columns.Select(c => c.ColumnName));

                    foreach (var col in table.Columns)
                    {
                        if (existingColumnNames.TryGetValue(col.ColumnName, out var existingCol))
                        {
                            if (existingCol.DataType != col.DataType || existingCol.Definition != col.Definition)
                            {
                                existingCol.DataType = col.DataType;
                                existingCol.Definition = col.Definition;
                                columnsUpdated++;
                            }
                        }
                        else
                        {
                            existingDataset.Columns.Add(new DatasetColumn
                            {
                                Name = col.ColumnName,
                                DataType = col.DataType,
                                Definition = col.Definition,
                                IsPii = false,
                                SampleData = null
                            });
                            columnsAdded++;
                        }
                    }

                    var columnsToRemove = existingDataset.Columns
                        .Where(c => !dwColumnNames.Contains(c.Name))
                        .ToList();

                    foreach (var col in columnsToRemove)
                    {
                        existingDataset.Columns.Remove(col);
                    }
                }
            }

            await _context.SaveChangesAsync();

            return $"✓ Catalog sync completed! " +
                   $"Tables added: {tablesAdded}, " +
                   $"Columns added: {columnsAdded}, " +
                   $"Columns updated: {columnsUpdated}. " +
                   $"All existing security settings have been preserved.";
        }
        catch (Exception ex)
        {
            return $"✗ Sync failed: {ex.Message}";
        }
    }

    private async Task<List<TableInfo>> GetDataWarehouseTablesAsync()
    {
        var tables = new List<TableInfo>();
        var dwConnString = _configuration.GetConnectionString("DataWarehouseConnection");

        if (string.IsNullOrEmpty(dwConnString))
            throw new InvalidOperationException("DataWarehouseConnection not configured");

        using (var connection = new SqlConnection(dwConnString))
        {
            await connection.OpenAsync();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT DISTINCT TABLE_NAME
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_TYPE = 'BASE TABLE'
                    AND TABLE_SCHEMA NOT IN ('sys', 'INFORMATION_SCHEMA')
                    ORDER BY TABLE_NAME";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var tableNames = new List<string>();
                    while (await reader.ReadAsync())
                    {
                        tableNames.Add(reader.GetString(0));
                    }

                    foreach (var tableName in tableNames)
                    {
                        var columns = await GetTableColumnsAsync(connection, tableName);
                        tables.Add(new TableInfo
                        {
                            TableName = tableName,
                            Columns = columns
                        });
                    }
                }
            }
        }

        return tables;
    }

    private async Task<List<ColumnInfo>> GetTableColumnsAsync(SqlConnection connection, string tableName)
    {
        var columns = new List<ColumnInfo>();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT COLUMN_NAME, DATA_TYPE
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @tableName
                ORDER BY ORDINAL_POSITION";

            cmd.Parameters.AddWithValue("@tableName", tableName);

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    columns.Add(new ColumnInfo
                    {
                        ColumnName = reader.GetString(0),
                        DataType = reader.GetString(1),
                        Definition = null
                    });
                }
            }
        }

        return columns;
    }

    private class TableInfo
    {
        public string TableName { get; set; } = string.Empty;
        public List<ColumnInfo> Columns { get; set; } = new();
    }

    private class ColumnInfo
    {
        public string ColumnName { get; set; } = string.Empty;
        public string? DataType { get; set; }
        public string? Definition { get; set; }
    }
}

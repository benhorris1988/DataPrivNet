using System.Collections.Generic;
using System.Threading.Tasks;
using DataProvisioning.Application.DTOs;

namespace DataProvisioning.Application.Interfaces;

public interface ICatalogService
{
    Task<List<DatasetCatalogDto>> GetCatalogAsync(int currentUserId, string? searchQuery = null);
    Task<string> SyncCatalogAsync();
}

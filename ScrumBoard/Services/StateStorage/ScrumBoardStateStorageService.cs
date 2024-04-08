using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ScrumBoard.Models;
using SharedLensResources.Blazor.StateManagement;

namespace ScrumBoard.Services.StateStorage
{
    /// <summary>
    /// Extends the base state provider to cater to some ScrumBoard specific state.
    /// </summary>
    public interface IScrumBoardStateStorageService : IStateStorageService
    {
        Task SetSelectedProjectIdAsync(long projectId);
        Task<long?> GetSelectedProjectIdAsync();
        Task<List<TableColumnConfiguration>> GetTableColumnConfiguration();
        Task SetTableColumnConfiguration(List<TableColumnConfiguration> configuration);
        Task RemoveSelectedProjectIdAsync();
    }

    /// <summary>
    /// Extends the base state provider to cater to some ScrumBoard specific state.
    /// </summary>
    public class ScrumBoardStateStorageService : StateStorageService, IScrumBoardStateStorageService
    {
        private const string SelectedProjectKey = "SELECTED_PROJECT_ID";

        private const string TableColumnConfigurationKey = "report.worklogtable";

        private readonly IProtectedLocalStorageWrapper _protectedLocalStorage;
        private readonly IProtectedSessionStorageWrapper _protectedSessionStorage;

        private readonly ILogger<ScrumBoardStateStorageService> _logger;

        public ScrumBoardStateStorageService(
            IProtectedLocalStorageWrapper localStorageService,
            IProtectedSessionStorageWrapper sessionStorageService,
            ILogger<ScrumBoardStateStorageService> logger
        ) : base(localStorageService, sessionStorageService, logger)
        {
            _protectedLocalStorage = localStorageService;
            _protectedSessionStorage = sessionStorageService;
            _logger = logger;
        }

        public async Task SetSelectedProjectIdAsync(long projectId) => await SetAsync(SelectedProjectKey, projectId);

        public async Task<long?> GetSelectedProjectIdAsync() {
            return await GetAsync<long?>(SelectedProjectKey);
        }

        public async Task<List<TableColumnConfiguration>> GetTableColumnConfiguration()
        {
            return await GetAsync<List<TableColumnConfiguration>>(TableColumnConfigurationKey);
        }

        public async Task SetTableColumnConfiguration(List<TableColumnConfiguration> configuration)
        {
            await SetAsync(TableColumnConfigurationKey, configuration);
        }

        public async Task RemoveSelectedProjectIdAsync()
        {
            await _protectedLocalStorage.DeleteAsync(SelectedProjectKey);
            await _protectedSessionStorage.DeleteAsync(SelectedProjectKey);
        }
    }
}
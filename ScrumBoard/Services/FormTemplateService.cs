using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities.Forms.Templates;
using ScrumBoard.Repositories;

namespace ScrumBoard.Services;

public  interface IFormTemplateService
{
    Task AddOrUpdateAsync(FormTemplate currentFormTemplate);
    Task<bool> CheckForDuplicateName(string potentialName, string currentName);
    Task<ICollection<FormTemplateBlock>> GetOrderedBlocksForFormTemplateAsync(long formTemplateId);
}


public class FormTemplateService : IFormTemplateService
{
    private readonly IFormTemplateRepository _formTemplateRepository;
    private readonly IDbContextFactory<DatabaseContext> _dbContextFactory;

    public FormTemplateService(IFormTemplateRepository formTemplateRepository, IDbContextFactory<DatabaseContext> dbContextFactory)
    {
        _formTemplateRepository = formTemplateRepository;
        _dbContextFactory = dbContextFactory;
    }
    
    
    public async Task AddOrUpdateAsync(FormTemplate currentFormTemplate)
    {
        var isNewForm = currentFormTemplate.Id == default;
        
        if (isNewForm)
        {
            await _formTemplateRepository.AddAsync(currentFormTemplate);
        }
        else
        {
            await _formTemplateRepository.UpdateWithBlocksAsync(currentFormTemplate);
        }
    }

    /// <summary>
    /// Returns true if the potential name is already in the database
    /// </summary>
    /// <param name="potentialName"></param>
    /// <param name="currentName"></param>
    /// <returns></returns>
    public async Task<bool> CheckForDuplicateName(string potentialName, string currentName)
    {
        return potentialName != currentName && await _formTemplateRepository.GetByNameAsync(potentialName) != null;
    }

    public async Task<ICollection<FormTemplateBlock>> GetOrderedBlocksForFormTemplateAsync(long formTemplateId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.FormTemplateBlocks
            .Where(x => x.FormTemplateId == formTemplateId)
            .OrderBy(x => x.FormPosition)
            .ToListAsync();
    }
}
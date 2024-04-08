using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScrumBoard.DataAccess;
using ScrumBoard.Models.Entities.Forms.Templates;

namespace ScrumBoard.Repositories;

using TransformType = Func<IQueryable<FormTemplate>, IQueryable<FormTemplate>>;

public interface IFormTemplateRepository : IRepository<FormTemplate>
{
    Task<FormTemplate> GetByIdAsync(long id, params TransformType[] transforms);
    Task<FormTemplate> GetByNameAsync(string name, params TransformType[] transforms);
    Task UpdateWithBlocksAsync(FormTemplate formTemplate);
}

public static class FormTemplateIncludes
{
    /// <summary>
    /// Includes FormTemplate.Blocks
    /// </summary>
    public static readonly TransformType Blocks = query => query.Include(form => form.Blocks);
    
    /// <summary>
    /// Includes FormTemplate.Assignments
    /// </summary>
    public static readonly TransformType Assignments = query => query.Include(form => form.Assignments);

    /// <summary>
    /// Includes FormTemplate.Creator
    /// </summary>
    public static readonly TransformType Creator = query => query.Include(form => form.Creator);
}

/// <summary>
/// Repository for FormTemplates
/// </summary>
public class FormTemplateRepository : Repository<FormTemplate>, IFormTemplateRepository
{
    public FormTemplateRepository(IDbContextFactory<DatabaseContext> dbContextFactory,
        ILogger<FormTemplateRepository> logger) : base(dbContextFactory, logger)
    {
    }

    /// <summary>
    /// Gets a form template by its Id, returns null if no form template with the id exists
    /// </summary>
    /// <param name="id">Form template key to find</param>
    /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
    /// <returns>Form template with the given Id if it exists, otherwise null</returns>
    public async Task<FormTemplate> GetByIdAsync(long id, params TransformType[] transforms)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await GetBaseQuery(context, transforms)
            .Where(formTemplate => formTemplate.Id == id)
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// Gets a form template by its Name, returns null if no form template with the name exists
    /// </summary>
    /// <param name="name">Form template name to find</param>
    /// <param name="transforms">List of transformations on the queryable to apply e.g. includes, filters</param>
    /// <returns>Form template with the given Name if it exists, otherwise null</returns>
    public async Task<FormTemplate> GetByNameAsync(string name, params TransformType[] transforms)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await GetBaseQuery(context, transforms)
            .Where(formTemplate => formTemplate.Name == name)
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// Updates the form template (persisting it to the db) and any blocks within the form
    /// </summary>
    /// <param name="formTemplate">Form template to update</param>
    public async Task UpdateWithBlocksAsync(FormTemplate formTemplate)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Database.BeginTransactionAsync();
        context.Update(formTemplate);
        DeleteLeftoverChildren(context, formTemplate, formTemplate.Blocks, block => block.FormTemplate);
        foreach (var multichoice in formTemplate.Blocks.OfType<MultiChoiceQuestion>())
            DeleteLeftoverChildren(context, multichoice, multichoice.Options, option => option.MultiChoiceQuestion);
        await context.SaveChangesAsync();
        await context.Database.CommitTransactionAsync();

        await UpdateRowVersion(context, formTemplate.Id);
        var updatedForm = await GetByIdAsync(formTemplate.Id);
        formTemplate.RowVersion = updatedForm.RowVersion;
    }
}
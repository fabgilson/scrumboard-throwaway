using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ScrumBoard.Models;
using Xunit;

namespace ScrumBoard.Tests.Unit.Models;

public class TableColumnTest
{
    private static readonly IEnumerable<TableColumn> NotOrderableTableColumns = new List<TableColumn> { TableColumn.IssueTags, TableColumn.TaskTags, TableColumn.WorklogTags };
    
    /// <summary>
    /// This is more complex than manually writing each TableColumn value, but ensures that all Table Columns are tested.
    /// When a new column is added, the test will pass by default if the column is orderable. 
    /// </summary>
    public static TheoryData<TableColumn> OrderableTableColumnsMemberData =>
        new(Enum.GetValues(typeof(TableColumn))
                .Cast<TableColumn>()
                .Where(column => !NotOrderableTableColumns.Contains(column))
        );

    public static TheoryData<TableColumn> NotOrderableTableColumnsMemberData => new() {TableColumn.IssueTags, TableColumn.TaskTags, TableColumn.WorklogTags};

    
    [Theory]
    [MemberData(nameof(OrderableTableColumnsMemberData))]
    public void GetOrderabilityOfTableColumn_IsOrderable_ReturnsTrue(TableColumn tableColumn)
    {
        var isOrderable = tableColumn.IsOrderable();
        isOrderable.Should().Be(true);
    }

    [Theory]
    [MemberData(nameof(NotOrderableTableColumnsMemberData))]
    public void GetOrderabilityOfTableColumn_IsNotOrderable_ReturnsTrue(TableColumn tableColumn)
    {
        var isOrderable = tableColumn.IsOrderable();
        isOrderable.Should().Be(false);
    }
}
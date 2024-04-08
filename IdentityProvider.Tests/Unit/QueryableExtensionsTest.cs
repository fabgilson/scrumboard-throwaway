using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using IdentityProvider.Extensions;
using SharedLensResources;
using Xunit;

namespace IdentityProvider.Tests.Unit;

public class QueryableExtensionsTest
{
    /// <summary>
    /// Test cases for generating the DynamicLinq supported query strings from some BasicStringFilteringOptions
    /// </summary>
    public static readonly TheoryData<StringFilterType, bool, string[], string> GenerateFilterTextTheoryData = new()
    {
        // Exact match
        {
            StringFilterType.MatchFullText, true, new []{ "Name"}, 
            "Name == @0"
        },
        {
            StringFilterType.MatchFullText, false, new []{ "Name"}, 
            "Name.ToLower() == @0.ToLower()"
        },
        {
            StringFilterType.MatchFullText, true, new []{ "Name", "Description"}, 
            "Name == @0 || Description == @0"
        },
        {
            StringFilterType.MatchFullText, false, new []{ "Name", "Description"}, 
            "Name.ToLower() == @0.ToLower() || Description.ToLower() == @0.ToLower()"
        },
        // Wildcard match
        {
            StringFilterType.Contains, true, new []{ "Name"}, 
            "Name.Contains(@0)"
        },
        {
            StringFilterType.Contains, false, new []{ "Name"}, 
            "Name.ToLower().Contains(@0.ToLower())"
        },
        {
            StringFilterType.Contains, true, new []{ "Name", "Description"}, 
            "Name.Contains(@0) || Description.Contains(@0)"
        },
        {
            StringFilterType.Contains, false, new []{ "Name", "Description"}, 
            "Name.ToLower().Contains(@0.ToLower()) || Description.ToLower().Contains(@0.ToLower())"
        },
    };

    [Theory]
    [MemberData(nameof(GenerateFilterTextTheoryData))]
    public void GenerateStringFilterText_VariousOptions_CallsWhereWithCorrectParams(
        StringFilterType filterType,
        bool isCaseSensitive,
        string[] propertyNames,
        string expectedOutput
    ) {
        var stringFilteringOptions = new BasicStringFilteringOptions
        {
            FilterText = "someFilterText",
            FilterType = filterType,
            IsCaseSensitive = isCaseSensitive
        };
        QueryableExtensions
            .GenerateStringFilterText(stringFilteringOptions, propertyNames)
            .Should().Be(expectedOutput);
    }
    
    private class QueryableExtensionsTestingObject
    {
        public string Name { get; }
        public string Description { get; }
        public DateTime Start { get; }
        
        public QueryableExtensionsTestingObject(string name, string description, DateTime? start=null)
        {
            Name = name;
            Description = description;
            Start = start ?? DateTime.MinValue;
        }
    }

    private static IQueryable<QueryableExtensionsTestingObject> TestingFilterableObjects => new List<QueryableExtensionsTestingObject>
    {
        new("SENG302", "Software engineering project course"),
        new("COSC302", "Computer science project course"),
        new("SENG202", "Second year software engineering project course")
    }.AsQueryable();

    [Theory]
    [InlineData("SENG", false, new[] { "Name" }, new int[] {})]
    [InlineData("COSC", false, new[] { "Name" }, new int[] {})]
    [InlineData("Seng302", false, new[] { "Name" }, new[] {0})]
    [InlineData("Seng302", true, new[] { "Name" }, new int[] {})]
    [InlineData("SENG302", true, new[] { "Name" }, new[] {0})]
    [InlineData("prOjeCt Course", false, new[] { "Name" }, new int[] {})]
    [InlineData("prOjeCt Course", true, new[] { "Name", "Description" }, new int[] {})]
    [InlineData("Software engineering project course", true, new[] { "Name", "Description" }, new [] {0})]
    [InlineData("Software ENGINEERING project course", false, new[] { "Name", "Description" }, new [] {0})]
    [InlineData("softWARE ENGINEERING", false, new[] { "Description" }, new int[] {})]
    [InlineData("softWARE ENGINEERING", true, new[] { "Description" }, new int[] {})]
    public void ApplyTextFilter_ExactMatch_CorrectResultsReturned(
        string filterText,
        bool isCaseSensitive,
        string[] searchedPropertyNames, 
        int[] objectIndicesExpected
    ) {   
        var stringFilteringOptions = new BasicStringFilteringOptions
        {
            FilterText = filterText,
            FilterType = StringFilterType.MatchFullText,
            IsCaseSensitive = isCaseSensitive
        };
        
        var results = TestingFilterableObjects
            .ApplyStringFilter(stringFilteringOptions, searchedPropertyNames);
        
        results.Should()
            .BeEquivalentTo(TestingFilterableObjects
                .Select((objectValue, index) => new {objectValue, index})
                .Where(item => objectIndicesExpected.Contains(item.index))
                .Select(x => x.objectValue)
            );
    }
    
    [Theory]
    [InlineData("SENG", false, new[] { "Name" }, new[] {0,2})]
    [InlineData("COSC", false, new[] { "Name" }, new[] {1})]
    [InlineData("302", false, new[] { "Name" }, new[] {0,1})]
    [InlineData("Seng302", false, new[] { "Name" }, new[] {0})]
    [InlineData("Seng302", true, new[] { "Name" }, new int[] {})]
    [InlineData("SENG302", true, new[] { "Name" }, new[] {0})]
    [InlineData("prOjeCt Course", false, new[] { "Name" }, new int[] {})]
    [InlineData("prOjeCt Course", true, new[] { "Name", "Description" }, new int[] {})]
    [InlineData("prOjeCt Course", false, new[] { "Name", "Description" }, new int[] {0,1,2})]
    [InlineData("Software engineering project course", true, new[] { "Name", "Description" }, new [] {0})]
    [InlineData("Software ENGINEERING project course", false, new[] { "Name", "Description" }, new [] {0,2})]
    [InlineData("softWARE ENGINEERING", false, new[] { "Description" }, new[] {0,2})]
    [InlineData("softWARE ENGINEERING", true, new[] { "Description" }, new int[] {})]
    public void ApplyTextFilter_FuzzySearch_CorrectResultsReturned(
        string filterText,
        bool isCaseSensitive,
        string[] searchedPropertyNames, 
        int[] objectIndicesExpected
    ) {   
        var stringFilteringOptions = new BasicStringFilteringOptions
        {
            FilterText = filterText,
            FilterType = StringFilterType.Contains,
            IsCaseSensitive = isCaseSensitive
        };
        
        var results = TestingFilterableObjects
            .ApplyStringFilter(stringFilteringOptions, searchedPropertyNames);
        
        results.Should()
            .BeEquivalentTo(TestingFilterableObjects
                .Select((objectValue, index) => new {objectValue, index})
                .Where(item => objectIndicesExpected.Contains(item.index))
                .Select(x => x.objectValue)
            );
    }

    private static IQueryable<QueryableExtensionsTestingObject> TestingOrderableItems => new List<QueryableExtensionsTestingObject>
    {
        new("SENG302", "Software engineering project course #1"),
        new("SENG302", "Software engineering project course #2"),
        new("SENG202", "Software engineering project course #2"),
        new("COSC302", "Computer science project course 1"),
        new("COSC202", "Computer science project course 2"),
        new("COSC3023", "Computer science project course 3"),
        new("SENG202", "Second year software engineering project course")
    }.AsQueryable();
    
    [Fact]
    public void ApplyOrderByOptions_SinglePropertyAscending_CorrectOrderReturned()
    {
        TestingOrderableItems.ApplyOrderByOptions(new RepeatedField<OrderByOption>
        {
            new OrderByOption{PropertyName = "Name", IsAscendingOrder = true}
        }).Should().BeEquivalentTo(
            TestingOrderableItems.OrderBy(x => x.Name), 
            options => options.WithStrictOrdering()
        );
    }
    
    [Fact]
    public void ApplyOrderByOptions_SinglePropertyDescending_CorrectOrderReturned()
    {
        TestingOrderableItems.ApplyOrderByOptions(new RepeatedField<OrderByOption>
        {
            new OrderByOption{PropertyName = "Description", IsAscendingOrder = false}
        }).Should().BeEquivalentTo(
            TestingOrderableItems.OrderByDescending(x => x.Description),
            options => options.WithStrictOrdering()
        );
    }

    [Fact]
    public void ApplyOrderByOptions_MultiplePropertiesAscending_CorrectOrderReturned()
    {
        TestingOrderableItems.ApplyOrderByOptions(new RepeatedField<OrderByOption>
        {
            new OrderByOption { PropertyName = "Description", IsAscendingOrder = true },
            new OrderByOption { PropertyName = "Name", IsAscendingOrder = true }
        }).Should().BeEquivalentTo(
            TestingOrderableItems
                .OrderBy(x => x.Description)
                .ThenBy(x => x.Name),
            options => options.WithStrictOrdering()
        );
    }

    [Fact]
    public void ApplyOrderByOptions_MultiplePropertiesDescending_CorrectOrderReturned()
    {
        TestingOrderableItems.ApplyOrderByOptions(new RepeatedField<OrderByOption>
        {
            new OrderByOption{PropertyName = "Description", IsAscendingOrder = false},
            new OrderByOption{PropertyName = "Name", IsAscendingOrder = false}
        }).Should().BeEquivalentTo(
            TestingOrderableItems
                .OrderByDescending(x => x.Description)
                .ThenByDescending(x => x.Name),
            options => options.WithStrictOrdering()
        );
    }
    
    [Fact]
    public void ApplyOrderByOptions_MultiplePropertiesMixed_CorrectOrderReturned()
    {
        TestingOrderableItems.ApplyOrderByOptions(new RepeatedField<OrderByOption>
        {
            new OrderByOption{PropertyName = "Name", IsAscendingOrder = true},
            new OrderByOption{PropertyName = "Description", IsAscendingOrder = false}
        }).Should().BeEquivalentTo(
            TestingOrderableItems
                .OrderBy(x => x.Name)
                .ThenByDescending(x => x.Description),
            options => options.WithStrictOrdering()
        );
    }
    
    [Fact]
    public void ApplyOrderByOptions_NoOrderingOptionsGiven_QueryableReturnedUnmodified()
    {
        TestingOrderableItems.ApplyOrderByOptions(new RepeatedField<OrderByOption>())
            .Should().BeEquivalentTo(TestingOrderableItems, options => options.WithStrictOrdering());
    }

    [Fact]
    public void ApplyOptionalPropertyValueFilter_NoValueGiven_QueryableReturnedUnmodified()
    {
        TestingOrderableItems.ApplyOptionalPropertyValueFilter(null, nameof(QueryableExtensionsTestingObject.Name))
            .Should().BeEquivalentTo(TestingOrderableItems);
    }
    
    [Fact]
    public void ApplyOptionalPropertyValueFilter_ValueGiven_ExpectedResultsReturned()
    {
        var expectedObject = TestingDateTimeFilterableObjects.First();
        TestingDateTimeFilterableObjects
            .ApplyOptionalPropertyValueFilter(expectedObject.Name, nameof(QueryableExtensionsTestingObject.Name))
            .Should().BeEquivalentTo(new[] {expectedObject});
    }

    private static IQueryable<QueryableExtensionsTestingObject> TestingDateTimeFilterableObjects => new List<QueryableExtensionsTestingObject>
    {
        new("Yesterday", "", DateTime.Now.AddDays(-1)),
        new("Now", "", DateTime.Now),
        new("Tomorrow", "", DateTime.Now.AddDays(1)),
    }.AsQueryable();
    
    [Fact]
    public void ApplyDateTimeFilteringOptions_FilteringOptionsNull_QueryableReturnedUnmodified()
    {
        TestingDateTimeFilterableObjects.ApplyDateTimeFilterOptions(null, x => x.Start)
            .Should().BeEquivalentTo(TestingDateTimeFilterableObjects);
    }
    
    [Fact]
    public void ApplyDateTimeFilteringOptions_FilteringOptionsPropertiesNull_QueryableReturnedUnmodified()
    {
        TestingDateTimeFilterableObjects
            .ApplyDateTimeFilterOptions(
                new BasicDateTimeFilteringOptions { Earliest = null, Latest = null }, 
                x => x.Start
            ).Should().BeEquivalentTo(TestingDateTimeFilterableObjects);
    }


    // Earliest, latest, expected result indices
    public static readonly TheoryData<DateTime?, DateTime?, int[]> DateTimeFilteringTheoryData = new()
    {
        { DateTime.Now.AddHours(-1), null, new[] {1,2} },
        { DateTime.Now.AddDays(-2), null, new[] {0,1,2} },
        { DateTime.Now.AddHours(1), null, new[] {2} },
        { DateTime.Now.AddDays(2), null, Array.Empty<int>() },
        { null, DateTime.Now.AddHours(-1), new[] {0} },
        { null, DateTime.Now.AddHours(1), new[] {0,1} },
        { null, DateTime.Now.AddDays(-2), Array.Empty<int>() },
        { null, DateTime.Now.AddDays(2), new[] {0,1,2} },
        { DateTime.Now.AddDays(-2), DateTime.Now.AddDays(2), new [] {0,1,2} },
        { DateTime.Now.AddHours(-1), DateTime.Now.AddHours(1), new [] {1} }
    };
    
    [Theory]
    [MemberData(nameof(DateTimeFilteringTheoryData))]
    public void ApplyDateTimeFilteringOptions_FilteringOptionsProvided_CorrectResultsReturned(DateTime? earliest, DateTime? latest, int[] objectIndicesExpected)
    {
        var results = TestingDateTimeFilterableObjects
            .ApplyDateTimeFilterOptions(
                new BasicDateTimeFilteringOptions
                {
                    Earliest = earliest is not null ? Timestamp.FromDateTimeOffset(earliest.Value) : null,
                    Latest = latest is not null ? Timestamp.FromDateTimeOffset(latest.Value) : null
                },
                x => x.Start
            );
        
        results.Should()
            .BeEquivalentTo(TestingDateTimeFilterableObjects
                .Select((objectValue, index) => new {objectValue, index})
                .Where(item => objectIndicesExpected.Contains(item.index))
                .Select(x => x.objectValue)
            );
    }
}
using System.Net;
using FluentAssertions;
using JsonApiDotNetCore.Queries.Internal.Parsing;
using JsonApiDotNetCore.Queries.Internal.QueryableBuilding;
using JsonApiDotNetCore.Repositories;
using JsonApiDotNetCore.Serialization.Objects;
using JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using TestBuildingBlocks;
using Xunit;

namespace JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase;

public sealed class IsUpperCaseTests : IClassFixture<IntegrationTestContext<TestableStartup<IsUpperCaseDbContext>, IsUpperCaseDbContext>>
{
    private readonly IntegrationTestContext<TestableStartup<IsUpperCaseDbContext>, IsUpperCaseDbContext> _testContext;
    private readonly IsUpperCaseFakers _fakers = new();

    public IsUpperCaseTests(IntegrationTestContext<TestableStartup<IsUpperCaseDbContext>, IsUpperCaseDbContext> testContext)
    {
        _testContext = testContext;

        testContext.UseController<NewspapersController>();
        testContext.UseController<NewsArticlesController>();

        testContext.ConfigureServicesAfterStartup(services =>
        {
            services.AddScoped<IQueryExpressionParserFactory, IsUpperCaseParserFactory>();
            services.AddScoped<IQueryableFactory, IsUpperCaseQueryableFactory>();

            services.AddScoped(typeof(IResourceRepository<,>), typeof(IsUpperCaseRepository<,>));
            services.AddScoped(typeof(IResourceReadRepository<,>), typeof(IsUpperCaseRepository<,>));
            services.AddScoped(typeof(IResourceWriteRepository<,>), typeof(IsUpperCaseRepository<,>));
        });
    }

    [Fact]
    public async Task Can_filter_casing_on_primary_endpoint()
    {
        // Arrange
        List<NewsArticle> articles = _fakers.NewsArticle.Generate(2);

        articles[0].Headline = articles[0].Headline.ToLowerInvariant();
        articles[1].Headline = articles[1].Headline.ToUpperInvariant();

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await dbContext.ClearTableAsync<NewsArticle>();
            dbContext.NewsArticles.AddRange(articles);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/newsArticles?filter=isUpperCase(headline)";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Id.Should().Be(articles[1].StringId);
    }

    [Fact]
    public async Task Can_filter_casing_in_compound_expression_on_secondary_endpoint()
    {
        // Arrange
        Newspaper newspaper = _fakers.Newspaper.Generate();
        newspaper.Articles = _fakers.NewsArticle.Generate(3);

        newspaper.Articles[0].Headline = newspaper.Articles[0].Headline.ToUpperInvariant();
        newspaper.Articles[0].Content = newspaper.Articles[0].Content.ToUpperInvariant();

        newspaper.Articles[1].Headline = newspaper.Articles[1].Headline.ToUpperInvariant();
        newspaper.Articles[1].Content = newspaper.Articles[1].Content.ToLowerInvariant();

        newspaper.Articles[2].Headline = newspaper.Articles[2].Headline.ToLowerInvariant();
        newspaper.Articles[2].Content = newspaper.Articles[2].Content.ToLowerInvariant();

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            dbContext.Newspapers.Add(newspaper);
            await dbContext.SaveChangesAsync();
        });

        string route = $"/newspapers/{newspaper.StringId}/articles?filter=and(isUpperCase(headline),not(isUpperCase(content)))";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Id.Should().Be(newspaper.Articles[1].StringId);
    }

    [Fact]
    public async Task Can_filter_casing_in_included_resources()
    {
        // Arrange
        List<Newspaper> newspapers = _fakers.Newspaper.Generate(2);
        newspapers[0].FrontPageHeadline = newspapers[0].FrontPageHeadline.ToLowerInvariant();
        newspapers[1].FrontPageHeadline = newspapers[1].FrontPageHeadline.ToUpperInvariant();

        newspapers[1].Articles = _fakers.NewsArticle.Generate(3);
        newspapers[1].Articles[0].Headline = newspapers[1].Articles[0].Headline.ToLowerInvariant();
        newspapers[1].Articles[1].Headline = newspapers[1].Articles[1].Headline.ToUpperInvariant();

        await _testContext.RunOnDatabaseAsync(async dbContext =>
        {
            await dbContext.ClearTableAsync<Newspaper>();
            dbContext.Newspapers.AddRange(newspapers);
            await dbContext.SaveChangesAsync();
        });

        const string route = "/newspapers?filter=isUpperCase(frontPageHeadline)&include=articles&filter[articles]=isUpperCase(headline)";

        // Act
        (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

        // Assert
        httpResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        responseDocument.Data.ManyValue.ShouldHaveCount(1);
        responseDocument.Data.ManyValue[0].Type.Should().Be("newspapers");
        responseDocument.Data.ManyValue[0].Id.Should().Be(newspapers[1].StringId);

        responseDocument.Included.ShouldHaveCount(1);
        responseDocument.Included[0].Type.Should().Be("newsArticles");
        responseDocument.Included[0].Id.Should().Be(newspapers[1].Articles[1].StringId);
    }
}

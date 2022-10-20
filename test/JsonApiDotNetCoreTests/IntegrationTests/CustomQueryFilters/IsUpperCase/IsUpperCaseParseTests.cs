using System.ComponentModel.Design;
using System.Net;
using FluentAssertions;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Errors;
using JsonApiDotNetCore.Middleware;
using JsonApiDotNetCore.Queries;
using JsonApiDotNetCore.Queries.Expressions;
using JsonApiDotNetCore.QueryStrings.Internal;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Serialization.Objects;
using JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase.Extensibility;
using Microsoft.Extensions.Logging.Abstractions;
using TestBuildingBlocks;
using Xunit;

namespace JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase;

public sealed class IsUpperCaseParseTests
{
    private readonly FilterQueryStringParameterReader _reader;

    public IsUpperCaseParseTests()
    {
        var options = new JsonApiOptions();

        // @formatter:wrap_chained_method_calls chop_always
        // @formatter:keep_existing_linebreaks true

        IResourceGraph resourceGraph = new ResourceGraphBuilder(options, NullLoggerFactory.Instance)
            .Add<Newspaper, long>()
            .Add<NewsArticle, long>()
            .Build();

        // @formatter:keep_existing_linebreaks restore
        // @formatter:wrap_chained_method_calls restore

        var request = new JsonApiRequest
        {
            Kind = EndpointKind.Primary,
            PrimaryResourceType = resourceGraph.GetResourceType<Newspaper>(),
            IsCollection = true,
            IsReadOnly = true
        };

        var resourceFactory = new ResourceFactory(new ServiceContainer());
        var parserFactory = new IsUpperCaseParserFactory(resourceGraph, resourceFactory);

        _reader = new FilterQueryStringParameterReader(request, resourceGraph, options, parserFactory);
    }

    [Theory]
    [InlineData("filter", "isUpperCase", "( expected.")]
    [InlineData("filter", "isUpperCase(", "Attribute name expected.")]
    [InlineData("filter", "isUpperCase( ", "Unexpected whitespace.")]
    [InlineData("filter", "isUpperCase()", "Attribute name expected.")]
    [InlineData("filter", "isUpperCase('a')", "Attribute name expected.")]
    [InlineData("filter", "isUpperCase(some)", "Attribute 'some' does not exist on resource type 'newspapers'.")]
    [InlineData("filter", "isUpperCase(articles)", "Attribute 'articles' does not exist on resource type 'newspapers'.")]
    [InlineData("filter", "isUpperCase(null)", "Attribute 'null' does not exist on resource type 'newspapers'.")]
    [InlineData("filter", "isUpperCase(publicationDate)", "Attribute of type 'string' expected.")]
    [InlineData("filter", "isUpperCase(frontPageHeadline))", "End of expression expected.")]
    public void Reader_Read_Fails(string parameterName, string parameterValue, string errorMessage)
    {
        // Act
        Action action = () => _reader.Read(parameterName, parameterValue);

        // Assert
        InvalidQueryStringParameterException exception = action.Should().ThrowExactly<InvalidQueryStringParameterException>().And;

        exception.ParameterName.Should().Be(parameterName);
        exception.Errors.ShouldHaveCount(1);

        ErrorObject error = exception.Errors[0];
        error.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Title.Should().Be("The specified filter is invalid.");
        error.Detail.Should().Be(errorMessage);
        error.Source.ShouldNotBeNull();
        error.Source.Parameter.Should().Be(parameterName);
    }

    [Theory]
    [InlineData("filter", "isUpperCase(frontPageHeadline)", null, "isUpperCase(frontPageHeadline)")]
    [InlineData("filter[articles]", "isUpperCase(headline)", "articles", "isUpperCase(headline)")]
    [InlineData("filter[articles]", "or(isUpperCase(headline),isUpperCase(content))", "articles", "or(isUpperCase(headline),isUpperCase(content))")]
    [InlineData("filter[articles]", "isUpperCase(publishedIn.frontPageHeadline)", "articles", "isUpperCase(publishedIn.frontPageHeadline)")]
    public void Reader_Read_Succeeds(string parameterName, string parameterValue, string scopeExpected, string valueExpected)
    {
        // Act
        _reader.Read(parameterName, parameterValue);

        IReadOnlyCollection<ExpressionInScope> constraints = _reader.GetConstraints();

        // Assert
        ResourceFieldChainExpression? scope = constraints.Select(expressionInScope => expressionInScope.Scope).Single();
        scope?.ToString().Should().Be(scopeExpected);

        QueryExpression value = constraints.Select(expressionInScope => expressionInScope.Expression).Single();
        value.ToString().Should().Be(valueExpected);
    }
}

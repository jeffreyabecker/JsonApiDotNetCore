using Bogus;
using TestBuildingBlocks;

// @formatter:wrap_chained_method_calls chop_always
// @formatter:keep_existing_linebreaks true

namespace JsonApiDotNetCoreTests.IntegrationTests.CustomQueryFilters.IsUpperCase;

internal sealed class IsUpperCaseFakers : FakerContainer
{
    private readonly Lazy<Faker<Newspaper>> _lazyNewspaperFaker = new(() =>
        new Faker<Newspaper>()
            .UseSeed(GetFakerSeed())
            .RuleFor(newspaper => newspaper.FrontPageHeadline, faker => faker.Lorem.Sentence())
            .RuleFor(newspaper => newspaper.PublicationDate, faker => faker.Date.Recent()));

    private readonly Lazy<Faker<NewsArticle>> _lazyNewsArticleFaker = new(() =>
        new Faker<NewsArticle>()
            .UseSeed(GetFakerSeed())
            .RuleFor(newsArticle => newsArticle.Headline, faker => faker.Lorem.Sentence())
            .RuleFor(newsArticle => newsArticle.Content, faker => faker.Lorem.Paragraph()));

    public Faker<Newspaper> Newspaper => _lazyNewspaperFaker.Value;
    public Faker<NewsArticle> NewsArticle => _lazyNewsArticleFaker.Value;
}

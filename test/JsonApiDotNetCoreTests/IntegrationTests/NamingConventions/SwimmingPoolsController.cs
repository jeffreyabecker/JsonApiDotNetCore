using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Controllers;
using JsonApiDotNetCore.Services;
using Microsoft.Extensions.Logging;

namespace JsonApiDotNetCoreTests.IntegrationTests.NamingConventions;

public sealed class SwimmingPoolsController(
    IJsonApiOptions options, IResourceGraph resourceGraph, ILoggerFactory loggerFactory, IResourceService<SwimmingPool, int> resourceService)
    : JsonApiController<SwimmingPool, int>(options, resourceGraph, loggerFactory, resourceService);

using System.Security.Claims;
using AppInventory.Api.Controllers;
using AppInventory.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AppInventory.Tests.Unit.Controllers;

public class SearchControllerTests
{
    private readonly Mock<ISearchProvider> _search;
    private readonly SearchController _controller;

    public SearchControllerTests()
    {
        _search = new Mock<ISearchProvider>();
        _search.Setup(s => s.IsAvailable).Returns(true);
        _controller = new SearchController(_search.Object);
        _controller.ControllerContext = BuildContext("ReadOnly");
    }

    [Fact]
    public async Task SearchAsync_Returns400_WhenTermIsEmptyAsync()
    {
        var result = await _controller.SearchAsync(null, null, null, 1, 20, CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task SearchAsync_Returns501_WhenProviderUnavailableAsync()
    {
        _search.Setup(s => s.IsAvailable).Returns(false);
        var result = await _controller.SearchAsync("test", null, null, 1, 20, CancellationToken.None);
        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(501);
    }

    [Fact]
    public async Task SearchAsync_ReturnsOk_WithResultsAsync()
    {
        _search.Setup(s => s.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult(
                [new SearchResultItem("Application", 1, "MyApp", "a great app", 0.9)],
                1, IsAvailable: true));

        var result = await _controller.SearchAsync("myapp", null, null, 1, 20, CancellationToken.None);
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value as SearchResponseDto;
        dto!.TotalCount.Should().Be(1);
        dto.Items[0].Title.Should().Be("MyApp");
    }

    [Fact]
    public async Task SearchAsync_ReadOnlyUser_LimitsDocTypesToUserAsync()
    {
        SearchQuery? capturedQuery = null;
        _search.Setup(s => s.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
            .Callback<SearchQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(new SearchResult([], 0, IsAvailable: true));

        await _controller.SearchAsync("test", null, null, 1, 20, CancellationToken.None);

        capturedQuery!.AllowedDocumentationTypes.Should().ContainSingle().Which.Should().Be("User");
    }

    [Fact]
    public async Task SearchAsync_DeveloperUser_AllowsUserAndDeveloperDocTypesAsync()
    {
        _controller.ControllerContext = BuildContext("Developer");
        SearchQuery? capturedQuery = null;
        _search.Setup(s => s.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
            .Callback<SearchQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(new SearchResult([], 0, IsAvailable: true));

        await _controller.SearchAsync("test", null, null, 1, 20, CancellationToken.None);

        capturedQuery!.AllowedDocumentationTypes.Should().BeEquivalentTo(["User", "Developer"]);
    }

    [Fact]
    public async Task SearchAsync_AdminUser_AllowsAllDocTypesAsync()
    {
        _controller.ControllerContext = BuildContext("Admin");
        SearchQuery? capturedQuery = null;
        _search.Setup(s => s.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
            .Callback<SearchQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(new SearchResult([], 0, IsAvailable: true));

        await _controller.SearchAsync("test", null, null, 1, 20, CancellationToken.None);

        capturedQuery!.AllowedDocumentationTypes.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_PassesResourceTypeFilterAsync()
    {
        SearchQuery? capturedQuery = null;
        _search.Setup(s => s.SearchAsync(It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>()))
            .Callback<SearchQuery, CancellationToken>((q, _) => capturedQuery = q)
            .ReturnsAsync(new SearchResult([], 0, IsAvailable: true));

        await _controller.SearchAsync("test", ["Application"], null, 1, 20, CancellationToken.None);

        capturedQuery!.ResourceTypes.Should().ContainSingle().Which.Should().Be("Application");
    }

    private static ControllerContext BuildContext(string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}

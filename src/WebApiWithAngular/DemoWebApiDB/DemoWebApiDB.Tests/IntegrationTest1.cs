using DemoWebApiDB.Tests.TestInfrastructure;

using System.Net;
using FluentAssertions;
using System.Net.Http.Json;


namespace DemoWebApiDB.Tests;


/// <summary>
///     Demo showing Integration Testing
///     - With the WebApplicationFactory object provided through Dependency Injection
///     - And the HttpClient object being shared across all the Test Methods.
///     NOTE: 
///         The in-memory Test Database instantiated would be "scoped" in the DI container.
///         Meaning: all tests in this class would share the same instance of the ApplicationDbContext object.
///         PROBLEM: Running the tests parallelly, could cause race conditions, 
///                  or result in failures due to undeterministic test data.
///         SOLUTION: Ensure that each test creates an individual instance of the "factory" object!
/// </summary>
public sealed class IntegrationTest1
    : IClassFixture<CustomWebApplicationFactory>
{

    private readonly HttpClient _httpClient;


    public IntegrationTest1(CustomWebApplicationFactory factory)
    {
        // Receive the FIXTURE - CustomWebApplicationFactory object by Dependency Injection,
        // and initialize the HttpClient object.
        // NOTE: The ApplicationDbContext from the CustomWebApplicationFactory is a "scoped" DI service,
        //       so all test methods would access the same shared instance! This might not be always suitable.
        _httpClient = factory.CreateClient();
    }


    [Fact]
    public async Task CreateCategory_Return201_WhenValid()
    {
        // ----- Arrange
        var expectedResult = "Microsoft";

        // ----- Act 

        var response
            = await _httpClient.GetAsync( "api/testdemo", TestContext.Current.CancellationToken);

        // ----- Assert

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content 
            = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        /*********************
        If the controller returned:
            return Ok( new { Name = "Microsoft" } );

        Then, the content can be retrieved as a serialized object:
            var content
                = await response.Content.ReadFromJsonAsync<DtoModel>();
        ***********/

        content.Should().Be(expectedResult);
    }

}

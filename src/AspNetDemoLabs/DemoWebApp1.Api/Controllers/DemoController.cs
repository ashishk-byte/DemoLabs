using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DemoWebApp1.Api.Controllers;


/// <summary>
///     The API Controller used to demonstrate the XMLHttpRequest readyState transitions.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class DemoController : ControllerBase
{


    /// <summary>
    /// Returns a greeting message after an artificial delay.
    /// </summary>
    /// <remarks>
    /// The delay is intentionally introduced to allow the client-side
    /// JavaScript to clearly observe XHR readyState transitions.
    /// </remarks>
    /// <returns>A string containing "Hello world".</returns>
    /// <response code="200">Returns the greeting string.</response>
    [HttpGet("GetHello")]
    public async Task<IActionResult> GetHello()
    {
        // Simulate server-side processing delay
        await Task.Delay(3000);

        return Ok("Hello world");
    }


    [HttpGet("GetStream")]
    public async IAsyncEnumerable<string> GetStream()
    {
        string[] data = { "Hello", "from", ".NET 8", "Async", "Streams!" };

        foreach (var item in data)
        {
            // Simulate asynchronous work (e.g., database or external API call)
            await Task.Delay(2000);
            yield return item;          // Streams the item immediately
        }
    }

}

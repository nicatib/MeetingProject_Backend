using Meeting_Project.Services;
using Microsoft.AspNetCore.Mvc;

namespace Meeting_Project.Contollers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlightController : ControllerBase
    {
        private readonly IFlightApiService _flightApi;

        public FlightController(IFlightApiService flightApi)
        {
            _flightApi = flightApi;
        }

        [HttpGet("test-tracking")]
        public async Task<IActionResult> Test()
        {
            var flights = await _flightApi.GetFlightsAsync();
            return Ok(flights);
        }
    }
}
using Meeting_Project.Dtos.Flights;
using Meeting_Project.Entity;

namespace Meeting_Project.Services
{
    public interface IFlightApiService
    {
        Task<List<FlightDto>> GetFlightsAsync();
    }
}

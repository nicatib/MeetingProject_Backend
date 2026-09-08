using Meeting_Project.Dtos.Flights;
using Meeting_Project.Entity;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace Meeting_Project.Services
{
    public class FlightApiService : IFlightApiService
    {
        private readonly HttpClient _http;

        public FlightApiService(HttpClient http)
        {
            _http = http;
        }
        private double GetDouble(object value)
        {
            if (value == null) return 0;

            if (value is JsonElement je && je.ValueKind == JsonValueKind.Number)
                return je.GetDouble();

            if (double.TryParse(value.ToString(), out var d))
                return d;

            return 0;
        }

        private string GetString(object value)
        {
            return value?.ToString()?.Trim();
        }
        public async Task<List<FlightDto>> GetFlightsAsync()
        {
            var response = await _http.GetAsync("https://opensky-network.org/api/states/all");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var data = JsonSerializer.Deserialize<OpenSkyResponse>(
             json,
             new JsonSerializerOptions
             {
                 PropertyNameCaseInsensitive = true
             });
            Console.WriteLine(json);
            var result = new List<FlightDto>();

            if (data?.States == null)
                return result;

            foreach (var s in data.States)
            {
                result.Add(new FlightDto
                {
                    FlightNumber = GetString(s[1]),
                    Country = GetString(s[2]),
                    Longitude = GetDouble(s[5]),
                    Latitude = GetDouble(s[6]),
                    Altitude = GetDouble(s[7]),
                    OnGround = s[8] != null && bool.TryParse(s[8]?.ToString(), out var g) && g,
                    Speed = GetDouble(s[9])
                });
                Console.WriteLine($"CALLSIGN: {s[1]}");

            }

            return result;
        }
    }
}

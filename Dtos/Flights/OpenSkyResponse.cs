using System.Text.Json;

namespace Meeting_Project.Dtos.Flights
{
    public class OpenSkyResponse
    {
        public long Time { get; set; }
        public List<List<object>> States { get; set; }
    }

}

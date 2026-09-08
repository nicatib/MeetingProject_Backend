using Meeting_Project.Helper;

namespace Meeting_Project.Entity
{
    public class Flight : BaseEntity
    {
        public string FlightNumber { get; set; }

        public int StateGovId { get; set; }
        public StateGov StateGov { get; set; }

        public string? PlannedArrivedTime { get; set; }
        public string? RealArrivedTime { get; set; }

        public bool IsArrived { get; set; }

        public string? Airline { get; set; }
        public string? Latitude { get; set; }    
        public string? Longitude { get; set; }  
        public FlightStatus Status { get; set; }

    }
}

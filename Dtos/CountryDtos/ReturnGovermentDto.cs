namespace Meeting_Project.Dtos.CountryDtos
{
    public class ReturnGovermentDto
    {
        public string Name { get; set; }
        public string? FlightNumber { get; set; }

        public int Id { get; set; }
        public int CountryId { get;set; }
        public string CountryName { get; set; } 
        public bool IsMain { get; set; }
        public string? PlannedArrivedTime { get; set; }
        public string? RealArrivedTime { get; set; }
    }
}

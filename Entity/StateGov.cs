namespace Meeting_Project.Entity
{
    public class StateGov:BaseEntity
    {
        public int CountryId { get; set; }
        public Country Country { get; set; }
        public string Name { get; set; }
        public bool IsMain { get; set; }
        public Flight? Flights { get; set; }
       
    }
}

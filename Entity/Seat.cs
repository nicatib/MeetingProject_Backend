namespace Meeting_Project.Entity
{
    public class Seat:BaseEntity
    {
        public int? StateGovId { get; set; }
        public StateGov StateGov{ get; set; }
        public int SeatIndex { get; set; }
        public int CircleTableId { get; set; }
        public CircleTable CircleTable { get; set;}
    }
}

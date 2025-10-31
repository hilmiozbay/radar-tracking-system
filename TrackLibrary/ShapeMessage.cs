namespace TrackLibrary
{
    public class ShapeMessage
    {
        public string? AreaId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double MinLatitude { get; set; }
        public double MaxLatitude { get; set; }
        public double MinLongitude { get; set; }
        public double MaxLongitude { get; set; }
        public string Action { get; set; } = string.Empty; // "create", "update", "delete"
    }
}

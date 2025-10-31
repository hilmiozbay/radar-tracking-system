
using System.Collections.ObjectModel;
using System.Globalization;
using MongoDB.Bson;
using MongoDB.EntityFrameworkCore;

namespace TrackLibrary
{
   
    [Collection("tracks")]
    public class Track
    {
        public ObjectId Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public double Angle { get; set; }
        public double Speed { get; set; }
        public required string Environment { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;

        // IFF bilgileri - başlangıçta null olabilir, IFF servisi tarafından doldurulur

        public string? IffType { get; set; } // Dost, Düşman, Tarafsız, Bilinmeyen
        public string? Country { get; set; }
        public string? Callsign { get; set; }
        public DateTime? IffUpdatedAt { get; set; }
    }
}

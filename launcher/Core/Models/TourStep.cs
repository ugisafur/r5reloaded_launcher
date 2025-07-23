using System.Numerics;
using System.Windows;

namespace launcher.Core.Models
{
    public class TourStep(string title, string description, Rect geoRect, Vector2 translatePos)
    {
        public string Title { get; set; } = title;
        public string Description { get; set; } = description;
        public Rect geoRect { get; set; } = geoRect;
        public Vector2 translatePos { get; set; } = translatePos;
    }
} 
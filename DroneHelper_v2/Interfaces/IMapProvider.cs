using GMap.NET.WindowsPresentation;
using GMap.NET;
using System.Windows.Input;

namespace DroneHelper_v2.Interfaces
{
    public interface IMapProvider
    {
        void InitializeMap(PointLatLng position, int zoom);
        PointLatLng GetLatLngFromMousePosition(MouseEventArgs e);
        void AddMarker(GMapMarker marker);
        void RemoveMarker(GMapMarker marker);
        void ClearMarkers();
    }
}

using DroneHelper_v2.Interfaces;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using GMap.NET;
using System.Windows.Input;

namespace DroneHelper_v2.Logic
{
    public class GMapProviderWrapper : IMapProvider
    {
        private readonly GMapControl _mapControl;

        public GMapProviderWrapper(GMapControl mapControl)
        {
            _mapControl = mapControl;
        }

        public void InitializeMap(PointLatLng position, int zoom)
        {
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            _mapControl.MapProvider = GMapProviders.GoogleMap;
            _mapControl.Position = position;
            _mapControl.MaxZoom = zoom;
            _mapControl.Zoom = zoom;
        }

        public PointLatLng GetLatLngFromMousePosition(MouseEventArgs e)
        {
            var mousePos = e.GetPosition(_mapControl);
            return _mapControl.FromLocalToLatLng((int)mousePos.X, (int)mousePos.Y);
        }

        public void AddMarker(GMapMarker marker)
        {
            _mapControl.Markers.Add(marker);
        }

        public void RemoveMarker(GMapMarker marker)
        {
            _mapControl.Markers.Remove(marker);
        }

        public void ClearMarkers()
        {
            _mapControl.Markers.Clear();
        }
    }
}

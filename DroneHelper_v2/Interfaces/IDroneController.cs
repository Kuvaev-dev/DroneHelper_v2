using GMap.NET;
using System.Collections.Generic;

namespace DroneHelper_v2.Interfaces
{
    public interface IDroneController
    {
        bool HasDrones { get; }
        void AddDrone(PointLatLng position, double radius, double speed, System.Windows.Controls.ListBox coordinatesListBox);
        void InitializeAttack(double radius, double speed);
        void ExecuteAttack(List<(double lat, double lng)> enemies);
        void ResetDrones();
    }
}

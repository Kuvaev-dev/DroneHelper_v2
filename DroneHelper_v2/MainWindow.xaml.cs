﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;

namespace DroneHelper_v2
{
    public partial class MainWindow : Window
    {
        private static readonly Random _rnd = new Random();
        const int MaxDrones = 10;
        const int GenerationSize = 100;
        const int GenerationNumbers = 200;
        const double MutationProbability = 0.2;
        static double Radius;
        static double Speed;
        private List<GMapMarker> initialDroneMarkers = new List<GMapMarker>();
        private List<(double lat, double lng)> enemies = new List<(double lat, double lng)>();
        private List<Drone> drones = new List<Drone>();
        private HashSet<(double lat, double lng)> destroyedEnemies = new HashSet<(double lat, double lng)>();
        private List<(double lat, double lng)> initialDronePositions = new List<(double lat, double lng)>();

        private List<GMapMarker> droneMarkers = new List<GMapMarker>();
        private List<GMapMarker> enemyMarkers = new List<GMapMarker>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeMap();
        }

        private void InitializeMap()
        {
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            MapControl.MapProvider = GMapProviders.GoogleMap;
            MapControl.Position = new PointLatLng(55.0, 37.0);
            MapControl.MaxZoom = 3;
            MapControl.Zoom = 3;
        }

        private void btnGenerateEnemies_Click(object sender, RoutedEventArgs e)
        {
            GenerateEnemies(_rnd.Next(5, 20));
            DrawEnemies();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            if (drones.Count == 0)
            {
                MessageBox.Show("Будь ласка, розмістіть дронів на карті перед запуском алгоритму.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Radius = Convert.ToDouble(txtRadius.Text);
            Speed = Convert.ToDouble(txtSpeed.Text);

            List<KeyValuePair<int[], double>> generation = GenerateRandom();
            SortGeneration(generation);

            for (int getNum = 0; getNum < GenerationNumbers; getNum++)
            {
                generation = GenerateNewGeneration(generation, true);
                SortGeneration(generation);
            }

            int[] bestGenome = generation[0].Key;
            AttackEnemies(bestGenome);

            DrawEnemies();
        }

        private void MapControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(MapControl);
            var latLng = MapControl.FromLocalToLatLng((int)mousePos.X, (int)mousePos.Y);

            if (drones.Count < MaxDrones)
            {
                drones.Add(new Drone { Position = (latLng.Lat, latLng.Lng), Speed = Speed, Radius = Radius });
                initialDronePositions.Add((latLng.Lat, latLng.Lng)); // Сохраняем начальную позицию
                lstDroneCoordinates.Items.Add($"Drone {drones.Count}: Initial Lat: {latLng.Lat}, Lng: {latLng.Lng}");
                DrawDrones(null, false);
            }
            else
            {
                MessageBox.Show("Максимальна кількість дронів досягнута.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            drones.Clear();
            enemies.Clear();
            destroyedEnemies.Clear();
            lstDroneCoordinates.Items.Clear();
            droneMarkers.Clear();
            enemyMarkers.Clear();
            MapControl.Markers.Clear();

            Radius = 0;
            Speed = 0;
            txtRadius.Text = string.Empty;
            txtSpeed.Text = string.Empty;
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtRadius.Text) || !double.TryParse(txtRadius.Text, out Radius) || Radius <= 0)
            {
                MessageBox.Show("Будь ласка, введіть дійсне додатне число для радіуса.", "Помилка вводу", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtSpeed.Text) || !double.TryParse(txtSpeed.Text, out Speed) || Speed <= 0)
            {
                MessageBox.Show("Будь ласка, введіть дійсне додатне число для швидкості.", "Помилка вводу", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void GenerateEnemies(int count)
        {
            enemies.Clear();
            for (int i = 0; i < count; i++)
            {
                double lat = 55.0 + (_rnd.NextDouble() * 10 - 5);
                double lng = 37.0 + (_rnd.NextDouble() * 10 - 5);
                enemies.Add((lat, lng));
            }
            DrawEnemies();
        }

        private void DrawEnemies()
        {
            foreach (var marker in enemyMarkers)
            {
                MapControl.Markers.Remove(marker);
            }
            enemyMarkers.Clear();

            foreach (var enemy in enemies)
            {
                var enemyMarker = new GMapMarker(new PointLatLng(enemy.lat, enemy.lng))
                {
                    Shape = new Ellipse { Fill = Brushes.Red, Width = 10, Height = 10 }
                };
                MapControl.Markers.Add(enemyMarker);
                enemyMarkers.Add(enemyMarker);
            }

            foreach (var destroyed in destroyedEnemies)
            {
                var destroyedMarker = new GMapMarker(new PointLatLng(destroyed.lat, destroyed.lng))
                {
                    Shape = new Ellipse { Fill = Brushes.Green, Width = 10, Height = 10 }
                };
                MapControl.Markers.Add(destroyedMarker);
                enemyMarkers.Add(destroyedMarker);
            }
        }

        private void AttackEnemies(int[] bestGenome)
        {
            destroyedEnemies.Clear();
            lstDroneCoordinates.Items.Clear();
            List<(double lat, double lng)> attackPositions = new List<(double lat, double lng)>();

            for (int i = 0; i < initialDronePositions.Count; i++)
            {
                lstDroneCoordinates.Items.Add($"Drone {i + 1}: Initial Lat: {initialDronePositions[i].lat}, Lng: {initialDronePositions[i].lng}");
            }

            for (int i = 0; i < drones.Count; i++)
            {
                double droneLat = GetLatitude(bestGenome[i]);
                double droneLng = GetLongitude(bestGenome[i]);
                attackPositions.Add((droneLat, droneLng));

                foreach (var enemy in enemies.ToList())
                {
                    double distance = Math.Sqrt(Math.Pow(droneLat - enemy.lat, 2) + Math.Pow(droneLng - enemy.lng, 2));

                    if (distance <= Radius)
                    {
                        destroyedEnemies.Add(enemy);
                        enemies.Remove(enemy);
                    }
                }

                drones[i].Position = (droneLat, droneLng);
            }

            DrawDrones(attackPositions, true);
            DrawEnemies();
        }

        private double GetLatitude(int genome)
        {
            int latPart = genome & 0xffff;
            return 55.0 + (latPart * 10.0 / 0x10000) - 5;
        }

        private double GetLongitude(int genome)
        {
            int lngPart = (genome >> 16) & 0xffff;
            return 37.0 + (lngPart * 10.0 / 0x10000) - 5;
        }

        private List<KeyValuePair<int[], double>> GenerateRandom()
        {
            List<KeyValuePair<int[], double>> result = new List<KeyValuePair<int[], double>>();
            for (int i = 0; i < GenerationSize; i++)
            {
                int[] genome = new int[MaxDrones];
                for (int j = 0; j < MaxDrones; j++)
                {
                    genome[j] = _rnd.Next();
                }
                double fitness = Fitness(genome);
                result.Add(new KeyValuePair<int[], double>(genome, fitness));
            }
            return result;
        }

        private double Fitness(int[] genome)
        {
            HashSet<(double lat, double lng)> destroyedEnemies = new HashSet<(double lat, double lng)>();

            for (int i = 0; i < genome.Length; i++)
            {
                double droneLat = GetLatitude(genome[i]);
                double droneLng = GetLongitude(genome[i]);

                foreach (var enemy in enemies)
                {
                    if (Math.Sqrt(Math.Pow(droneLat - enemy.lat, 2) + Math.Pow(droneLng - enemy.lng, 2)) <= Radius)
                    {
                        destroyedEnemies.Add(enemy);
                    }
                }
            }

            return destroyedEnemies.Count;
        }

        private void SortGeneration(List<KeyValuePair<int[], double>> generation)
        {
            generation.Sort((x, y) => y.Value.CompareTo(x.Value));
            if (generation.Count > GenerationSize)
                generation.RemoveRange(GenerationSize, generation.Count - GenerationSize);
        }

        private List<KeyValuePair<int[], double>> GenerateNewGeneration(List<KeyValuePair<int[], double>> parents, bool useElitism)
        {
            List<KeyValuePair<int[], double>> result = new List<KeyValuePair<int[], double>>();

            if (useElitism)
                result.Add(parents[0]);

            while (result.Count < GenerationSize)
            {
                int parent1 = _rnd.Next(GenerationSize);
                int parent2 = _rnd.Next(GenerationSize);

                int[] child = new int[MaxDrones];
                for (int i = 0; i < MaxDrones; i++)
                {
                    child[i] = _rnd.NextDouble() < 0.5 ? parents[parent1].Key[i] : parents[parent2].Key[i];

                    if (_rnd.NextDouble() < MutationProbability)
                    {
                        child[i] ^= 1 << _rnd.Next(32);
                    }
                }

                double fitness = Fitness(child);
                result.Add(new KeyValuePair<int[], double>(child, fitness));
            }

            return result;
        }

        private void DrawDrones(List<(double lat, double lng)> attackPositions, bool isAttacking)
        {
            foreach (var marker in droneMarkers)
            {
                MapControl.Markers.Remove(marker);
            }
            droneMarkers.Clear();

            List<(double lat, double lng)> positionsToDisplay = isAttacking ? attackPositions : drones.Select(d => d.Position).ToList();

            foreach (var pos in positionsToDisplay)
            {
                var droneMarker = new GMapMarker(new PointLatLng(pos.lat, pos.lng))
                {
                    Shape = new Ellipse { Fill = Brushes.Blue, Width = 10, Height = 10 }
                };
                MapControl.Markers.Add(droneMarker);
                droneMarkers.Add(droneMarker);

                double adjustedRadius = Radius * (MapControl.Zoom / 3);
                var radiusMarker = new GMapMarker(new PointLatLng(pos.lat, pos.lng))
                {
                    Shape = new Ellipse
                    {
                        Fill = Brushes.Transparent,
                        Stroke = Brushes.Blue,
                        StrokeThickness = 1,
                        Width = adjustedRadius * 2,
                        Height = adjustedRadius * 2,
                        RenderTransform = new TranslateTransform(-adjustedRadius, -adjustedRadius)
                    }
                };
                MapControl.Markers.Add(radiusMarker);
                droneMarkers.Add(radiusMarker);
            }
        }
    }

    public class Drone
    {
        public (double lat, double lng) Position { get; set; }
        public double Speed { get; set; }
        public double Radius { get; set; }
    }
}

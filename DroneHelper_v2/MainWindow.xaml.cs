using System;
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
        // Генератор випадкових чисел
        private static readonly Random _rnd = new Random();

        // Максимальна кількість дронів
        const int MaxDrones = 10;

        // Розмір покоління
        const int GenerationSize = 100;

        // Кількість поколінь
        const int GenerationNumbers = 200;

        // Ймовірність мутації
        const double MutationProbability = 0.2;

        // Радіус дії дронів
        static double Radius;

        // Швидкість дронів
        static double Speed;

        // Список маркерів початкових позицій дронів
        private List<GMapMarker> initialDroneMarkers = new List<GMapMarker>();

        // Список координат ворогів
        private List<(double lat, double lng)> enemies = new List<(double lat, double lng)>();

        // Список дронів
        private List<Drone> drones = new List<Drone>();

        // Набір знищених ворогів
        private HashSet<(double lat, double lng)> destroyedEnemies = new HashSet<(double lat, double lng)>();

        // Список початкових позицій дронів
        private List<(double lat, double lng)> initialDronePositions = new List<(double lat, double lng)>();

        // Список маркерів дронів
        private List<GMapMarker> droneMarkers = new List<GMapMarker>();

        // Список маркерів ворогів
        private List<GMapMarker> enemyMarkers = new List<GMapMarker>();

        public MainWindow()
        {
            InitializeComponent(); // Ініціалізація компонентів вікна
            InitializeMap(); // Ініціалізація карти
        }

        // Метод для ініціалізації карти
        private void InitializeMap()
        {
            GMaps.Instance.Mode = AccessMode.ServerAndCache; // Встановлення режиму доступу до карт
            MapControl.MapProvider = GMapProviders.GoogleMap; // Встановлення постачальника карт
            MapControl.Position = new PointLatLng(55.0, 37.0); // Встановлення початкової позиції карти
            MapControl.MaxZoom = 3; // Максимальний рівень масштабування
            MapControl.Zoom = 3; // Початковий рівень масштабування
        }

        // Обробник події натискання кнопки для генерації ворогів
        private void btnGenerateEnemies_Click(object sender, RoutedEventArgs e)
        {
            GenerateEnemies(_rnd.Next(5, 20)); // Генерація випадкової кількості ворогів
            DrawEnemies(); // Відображення ворогів на карті
        }

        // Обробник події натискання кнопки для запуску алгоритму
        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            // Перевірка коректності введених даних
            if (!ValidateInput())
                return;

            // Перевірка наявності дронів перед запуском алгоритму
            if (drones.Count == 0)
            {
                MessageBox.Show("Будь ласка, розмістіть дронів на карті перед запуском алгоритму.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Зчитування значень радіусу та швидкості з текстових полів
            Radius = Convert.ToDouble(txtRadius.Text);
            Speed = Convert.ToDouble(txtSpeed.Text);

            // Генерація початкового покоління
            List<KeyValuePair<int[], double>> generation = GenerateRandom();
            SortGeneration(generation); // Сортування покоління

            // Генерація нових поколінь
            for (int getNum = 0; getNum < GenerationNumbers; getNum++)
            {
                generation = GenerateNewGeneration(generation, true);
                SortGeneration(generation);
            }

            // Отримання найкращого геному
            int[] bestGenome = generation[0].Key;
            AttackEnemies(bestGenome); // Атака ворогів за найкращим геномом

            DrawEnemies(); // Відображення ворогів на карті
        }

        // Обробник події натискання на карту для розміщення дронів
        private void MapControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(MapControl); // Отримання позиції миші на карті
            var latLng = MapControl.FromLocalToLatLng((int)mousePos.X, (int)mousePos.Y); // Перетворення позиції миші у координати

            // Перевірка, чи не перевищено максимальну кількість дронів
            if (drones.Count < MaxDrones)
            {
                // Додавання нового дрону до списку
                drones.Add(new Drone { Position = (latLng.Lat, latLng.Lng), Speed = Speed, Radius = Radius });
                initialDronePositions.Add((latLng.Lat, latLng.Lng)); // Збереження початкової позиції дрону
                lstDroneCoordinates.Items.Add($"Drone {drones.Count}: Initial Lat: {latLng.Lat}, Lng: {latLng.Lng}"); // Додавання інформації до списку
                DrawDrones(null, false); // Відображення дронів на карті
            }
            else
            {
                MessageBox.Show("Максимальна кількість дронів досягнута.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning); // Повідомлення про досягнення максимуму
            }
        }

        // Обробник події натискання кнопки для скидання карти
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            // Очищення всіх списків та маркерів
            drones.Clear();
            enemies.Clear();
            destroyedEnemies.Clear();
            lstDroneCoordinates.Items.Clear();
            droneMarkers.Clear();
            enemyMarkers.Clear();
            MapControl.Markers.Clear();

            // Скидання значень радіусу та швидкості
            Radius = 0;
            Speed = 0;
            txtRadius.Text = string.Empty;
            txtSpeed.Text = string.Empty;
        }

        // Метод для перевірки коректності введених даних
        private bool ValidateInput()
        {
            // Перевірка введеного радіусу
            if (string.IsNullOrWhiteSpace(txtRadius.Text) || !double.TryParse(txtRadius.Text, out Radius) || Radius <= 0)
            {
                MessageBox.Show("Будь ласка, введіть дійсне додатне число для радіуса.", "Помилка вводу", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // Перевірка введеної швидкості
            if (string.IsNullOrWhiteSpace(txtSpeed.Text) || !double.TryParse(txtSpeed.Text, out Speed) || Speed <= 0)
            {
                MessageBox.Show("Будь ласка, введіть дійсне додатне число для швидкості.", "Помилка вводу", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true; // Якщо всі перевірки пройдені, повертаємо true
        }

        // Метод для генерації ворогів
        private void GenerateEnemies(int count)
        {
            enemies.Clear(); // Очищення списку ворогів
            for (int i = 0; i < count; i++)
            {
                // Генерація випадкових координат ворогів
                double lat = 55.0 + (_rnd.NextDouble() * 10 - 5);
                double lng = 37.0 + (_rnd.NextDouble() * 10 - 5);
                enemies.Add((lat, lng)); // Додавання ворога до списку
            }
            DrawEnemies(); // Відображення ворогів на карті
        }

        // Метод для відображення ворогів на карті
        private void DrawEnemies()
        {
            // Видалення старих маркерів ворогів
            foreach (var marker in enemyMarkers)
            {
                MapControl.Markers.Remove(marker);
            }
            enemyMarkers.Clear(); // Очищення списку маркерів ворогів

            // Додавання нових маркерів ворогів
            foreach (var enemy in enemies)
            {
                var enemyMarker = new GMapMarker(new PointLatLng(enemy.lat, enemy.lng))
                {
                    Shape = new Ellipse { Fill = Brushes.Red, Width = 10, Height = 10 } // Встановлення кольору та розміру маркера
                };
                MapControl.Markers.Add(enemyMarker); // Додавання маркера на карту
                enemyMarkers.Add(enemyMarker); // Додавання маркера до списку
            }

            // Додавання маркерів знищених ворогів
            foreach (var destroyed in destroyedEnemies)
            {
                var destroyedMarker = new GMapMarker(new PointLatLng(destroyed.lat, destroyed.lng))
                {
                    Shape = new Ellipse { Fill = Brushes.Green, Width = 10, Height = 10 } // Встановлення кольору та розміру маркера
                };
                MapControl.Markers.Add(destroyedMarker); // Додавання маркера на карту
                enemyMarkers.Add(destroyedMarker); // Додавання маркера до списку
            }
        }

        // Метод для атаки ворогів
        private void AttackEnemies(int[] bestGenome)
        {
            destroyedEnemies.Clear(); // Очищення списку знищених ворогів
            lstDroneCoordinates.Items.Clear(); // Очищення списку координат дронів
            List<(double lat, double lng)> attackPositions = new List<(double lat, double lng)>(); // Список для зберігання атакуючих позицій

            // Додавання початкових координат дронів до списку
            for (int i = 0; i < initialDronePositions.Count; i++)
            {
                lstDroneCoordinates.Items.Add($"Drone {i + 1}: Initial Lat: {initialDronePositions[i].lat}, Lng: {initialDronePositions[i].lng}");
            }

            // Цикл по всіх дронах
            for (int i = 0; i < drones.Count; i++)
            {
                // Отримання координат дрону за його геномом
                double droneLat = GetLatitude(bestGenome[i]);
                double droneLng = GetLongitude(bestGenome[i]);
                attackPositions.Add((droneLat, droneLng)); // Додавання координат до списку атакуючих позицій

                // Обчислення відстані до початкової позиції дрону
                double distance = Math.Sqrt(Math.Pow(droneLat - initialDronePositions[i].lat, 2) + Math.Pow(droneLng - initialDronePositions[i].lng, 2));
                double timeOfFlight = distance / Speed; // Обчислення часу дольоту

                // Додавання інформації про атаку до списку
                lstDroneCoordinates.Items.Add($"Drone {i + 1}: Attack Lat: {droneLat}, Lng: {droneLng}, Time of Flight: {timeOfFlight:F2} hours");

                // Перевірка наявності ворогів в радіусі дії дрону
                foreach (var enemy in enemies.ToList())
                {
                    double enemyDistance = Math.Sqrt(Math.Pow(droneLat - enemy.lat, 2) + Math.Pow(droneLng - enemy.lng, 2));

                    // Якщо ворог знаходиться в радіусі дії, він знищується
                    if (enemyDistance <= Radius)
                    {
                        destroyedEnemies.Add(enemy); // Додавання ворога до списку знищених
                        enemies.Remove(enemy); // Видалення ворога зі списку
                    }
                }

                // Оновлення позиції дрону
                drones[i].Position = (droneLat, droneLng);
            }

            DrawDrones(attackPositions, true); // Відображення дронів на карті
            DrawEnemies(); // Відображення ворогів на карті
        }

        // Метод для отримання широти з геному
        private double GetLatitude(int genome)
        {
            int latPart = genome & 0xffff; // Отримання частини, що відповідає широті
            return 55.0 + (latPart * 10.0 / 0x10000) - 5; // Перетворення частини в широту
        }

        // Метод для отримання довготи з геному
        private double GetLongitude(int genome)
        {
            int lngPart = (genome >> 16) & 0xffff; // Отримання частини, що відповідає довготі
            return 37.0 + (lngPart * 10.0 / 0x10000) - 5; // Перетворення частини в довготу
        }

        // Метод для генерації випадкового покоління
        private List<KeyValuePair<int[], double>> GenerateRandom()
        {
            List<KeyValuePair<int[], double>> result = new List<KeyValuePair<int[], double>>();
            for (int i = 0; i < GenerationSize; i++)
            {
                int[] genome = new int[MaxDrones]; // Створення нового геному
                for (int j = 0; j < MaxDrones; j++)
                {
                    genome[j] = _rnd.Next(); // Генерація випадкового значення для кожного дрону
                }
                double fitness = Fitness(genome); // Обчислення придатності геному
                result.Add(new KeyValuePair<int[], double>(genome, fitness)); // Додавання геному до результату
            }
            return result; // Повернення згенерованого покоління
        }

        // Метод для обчислення придатності геному
        private double Fitness(int[] genome)
        {
            HashSet<(double lat, double lng)> destroyedEnemies = new HashSet<(double lat, double lng)>(); // Набір знищених ворогів

            // Цикл по всіх дронах
            for (int i = 0; i < genome.Length; i++)
            {
                double droneLat = GetLatitude(genome[i]); // Отримання широти дрону
                double droneLng = GetLongitude(genome[i]); // Отримання довготи дрону

                // Перевірка наявності ворогів в радіусі дії дрону
                foreach (var enemy in enemies)
                {
                    if (Math.Sqrt(Math.Pow(droneLat - enemy.lat, 2) + Math.Pow(droneLng - enemy.lng, 2)) <= Radius)
                    {
                        destroyedEnemies.Add(enemy); // Додавання ворога до списку знищених
                    }
                }
            }

            return destroyedEnemies.Count; // Повернення кількості знищених ворогів
        }

        // Метод для сортування покоління
        private void SortGeneration(List<KeyValuePair<int[], double>> generation)
        {
            generation.Sort((x, y) => y.Value.CompareTo(x.Value)); // Сортування за спаданням придатності
            if (generation.Count > GenerationSize)
                generation.RemoveRange(GenerationSize, generation.Count - GenerationSize); // Обрізання зайвих елементів
        }

        // Метод для генерації нового покоління
        private List<KeyValuePair<int[], double>> GenerateNewGeneration(List<KeyValuePair<int[], double>> parents, bool useElitism)
        {
            List<KeyValuePair<int[], double>> result = new List<KeyValuePair<int[], double>>();

            // Додавання найкращого геному, якщо використовується елітизм
            if (useElitism)
                result.Add(parents[0]);

            // Генерація нових геномів
            while (result.Count < GenerationSize)
            {
                int parent1 = _rnd.Next(GenerationSize); // Вибір першого батька
                int parent2 = _rnd.Next(GenerationSize); // Вибір другого батька

                int[] child = new int[MaxDrones]; // Створення нового геному для дитини
                for (int i = 0; i < MaxDrones; i++)
                {
                    // Вибір гена від одного з батьків
                    child[i] = _rnd.NextDouble() < 0.5 ? parents[parent1].Key[i] : parents[parent2].Key[i];

                    // Мутація з певною ймовірністю
                    if (_rnd.NextDouble() < MutationProbability)
                    {
                        child[i] ^= 1 << _rnd.Next(32); // Зміна випадкового біта
                    }
                }

                double fitness = Fitness(child); // Обчислення придатності дитини
                result.Add(new KeyValuePair<int[], double>(child, fitness)); // Додавання дитини до результату
            }

            return result; // Повернення нового покоління
        }

        // Метод для відображення дронів на карті
        private void DrawDrones(List<(double lat, double lng)> attackPositions, bool isAttacking)
        {
            // Видалення старих маркерів дронів
            foreach (var marker in droneMarkers)
            {
                MapControl.Markers.Remove(marker);
            }
            droneMarkers.Clear(); // Очищення списку маркерів дронів

            // Вибір позицій для відображення
            List<(double lat, double lng)> positionsToDisplay = isAttacking ? attackPositions : drones.Select(d => d.Position).ToList();

            // Додавання нових маркерів дронів
            foreach (var pos in positionsToDisplay)
            {
                var droneMarker = new GMapMarker(new PointLatLng(pos.lat, pos.lng))
                {
                    Shape = new Ellipse { Fill = Brushes.Blue, Width = 10, Height = 10 } // Встановлення кольору та розміру маркера
                };
                MapControl.Markers.Add(droneMarker); // Додавання маркера на карту
                droneMarkers.Add(droneMarker); // Додавання маркера до списку

                // Обчислення радіусу дії дрону
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
                        RenderTransform = new TranslateTransform(-adjustedRadius, -adjustedRadius) // Центрування маркера
                    }
                };
                MapControl.Markers.Add(radiusMarker); // Додавання радіусу на карту
                droneMarkers.Add(radiusMarker); // Додавання маркера до списку
            }
        }
    }

    // Клас для представлення дрону
    public class Drone
    {
        // Позиція дрону
        public (double lat, double lng) Position { get; set; }

        // Швидкість дрону
        public double Speed { get; set; }

        // Радіус дії дрону
        public double Radius { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace laba2
{
    /// <summary>
    /// Функция, построенная методом IDW (Inverse Distance Weighting)
    /// по точкам, заданным пользователем
    /// </summary>
    public class IDWFunction : IFunction
    {
        private List<PointD> points;
        private double power; // Степень для IDW (обычно 2)
        private string functionName;

        public IDWFunction(string name = "Пользовательский график")
        {
            points = new List<PointD>();
            power = 2.0; // Стандартная степень для IDW
            functionName = name;
        }

        public IDWFunction(List<PointD> initialPoints, string name = "Пользовательский график", double idwPower = 2.0)
        {
            points = new List<PointD>(initialPoints);
            power = idwPower;
            functionName = name;
            // Сортируем точки по x для быстрого поиска
            points = points.OrderBy(p => p.X).ToList();
        }

        public void AddPoint(double x, double y)
        {
            // Удаляем точку с таким же x, если она существует
            points.RemoveAll(p => Math.Abs(p.X - x) < 1e-10);
            points.Add(new PointD(x, y));
            points = points.OrderBy(p => p.X).ToList();
        }

        public void AddPoint(PointD point)
        {
            AddPoint(point.X, point.Y);
        }

        public void ClearPoints()
        {
            points.Clear();
        }

        public int PointCount => points.Count;

        public List<PointD> GetPoints() => new List<PointD>(points);

        public string Name => functionName;

        public void SetName(string name)
        {
            functionName = name;
        }

        public double Calc(double x)
        {
            if (points.Count == 0)
                return double.NaN;

            // Если есть точка с точно таким же x, возвращаем её y
            var exactMatch = points.FirstOrDefault(p => Math.Abs(p.X - x) < 1e-10);
            if (exactMatch != null)
                return exactMatch.Y;

            // Если только одна точка, возвращаем её y
            if (points.Count == 1)
                return points[0].Y;

            // Метод IDW (Inverse Distance Weighting)
            double numerator = 0.0;
            double denominator = 0.0;
            const double epsilon = 1e-10; // Малое значение для избежания деления на ноль

            foreach (var point in points)
            {
                double distance = Math.Abs(x - point.X);
                
                // Если расстояние очень мало, возвращаем значение точки
                if (distance < epsilon)
                    return point.Y;

                double weight = 1.0 / Math.Pow(distance, power);
                numerator += weight * point.Y;
                denominator += weight;
            }

            if (Math.Abs(denominator) < epsilon)
                return double.NaN;

            return numerator / denominator;
        }

        // Метод для сохранения точек в строку (для сериализации)
        public string SerializePoints()
        {
            return string.Join(";", points.Select(p => $"{p.X}:{p.Y}"));
        }

        // Метод для загрузки точек из строки
        public void DeserializePoints(string data)
        {
            points.Clear();
            if (string.IsNullOrWhiteSpace(data))
                return;

            var parts = data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var coords = part.Split(':');
                if (coords.Length == 2)
                {
                    if (double.TryParse(coords[0], out double x) && double.TryParse(coords[1], out double y))
                    {
                        points.Add(new PointD(x, y));
                    }
                }
            }
            points = points.OrderBy(p => p.X).ToList();
        }
    }

    /// <summary>
    /// Класс для представления точки с координатами double
    /// </summary>
    public class PointD
    {
        public double X { get; set; }
        public double Y { get; set; }

        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }

        public PointD()
        {
            X = 0;
            Y = 0;
        }
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace laba2
{
    /// <summary>
    /// Менеджер для сохранения и загрузки пользовательских графиков
    /// </summary>
    public class UserGraphsManager
    {
        private static string GetDataFilePath()
        {
            // Сохраняем файл в папке приложения
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Laba2Graphs"
            );
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            return Path.Combine(appDataPath, "user_graphs.xml");
        }

        /// <summary>
        /// Сохраняет список пользовательских графиков в файл
        /// </summary>
        public static void SaveGraphs(List<IDWFunction> graphs)
        {
            try
            {
                string filePath = GetDataFilePath();
                XDocument doc = new XDocument(
                    new XElement("UserGraphs",
                        graphs.Select(graph => new XElement("Graph",
                            new XAttribute("Name", graph.Name),
                            new XAttribute("Points", graph.SerializePoints()),
                            new XAttribute("Power", "2.0")
                        ))
                    )
                );
                doc.Save(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении графиков: {ex.Message}");
            }
        }

        /// <summary>
        /// Загружает список пользовательских графиков из файла
        /// </summary>
        public static List<IDWFunction> LoadGraphs()
        {
            List<IDWFunction> graphs = new List<IDWFunction>();
            try
            {
                string filePath = GetDataFilePath();
                if (!File.Exists(filePath))
                    return graphs;

                XDocument doc = XDocument.Load(filePath);
                var graphElements = doc.Root?.Elements("Graph");
                
                if (graphElements != null)
                {
                    foreach (var elem in graphElements)
                    {
                        string name = elem.Attribute("Name")?.Value ?? "Пользовательский график";
                        string pointsData = elem.Attribute("Points")?.Value ?? "";
                        string powerStr = elem.Attribute("Power")?.Value ?? "2.0";
                        
                        if (double.TryParse(powerStr, out double power))
                        {
                            var graph = new IDWFunction(name);
                            graph.DeserializePoints(pointsData);
                            graphs.Add(graph);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке графиков: {ex.Message}");
            }

            return graphs;
        }
    }
}


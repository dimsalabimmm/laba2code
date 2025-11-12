using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace laba2
{
    /// <summary>
    /// Менеджер для синхронизации графиков между экземплярами приложения через сокеты
    /// </summary>
    public class GraphSocketManager : IDisposable
    {
        private const int PORT = 12345;
        private const string LOCALHOST = "127.0.0.1";
        private TcpListener server;
        private bool isServerRunning = false;
        private Thread serverThread;
        private Form1 parentForm;
        private List<IDWFunction> currentGraphs;
        private readonly object graphsLock = new object();

        public GraphSocketManager(Form1 form)
        {
            parentForm = form;
            currentGraphs = new List<IDWFunction>();
        }

        /// <summary>
        /// Проверяет, есть ли другие запущенные экземпляры
        /// </summary>
        public bool CheckForOtherInstances()
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(LOCALHOST, PORT, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
                    
                    if (success)
                    {
                        client.EndConnect(result);
                        return true; // Другой экземпляр найден
                    }
                }
            }
            catch
            {
                // Не удалось подключиться - значит других экземпляров нет
            }
            return false;
        }

        /// <summary>
        /// Запрашивает графики у других экземпляров
        /// </summary>
        public List<IDWFunction> RequestGraphsFromOtherInstances()
        {
            List<IDWFunction> graphs = new List<IDWFunction>();
            
            try
            {
                using (var client = new TcpClient())
                {
                    // Устанавливаем таймаут подключения
                    client.ReceiveTimeout = 10000;
                    client.SendTimeout = 10000;
                    
                    client.Connect(LOCALHOST, PORT);
                    
                    using (var stream = client.GetStream())
                    {
                        stream.ReadTimeout = 10000;
                        stream.WriteTimeout = 10000;
                        
                        using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                        using (var reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            // Отправляем запрос на получение графиков
                            writer.WriteLine("GET_GRAPHS");
                            writer.Flush();

                            // Читаем ответ
                            string response = reader.ReadLine();
                            if (response == null)
                            {
                                System.Diagnostics.Debug.WriteLine("Получен null ответ от сервера");
                                return graphs;
                            }
                            
                            if (response == "GRAPHS_DATA")
                            {
                                // Читаем количество графиков
                                string countStr = reader.ReadLine();
                                if (countStr == null)
                                {
                                    System.Diagnostics.Debug.WriteLine("Не удалось прочитать количество графиков");
                                    return graphs;
                                }
                                
                                if (int.TryParse(countStr, out int count))
                                {
                                    System.Diagnostics.Debug.WriteLine($"Ожидается получение {count} графиков");
                                    
                                    for (int i = 0; i < count; i++)
                                    {
                                        string name = reader.ReadLine();
                                        string pointsData = reader.ReadLine();
                                        
                                        if (name == null || pointsData == null)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Ошибка чтения графика {i + 1}: получен null");
                                            break;
                                        }
                                        
                                        if (!string.IsNullOrEmpty(pointsData) && !string.IsNullOrEmpty(name))
                                        {
                                            try
                                            {
                                                var graph = new IDWFunction(name);
                                                graph.DeserializePoints(pointsData);
                                                if (graph.PointCount > 0)
                                                {
                                                    graphs.Add(graph);
                                                    System.Diagnostics.Debug.WriteLine($"Успешно получен график: {name} с {graph.PointCount} точками");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"Ошибка при десериализации графика: {ex.Message}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сокета при запросе графиков: {ex.Message}");
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка IO при запросе графиков: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запросе графиков: {ex.Message}");
            }
            
            return graphs;
        }

        /// <summary>
        /// Запускает сервер для прослушивания запросов от других экземпляров
        /// </summary>
        public void StartServer(List<IDWFunction> graphs)
        {
            if (isServerRunning) return;

            try
            {
                server = new TcpListener(IPAddress.Parse(LOCALHOST), PORT);
                server.Start();
                isServerRunning = true;

                serverThread = new Thread(() => ServerLoop(graphs));
                serverThread.IsBackground = true;
                serverThread.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запуске сервера: {ex.Message}");
                isServerRunning = false;
            }
        }

        private void ServerLoop(List<IDWFunction> initialGraphs)
        {
            // Инициализируем список графиков
            lock (graphsLock)
            {
                currentGraphs = new List<IDWFunction>(initialGraphs);
            }

            while (isServerRunning)
            {
                try
                {
                    var client = server.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(HandleClient, client);
                }
                catch (ObjectDisposedException)
                {
                    // Сервер был остановлен
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка в серверном цикле: {ex.Message}");
                }
            }
        }

        private void HandleClient(object state)
        {
            TcpClient client = null;
            try
            {
                client = (TcpClient)state;
                
                using (var stream = client.GetStream())
                {
                    stream.ReadTimeout = 10000;
                    stream.WriteTimeout = 10000;
                    
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        string request = reader.ReadLine();
                        
                        if (request == "GET_GRAPHS")
                        {
                            // Получаем актуальный список графиков
                            List<IDWFunction> graphsToSend;
                            lock (graphsLock)
                            {
                                graphsToSend = new List<IDWFunction>(currentGraphs);
                            }
                            
                            // Отправляем только графики с точками
                            var graphsWithPoints = graphsToSend.Where(g => g.PointCount > 0).ToList();
                            
                            System.Diagnostics.Debug.WriteLine($"Отправка {graphsWithPoints.Count} графиков клиенту");
                            
                            writer.WriteLine("GRAPHS_DATA");
                            writer.Flush();
                            
                            writer.WriteLine(graphsWithPoints.Count.ToString());
                            writer.Flush();

                            foreach (var graph in graphsWithPoints)
                            {
                                string name = graph.Name ?? "Безымянный график";
                                string pointsData = graph.SerializePoints();
                                
                                writer.WriteLine(name);
                                writer.Flush();
                                
                                writer.WriteLine(pointsData);
                                writer.Flush();
                                
                                System.Diagnostics.Debug.WriteLine($"Отправлен график: {name}");
                            }
                            
                            // Убеждаемся, что все данные отправлены
                            writer.Flush();
                            System.Diagnostics.Debug.WriteLine("Все графики отправлены клиенту");
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка IO при обработке клиента: {ex.Message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обработке клиента: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (client != null)
                    {
                        client.Close();
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Обновляет список графиков на сервере
        /// </summary>
        public void UpdateServerGraphs(List<IDWFunction> graphs)
        {
            lock (graphsLock)
            {
                currentGraphs = new List<IDWFunction>(graphs);
            }
        }

        public void StopServer()
        {
            isServerRunning = false;
            try
            {
                server?.Stop();
            }
            catch { }
        }

        public void Dispose()
        {
            StopServer();
        }
    }
}


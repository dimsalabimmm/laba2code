using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const int BASE_PORT = 12345;
        private const int PORT_RANGE = 100; // Диапазон портов для поиска других экземпляров
        private const string LOCALHOST = "127.0.0.1";
        private TcpListener server;
        private int myPort;
        private bool isServerRunning = false;
        private Thread serverThread;
        private Form1 parentForm;
        private List<string> currentSelectedGraphs;
        private readonly object graphsLock = new object();
        private System.Threading.Timer syncTimer;
        private bool isDisposed = false;

        public GraphSocketManager(Form1 form)
        {
            parentForm = form;
            currentSelectedGraphs = new List<string>();
            
            // Определяем порт для этого экземпляра (используем хеш процесса для уникальности)
            myPort = BASE_PORT + (Math.Abs(Process.GetCurrentProcess().Id.GetHashCode()) % PORT_RANGE);
            
            // Запускаем таймер для периодического сохранения и синхронизации
            syncTimer = new System.Threading.Timer(SyncTimerCallback, null, 500, 500); // Каждые 500мс
        }

        /// <summary>
        /// Проверяет, есть ли другие запущенные экземпляры
        /// </summary>
        public bool CheckForOtherInstances()
        {
            // Проверяем все порты в диапазоне, кроме своего
            for (int port = BASE_PORT; port < BASE_PORT + PORT_RANGE; port++)
            {
                if (port == myPort) continue;
                
                try
                {
                    using (var client = new TcpClient())
                    {
                        var result = client.BeginConnect(LOCALHOST, port, null, null);
                        var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(100));
                        
                        if (success)
                        {
                            client.EndConnect(result);
                            return true; // Найден активный экземпляр
                        }
                    }
                }
                catch
                {
                    // Порт не активен, продолжаем поиск
                }
            }
            
            return false;
        }

        /// <summary>
        /// Получает список всех выбранных графиков от всех других экземпляров
        /// </summary>
        public List<string> GetAllSelectedGraphsFromOthers()
        {
            List<string> allSelectedGraphs = new List<string>();
            
            // Опрашиваем все порты в диапазоне, кроме своего
            for (int port = BASE_PORT; port < BASE_PORT + PORT_RANGE; port++)
            {
                if (port == myPort) continue;
                
                try
                {
                    using (var client = new TcpClient())
                    {
                        // Устанавливаем таймаут подключения
                        client.ReceiveTimeout = 1000;
                        client.SendTimeout = 1000;
                        
                        var result = client.BeginConnect(LOCALHOST, port, null, null);
                        var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(200));
                        
                        if (!success)
                        {
                            continue; // Порт не активен, пробуем следующий
                        }
                        
                        client.EndConnect(result);
                        
                        using (var stream = client.GetStream())
                        {
                            stream.ReadTimeout = 1000;
                            stream.WriteTimeout = 1000;
                            
                            using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                            using (var reader = new StreamReader(stream, Encoding.UTF8))
                            {
                                // Отправляем запрос на получение выбранных графиков
                                writer.WriteLine("GET_SELECTED_GRAPHS");
                                writer.Flush();

                                // Читаем ответ
                                string response = reader.ReadLine();
                                if (response == null)
                                {
                                    continue;
                                }
                                
                                if (response == "SELECTED_GRAPHS_DATA")
                                {
                                    // Читаем количество графиков
                                    string countStr = reader.ReadLine();
                                    if (countStr == null)
                                    {
                                        continue;
                                    }
                                    
                                    if (int.TryParse(countStr, out int count))
                                    {
                                        for (int i = 0; i < count; i++)
                                        {
                                            string graphName = reader.ReadLine();
                                            
                                            if (graphName == null)
                                            {
                                                break;
                                            }
                                            
                                            if (!string.IsNullOrEmpty(graphName) && !allSelectedGraphs.Contains(graphName))
                                            {
                                                allSelectedGraphs.Add(graphName);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Игнорируем ошибки при подключении к неактивным портам
                }
            }
            
            return allSelectedGraphs;
        }
        
        /// <summary>
        /// Сохраняет список выбранных графиков этого экземпляра
        /// </summary>
        public void SaveSelectedGraphs(List<string> selectedGraphs)
        {
            lock (graphsLock)
            {
                currentSelectedGraphs = new List<string>(selectedGraphs);
            }
        }

        /// <summary>
        /// Запускает сервер для прослушивания запросов от других экземпляров
        /// </summary>
        public void StartServer()
        {
            if (isServerRunning) return;

            try
            {
                server = new TcpListener(IPAddress.Parse(LOCALHOST), myPort);
                server.Start();
                isServerRunning = true;

                serverThread = new Thread(ServerLoop);
                serverThread.IsBackground = true;
                serverThread.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запуске сервера: {ex.Message}");
                isServerRunning = false;
            }
        }

        private void ServerLoop()
        {
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
                    stream.ReadTimeout = 2000;
                    stream.WriteTimeout = 2000;
                    
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        string request = reader.ReadLine();
                        
                        if (request == "GET_SELECTED_GRAPHS")
                        {
                            // Получаем актуальный список выбранных графиков
                            List<string> graphsToSend;
                            lock (graphsLock)
                            {
                                graphsToSend = new List<string>(currentSelectedGraphs);
                            }
                            
                            System.Diagnostics.Debug.WriteLine($"Отправка {graphsToSend.Count} выбранных графиков клиенту");
                            
                            writer.WriteLine("SELECTED_GRAPHS_DATA");
                            writer.Flush();
                            
                            writer.WriteLine(graphsToSend.Count.ToString());
                            writer.Flush();

                            foreach (var graphName in graphsToSend)
                            {
                                writer.WriteLine(graphName);
                                writer.Flush();
                                
                                System.Diagnostics.Debug.WriteLine($"Отправлено имя графика: {graphName}");
                            }

                            // Убеждаемся, что все данные отправлены
                            writer.Flush();
                            System.Diagnostics.Debug.WriteLine("Все имена графиков отправлены клиенту");
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

        private void SyncTimerCallback(object state)
        {
            if (isDisposed || parentForm == null || parentForm.IsDisposed)
                return;

            try
            {
                // Сохраняем текущие выбранные графики
                if (parentForm.IsHandleCreated)
                {
                    List<string> selectedGraphs = null;
                    
                    if (parentForm.InvokeRequired)
                    {
                        parentForm.Invoke(new Action(() =>
                        {
                            selectedGraphs = parentForm.GetSelectedGraphNames();
                        }));
                    }
                    else
                    {
                        selectedGraphs = parentForm.GetSelectedGraphNames();
                    }
                    
                    if (selectedGraphs != null)
                    {
                        SaveSelectedGraphs(selectedGraphs);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в таймере синхронизации: {ex.Message}");
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
            isDisposed = true;
            syncTimer?.Dispose();
            StopServer();
        }
    }
}


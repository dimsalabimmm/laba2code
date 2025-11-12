using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

namespace laba2
{
    /// <summary>
    /// Менеджер для синхронизации графиков между экземплярами через файловую систему
    /// </summary>
    public class GraphSyncManager : IDisposable
    {
        private static string GetSyncDirectory()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Laba2Graphs",
                "Sync"
            );
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            return appDataPath;
        }

        private Form1 parentForm;
        private System.Threading.Timer syncTimer;
        private bool isDisposed = false;
        private string instanceId;
        private string instanceFilePath;

        public GraphSyncManager(Form1 form)
        {
            parentForm = form;
            
            // Создаем уникальный ID для этого экземпляра
            instanceId = Guid.NewGuid().ToString();
            instanceFilePath = Path.Combine(GetSyncDirectory(), $"{instanceId}.xml");
            
            // Запускаем таймер для периодического сохранения и синхронизации
            syncTimer = new System.Threading.Timer(SyncTimerCallback, null, 500, 500); // Каждые 500мс
        }

        /// <summary>
        /// Проверяет наличие других запущенных экземпляров
        /// </summary>
        public bool CheckForOtherInstances()
        {
            try
            {
                string syncDir = GetSyncDirectory();
                var files = Directory.GetFiles(syncDir, "*.xml");
                
                // Проверяем, есть ли файлы от других экземпляров (не старше 2 секунд)
                foreach (var file in files)
                {
                    if (file != instanceFilePath)
                    {
                        var fileInfo = new FileInfo(file);
                        var age = DateTime.Now - fileInfo.LastWriteTime;
                        if (age.TotalSeconds < 2)
                        {
                            return true; // Найден активный экземпляр
                        }
                    }
                }
            }
            catch { }
            
            return false;
        }

        /// <summary>
        /// Получает список всех выбранных графиков от всех других экземпляров
        /// </summary>
        public List<string> GetAllSelectedGraphsFromOthers()
        {
            List<string> allSelectedGraphs = new List<string>();
            
            try
            {
                string syncDir = GetSyncDirectory();
                var files = Directory.GetFiles(syncDir, "*.xml");
                
                foreach (var file in files)
                {
                    if (file != instanceFilePath)
                    {
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            var age = DateTime.Now - fileInfo.LastWriteTime;
                            
                            // Читаем только файлы от активных экземпляров (обновленные не более 2 секунд назад)
                            if (age.TotalSeconds < 2)
                            {
                                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    XDocument doc = XDocument.Load(fs);
                                    var graphElements = doc.Root?.Elements("Graph");
                                    
                                    if (graphElements != null)
                                    {
                                        foreach (var elem in graphElements)
                                        {
                                            string graphName = elem.Attribute("Name")?.Value;
                                            if (!string.IsNullOrEmpty(graphName) && !allSelectedGraphs.Contains(graphName))
                                            {
                                                allSelectedGraphs.Add(graphName);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Игнорируем ошибки чтения отдельных файлов
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при чтении графиков от других экземпляров: {ex.Message}");
            }
            
            return allSelectedGraphs;
        }

        /// <summary>
        /// Сохраняет список выбранных графиков этого экземпляра
        /// </summary>
        public void SaveSelectedGraphs(List<string> selectedGraphs)
        {
            try
            {
                XDocument doc = new XDocument(
                    new XElement("Instance",
                        new XAttribute("Id", instanceId),
                        new XAttribute("Timestamp", DateTime.Now.Ticks),
                        selectedGraphs.Select(graphName => new XElement("Graph",
                            new XAttribute("Name", graphName)
                        ))
                    )
                );
                
                // Сохраняем во временный файл, затем переименовываем
                string tempPath = instanceFilePath + ".tmp";
                doc.Save(tempPath);
                
                if (File.Exists(instanceFilePath))
                {
                    File.Delete(instanceFilePath);
                }
                File.Move(tempPath, instanceFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении выбранных графиков: {ex.Message}");
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

        public void Dispose()
        {
            isDisposed = true;
            syncTimer?.Dispose();
            
            // Удаляем файл этого экземпляра при закрытии
            try
            {
                if (File.Exists(instanceFilePath))
                {
                    File.Delete(instanceFilePath);
                }
            }
            catch { }
        }
    }
}

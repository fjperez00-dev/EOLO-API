using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System;
using System.Timers;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TaskApiClient_WPF.Models;
using TaskApiClient_WPF.Services;

namespace TaskApiClient_WPF
{
    public partial class MainWindow : Window
    {
        #region Fields  

        private byte[]? selectedImageBytes;
        private string? selectedImageFilename;
        private string apiBaseUrl = "";
        private readonly ApiService _apiService;
        private string _connectionString = "";

        // Campos del StatusBar

        private   System.Timers.Timer _clockTimer;

        public string AppVersion { get; set; }
        public string DatabaseInfo { get; set; }
        public string ApiUrl { get; set; }
        public string CurrentDateTime { get; set; }

        #endregion

        public ObservableCollection<TaskExecutionResult> Results { get; set; } = new ObservableCollection<TaskExecutionResult>();

        public MainWindow()
        {
            InitializeComponent();
            _apiService = new ApiService();
            DataGridResults.ItemsSource = Results;
            LoadInfoStatusBar();
            InitialAPPValues();
        }

        #region Parametros APP

        private void InitialAPPValues()
        {            
            DatePickerStart.SelectedDate = DateTime.Now.AddMonths(-6);
            LoadUnits();
            Load_TaskStatusCodes();
            Load_appsettings();
        }
        private void Load_TaskStatusCodes()
        {
            var statusCodes = new[]
            {
                new { Code = "D", Display = "(D) Desestimado" },
                new { Code = "E", Display = "(E) Ejecutando" },
                new { Code = "F", Display = "(F) Diferido" },
                new { Code = "P", Display = "(P) Pendiente" },
                new { Code = "Z", Display = "(Z) Finalizado" }
            }.ToList();
            ComboTaskStatusCodeDone.ItemsSource = statusCodes;            
            ComboTaskStatusCodeDone.SelectedValuePath = "Code";
        }
        private void LoadUnits()
        {
            var units = new[]
            {
                new { Code = "44000", Display = "44000	Main Base RIYADH" },
                new { Code = "44500", Display = "44500	Corvette Base JEDDAH" },
                new { Code = "44538", Display = "44538	HMS ALJUBAIL" },
                new { Code = "44539", Display = "44539	HMS ALDIRIYAH" },
                new { Code = "44540", Display = "44540	HMS HAIL" },
                new { Code = "44541", Display = "44541	HMS JAZAN" },
                new { Code = "44542", Display = "44542	HMS UNAYZAH" },
                new { Code = "44600", Display = "44600	ROTA Naval Base" }
            }.ToList();

            ComboUnits.ItemsSource = units;
            ComboUnits.SelectedIndex = 0;
            ComboUnitDone.ItemsSource = units;
            ComboUnitDone.SelectedIndex = 0;
        }

        private void Load_appsettings()
        {
            // Leer appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _connectionString = $"{config.GetConnectionString("EOLOConnectionString")}{""}";
            apiBaseUrl = $"{config["ApiSettings:BaseUrl"]}{""}";
        }
        #endregion

        #region Botones
        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            // Validations
            if (ComboUnits.SelectedValue == null)
            {
                MessageBox.Show("Falta el campo: Unit Code", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (DatePickerStart.SelectedDate == null)
            {
                MessageBox.Show("Falta el campo: Initial Date", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (DatePickerEnd.SelectedDate == null)
            {
                MessageBox.Show("Falta el campo: Final Date", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string unit = $"{ComboUnits.SelectedValue.ToString()}{""}";
            string fechaIni =  DatePickerStart.SelectedDate.Value.ToString("ddMMyyyy");
            string fechaFin = DatePickerEnd.SelectedDate.Value.ToString("ddMMyyyy");

            try
            {
                BtnSearch.IsEnabled = false;
                Results.Clear();
                var items = await _apiService.GetTasksAsync(unit, fechaIni, fechaFin);
                foreach (var it in items)
                {
                    Results.Add(it);
                }
                if (!Results.Any())
                {
                    MessageBox.Show("No se han encontrado registros.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                txtResultados.Text = string.Format("Resultados: {0}", Results.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al llamar al API: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSearch.IsEnabled = true;
            }
        }

        private void BtnSelectImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = "Imagenes|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Todos los archivos|*.*";
            if (dlg.ShowDialog() == true)
            {
                FileInfo fileInfo = new FileInfo(dlg.FileName);
                const long MAX_SIZE = 3 * 1024 * 1024; // 3 MB

                if (fileInfo.Length > MAX_SIZE)
                {
                    MessageBox.Show("La imagen seleccionada supera el tamaño máximo de 3 MB.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                selectedImageFilename = dlg.FileName;
                selectedImageBytes = File.ReadAllBytes(selectedImageFilename);
                LblImageName.Text = System.IO.Path.GetFileName(selectedImageFilename);
                ShowImagePreview(selectedImageBytes);
            }
        }
                
        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {         

            TxtValidationErrors.Text = "";
            if (!ValidateInputs(out string validationErrors))
            {
                TxtValidationErrors.Text = validationErrors;
                return;
            }
            var unitCode = $"{ComboUnitDone.SelectedValue}{""}";
            var taskCode = TxtTaskCodeDone.Text.Trim();
            var funCode = TxtFunCodeDone.Text.Trim();
            var taskdate = DatePickerFechaDone.SelectedDate.ToString();
            var statusCode = ComboTaskStatusCodeDone.SelectedValue.ToString();

            BtnSend.IsEnabled = false;
            BtnSend.Content = "Enviando...";

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(60);
                var url = $"{apiBaseUrl}/api/Tasks";

                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(unitCode), "UnitCode");
                content.Add(new StringContent(taskCode), "TaskCode");
                content.Add(new StringContent(funCode), "FunCode");
                content.Add(new StringContent(taskdate), "TaskExecutionDate");
                content.Add(new StringContent(statusCode), "TaskExecutionStatusCode");

                if (selectedImageBytes != null && selectedImageFilename != null)
                {
                    var imageContent = new ByteArrayContent(selectedImageBytes);
                    var ext = System.IO.Path.GetExtension(selectedImageFilename).ToLowerInvariant();
                    string mediaType = ext switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".bmp" => "image/bmp",
                        ".gif" => "image/gif",
                        _ => "application/octet-stream"
                    };
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

                    content.Add(imageContent, "file", System.IO.Path.GetFileName(selectedImageFilename));
                }

                var resp = await client.PostAsync(url, content);
                if (resp.IsSuccessStatusCode)
                {
                    var respText = await resp.Content.ReadAsStringAsync();
                    MessageBox.Show("Enviado correctamente. Respuesta: " + respText, "OK");
                }
                else
                {
                    var respText = await resp.Content.ReadAsStringAsync();
                    MessageBox.Show($"Error del servidor: {resp.StatusCode}\n{respText}", "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error enviando: " + ex.Message, "Error");
            }
            finally
            {
                BtnSend.IsEnabled = true;
                BtnSend.Content = "Enviar";
            }
        }
            
        private void dataGridTasks_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Verificamos que haya una fila seleccionada
            if (DataGridResults.SelectedItem is TaskExecutionResult selectedTask)
            {                
                TxtTaskCodeDone.Text = selectedTask.TaskCode;
                TxtFunCodeDone.Text = selectedTask.FunCode;                
                ComboUnitDone.SelectedValue = selectedTask.UnitCode;                
                ComboTaskStatusCodeDone.SelectedValue = selectedTask.TaskExecutionStatusCode;
                DatePickerFechaDone.Text = selectedTask.TaskExecutionDate.ToString("dd/MM/yyyy");
            }
        }

        private async void BtnCargar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                dgTareas.ItemsSource = null;
                dgTareas.IsEnabled = false;
                Cursor = System.Windows.Input.Cursors.Wait;

                var tareas = await CargarTareasAsync();
                dgTareas.ItemsSource = tareas;

                MessageBox.Show($"Se cargaron {tareas.Count} registros correctamente.",
                                "Datos cargados",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los datos:\n{ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            finally
            {
                dgTareas.IsEnabled = true;
                Cursor = System.Windows.Input.Cursors.Arrow;
            }
        }

        #endregion

        private void LoadInfoStatusBar()
        {

            // Obtiene el ensamblado actual (el .exe de la aplicación)
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            //AppVersion = $"Versión: {Assembly.GetExecutingAssembly().GetName().Version}";
            AppVersion = $"Versión: {version.Major.ToString()}.{version.Minor.ToString()}.{version.Revision.ToString()}.{version.Build.ToString()}";
            CurrentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            _clockTimer = new  System.Timers.Timer(1000); // Actualiza cada segundo
            _clockTimer.Elapsed += (s, e) =>
            {
                CurrentDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                Dispatcher.Invoke(() => { DataContext = null; DataContext = this; });
            };
            _clockTimer.Start();

            try
            {
                var exePath = AppContext.BaseDirectory;
                var config = new ConfigurationBuilder()
                    .SetBasePath(exePath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                string connectionString = config.GetConnectionString("EOLOConnectionString") ?? "";
                string apiUrl = config["ApiSettings:BaseUrl"] ?? "";

                // 🔒 Solo mostrar parte de la cadena de conexión
                if (connectionString.Contains("Server="))
                {
                    var parts = connectionString.Split(';');
                    foreach (var part in parts)
                    {
                        if (part.TrimStart().StartsWith("Server="))
                            DatabaseInfo = part;
                    }
                }
                else
                {
                    DatabaseInfo = "Server: desconocido";
                }

                ApiUrl = $"API: {apiUrl}";
            }
            catch (Exception ex)
            {
                DatabaseInfo = "Error al cargar configuración";
                ApiUrl = ex.Message;
            }
        }
        

        private bool ValidateInputs(out string errors)
        {
            errors = "";
            if (ComboUnitDone.SelectedValue.ToString()=="")
                errors += "Seleccione Código Unidad.\n";

            if (string.IsNullOrWhiteSpace(TxtTaskCodeDone.Text))
                errors += "TaskCode es obligatorio.\n";
            else if (TxtTaskCodeDone.Text.Length > 10)
                errors += "TaskCode excede 10 caracteres.\n";

            if (string.IsNullOrWhiteSpace(TxtFunCodeDone.Text))
                errors += "FunCode es obligatorio.\n";
            else if (TxtFunCodeDone.Text.Length > 30)
                errors += "FunCode excede 30 caracteres.\n";

            if (DatePickerFechaDone.SelectedDate == null)
                errors += "Seleccione una fecha.\n";

            if (ComboTaskStatusCodeDone.SelectedValue.ToString()=="")
                errors += "TaskExecutionStatusCode es obligatorio.\n";
            
            return string.IsNullOrEmpty(errors);
        }

        private void ShowImagePreview(byte[] bytes)
        {
            try
            {
                using var ms = new MemoryStream(bytes);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                ImgPreview.Source = bmp;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al mostrar la imagen: " + ex.Message);
            }
        }

        private async Task<List<TaskExecutionSend>> CargarTareasAsync()
        {
            var lista = new List<TaskExecutionSend>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"SELECT UnitCode, TaskCode, FunCode, [Date] AS TaskDate, TaskExecutionStatusCode, Imagen
                                 FROM MNT_Task_Execution_Tablet";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var tarea = new TaskExecutionSend
                        {
                            UnitCode = reader["UnitCode"].ToString(),
                            TaskCode = reader["TaskCode"].ToString(),
                            FunCode = reader["FunCode"].ToString(),
                            TaskDate = Convert.ToDateTime(reader["TaskDate"]),
                            TaskExecutionStatusCode = reader["TaskExecutionStatusCode"].ToString(),
                            Imagen = reader["Imagen"] == DBNull.Value ? null : (byte[])reader["Imagen"]
                        };

                        lista.Add(tarea);
                    }
                }
            }

            return lista;
        }

        private void EoloDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Alternar colores según el índice de la fila
            if (e.Row.GetIndex() % 2 == 0)
            {
                e.Row.Background = new SolidColorBrush(Colors.LightYellow); // Color para filas pares
            }
            else
            {
                e.Row.Background = new SolidColorBrush(Colors.Bisque); // Color para filas impares
            }
        }

    }
}



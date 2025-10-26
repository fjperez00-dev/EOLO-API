using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace ConsultaNaval
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InicializarControles();
        }

        private void InicializarControles()
        {
            comboBases.ItemsSource = new List<BaseNaval>
            {
                new BaseNaval { code = "44000", name = "44000-Main Base RIYADH" },
                new BaseNaval { code = "44500", name = "44500-Corvette Base JEDDAH" },
                new BaseNaval { code = "44538", name = "44538-HMS ALJUBAIL" },
                new BaseNaval { code = "44539", name = "44539-HMS ALDIRIYAH" },
                new BaseNaval { code = "44540", name = "44540-HMS HAIL" },
                new BaseNaval { code = "44541", name = "44541-HMS JAZAN" },
                new BaseNaval { code = "44542", name = "44542-HMS UNAYZAH" },
                new BaseNaval { code = "44600", name = "44600-ROTA Naval Base" }
            };

            fechaFinal.SelectedDate = DateTime.Today;
            fechaInicial.SelectedDate = DateTime.Today.AddMonths(-1);

            fechaInicial.SelectedDateChanged += ValidarCampos;
            fechaFinal.SelectedDateChanged += ValidarCampos;
            comboBases.SelectionChanged += ValidarCampos;
        }

        private void ValidarCampos(object sender, EventArgs e)
        {
            btnConsultar.IsEnabled = comboBases.SelectedItem != null &&
                                     fechaInicial.SelectedDate.HasValue &&
                                     fechaFinal.SelectedDate.HasValue;
        }

        private async void btnConsultar_Click(object sender, RoutedEventArgs e)
        {
            string baseUrl = ConfigurationManager.AppSettings["ApiBaseUrl"];
            string baseCode = ((BaseNaval)comboBases.SelectedItem).code;
            string fechaIni = fechaInicial.SelectedDate.Value.ToString("ddMMyyyy");
            string fechaFin = fechaFinal.SelectedDate.Value.ToString("ddMMyyyy");

            string url = $"{baseUrl}?unit={baseCode}&FechaIni={fechaIni}&FechaFin={fechaFin}";
            

            using HttpClient client = new HttpClient();
            try
            {
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var tareas = JsonSerializer.Deserialize<List<TaskExecutionAPI>>(json);
                dataGrid.ItemsSource = tareas;
                if (tareas != null)
                    txtResultados.Text = $"Resultados ({tareas.Count}) : ";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al consultar: {ex.Message}");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TaskApiClient_WPF.Models;

namespace TaskApiClient_WPF.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly string? _baseUrl;        

        public ApiService()
        {
            // Cargar configuración
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)          
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _baseUrl = config["ApiSettings:BaseUrl"];            

            _http = new HttpClient();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));            
        }

        public async Task<List<TaskExecutionResult>> GetTasksAsync(string unit, string fechaIni, string fechaFin)
        {
            string url = $"{_baseUrl}/api/Tasks?unit={Uri.EscapeDataString(unit)}&fechaini={fechaIni}&fechafin={fechaFin}";
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var stream = await resp.Content.ReadAsStreamAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = await JsonSerializer.DeserializeAsync<List<TaskExecutionResult>>(stream, options);
            return items ?? new List<TaskExecutionResult>();
        }
    }
}

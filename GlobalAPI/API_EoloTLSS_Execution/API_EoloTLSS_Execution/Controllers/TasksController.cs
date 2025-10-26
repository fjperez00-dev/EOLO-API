using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using TaskApi_Seguro.Models;

namespace TaskApi_Seguro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TasksController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks(
            [FromQuery] string unit,
            [FromQuery] string fechaini,
            [FromQuery] string fechafin)
        {
            if (string.IsNullOrEmpty(unit) || string.IsNullOrEmpty(fechaini) || string.IsNullOrEmpty(fechafin))
                return BadRequest("Debe especificar UNIDAD, Fecha Inicial y Fecha Final.");

            if (!DateTime.TryParseExact(fechaini, "ddMMyyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaIniParsed) ||
                !DateTime.TryParseExact(fechafin, "ddMMyyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fechaFinParsed))
                return BadRequest("El formato de fecha debe ser ddMMyyyy.");

            var result = new List<TaskExecution>();

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            string query = @"
                SELECT UnitCode, TaskCode, FunCode, PlannedDate, TaskExecutionStatusCode
                FROM dbo.MNT_Task_Execution
                WHERE UnitCode = @UNIT 
                AND PlannedDate BETWEEN @FECHAINI AND @FECHAFIN 
                AND TaskExecutionStatusCode = 'P';";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@UNIT", SqlDbType.VarChar, 5).Value = unit;
                cmd.Parameters.Add("@FECHAINI", SqlDbType.DateTime).Value = fechaIniParsed;
                cmd.Parameters.Add("@FECHAFIN", SqlDbType.DateTime).Value = fechaFinParsed;

                await conn.OpenAsync();
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new TaskExecution
                        {
                            UnitCode = reader["UnitCode"].ToString(),
                            TaskCode = reader["TaskCode"].ToString(),
                            FunCode = reader["FunCode"].ToString(),
                            PlannedDate = Convert.ToDateTime(reader["PlannedDate"]),
                            TaskExecutionStatusCode = reader["TaskExecutionStatusCode"].ToString()
                        });
                    }
                }
            }

            return Ok(result);
        }
    }
}

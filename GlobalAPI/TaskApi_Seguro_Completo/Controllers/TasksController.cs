using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient; // Cambiado de System.Data.SqlClient a Microsoft.Data.SqlClient
using System.Data;
using System.Data.SqlClient;
using TaskApi_EOLO.Models;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlParameter = Microsoft.Data.SqlClient.SqlParameter;

namespace TaskApi_EOLO.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly IConfiguration _config;


        public TasksController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasks(string unit, string fechaini, string fechafin)
        {
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(_config.GetConnectionString("EOLOConnectionString"));
            await connection.OpenAsync();

            var query = @"SELECT UnitCode, TaskCode, FunCode, [Date] AS TaskExecutionDate, TaskExecutionStatusCode
                          FROM dbo.MNT_Task_Execution
                          WHERE UnitCode=@unit AND [Date] BETWEEN @fechaini AND @fechafin
                          AND TaskExecutionStatusCode='P'";

            var command = new Microsoft.Data.SqlClient.SqlCommand(query, connection);
            command.Parameters.AddWithValue("@unit", unit);
            command.Parameters.AddWithValue("@fechaini", DateTime.ParseExact(fechaini, "ddMMyyyy", null));
            command.Parameters.AddWithValue("@fechafin", DateTime.ParseExact(fechafin, "ddMMyyyy", null));

            var reader = await command.ExecuteReaderAsync();
            var results = new List<TaskExecution>();

            while (await reader.ReadAsync())
            {
                results.Add(new TaskExecution
                {
                    UnitCode = $"{reader["UnitCode"].ToString()}{""}",
                    TaskCode = $"{reader["TaskCode"].ToString()}{""}",
                    FunCode = $"{reader["FunCode"].ToString()}{""}",
                    TaskExecutionDate = Convert.ToDateTime(reader["TaskExecutionDate"]),
                    TaskExecutionStatusCode = $"{reader["TaskExecutionStatusCode"].ToString()}{""}"
                });
            }

            return Ok(results);
        }

        [HttpPost]
        [RequestSizeLimit(long.MaxValue)]
        public async Task<IActionResult> Post()
        {
            // Esperamos multipart/form-data (campos + fichero opcional).
            if (!Request.HasFormContentType) return BadRequest("Se esperaba form-data.");

            var form = await Request.ReadFormAsync();

            string? unitCode = form["UnitCode"];
            string? taskCode = form["TaskCode"];
            string? funCode = form["FunCode"];
            DateTime taskDate = Convert.ToDateTime(form["TaskExecutionDate"]) ;
            string? taskstatusCode = form["TaskExecutionStatusCode"];

            if (string.IsNullOrWhiteSpace(unitCode) ||
                string.IsNullOrWhiteSpace(taskCode) ||
                string.IsNullOrWhiteSpace(funCode) ||
                string.IsNullOrWhiteSpace(taskDate.ToString()) ||
                string.IsNullOrWhiteSpace(taskstatusCode))
            {
                return BadRequest("Faltan campos obligatorios.");
            }

            if (!DateTime.TryParse(taskDate.ToString(), out DateTime date))
                return BadRequest("Fecha inválida.");

            // Validaciones de longitudes servidor-side (seguridad adicional)
            if (unitCode.Length > 10 || taskCode.Length > 10 || funCode.Length > 30 || taskstatusCode.Length > 10)
                return BadRequest("Algún campo excede la longitud permitida.");

            byte[]? imageBytes = null;
            var file = form.Files.FirstOrDefault();
            if (file != null && file.Length > 0)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                imageBytes = ms.ToArray();
            }

            // Insert en SQL
            var connStr = _config.GetConnectionString("EOLOConnectionString");
            using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr))
            {
                await conn.OpenAsync();
                // Verificamos si ya existe un registro con los mismos campos clave
                string checkQuery = @"SELECT COUNT(*) 
                              FROM MNT_Task_Execution_Tablet
                              WHERE UnitCode = @UnitCode AND TaskCode = @TaskCode AND FunCode = @FunCode AND [Date] = @Date";

                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@UnitCode", unitCode);
                    checkCmd.Parameters.AddWithValue("@TaskCode", taskCode);
                    checkCmd.Parameters.AddWithValue("@FunCode", funCode);
                    checkCmd.Parameters.AddWithValue("@Date", taskDate);
                                       
                    int exists = (int)checkCmd.ExecuteScalar();

                    if (exists > 0)
                    {
                        // Ya existe → actualizamos el registro
                        string updateQuery = @"UPDATE MNT_Task_Execution_Tablet
                                       SET TaskExecutionStatusCode = @TaskExecutionStatus,trz_mDate = GetDate(),Imagen=@Imagen
                                       WHERE UnitCode = @UnitCode AND TaskCode = @TaskCode AND FunCode = @FunCode 
                                            AND [Date] = @Date";

                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@TaskExecutionStatus", taskstatusCode ?? (object)DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@UnitCode", unitCode);
                            updateCmd.Parameters.AddWithValue("@TaskCode", taskCode);
                            updateCmd.Parameters.AddWithValue("@FunCode", funCode);
                            updateCmd.Parameters.AddWithValue("@Date", taskDate);
                            var UpdateimageParam = new SqlParameter("@Imagen", SqlDbType.VarBinary, -1);
                            UpdateimageParam.Value = (object?)imageBytes ?? DBNull.Value;
                            updateCmd.Parameters.Add(UpdateimageParam);

                            int rows = updateCmd.ExecuteNonQuery();
                            return Ok($"Registro actualizado ({rows} fila/s).");
                        }
                    }
                    else
                    {
                        // No existe → insertamos nuevo registro
                        string insertQuery = @"INSERT INTO MNT_Task_Execution_Tablet
                                       (UnitCode, TaskCode, FunCode, [Date], TaskExecutionStatusCode,Imagen,trz_cDate)
                                       VALUES (@UnitCode, @TaskCode, @FunCode, @Date, @TaskExecutionStatus,@Imagen,Getdate())";

                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@UnitCode", unitCode);
                            insertCmd.Parameters.AddWithValue("@TaskCode", taskCode);
                            insertCmd.Parameters.AddWithValue("@FunCode", funCode);
                            insertCmd.Parameters.AddWithValue("@Date", taskDate);
                            insertCmd.Parameters.AddWithValue("@TaskExecutionStatus", taskstatusCode ?? (object)DBNull.Value);
                            var InsertimageParam = new SqlParameter("@Imagen", SqlDbType.VarBinary, -1);
                            InsertimageParam.Value = (object?)imageBytes ?? DBNull.Value;
                            insertCmd.Parameters.Add(InsertimageParam);

                            int rows = insertCmd.ExecuteNonQuery();
                            return Ok($"Registro insertado ({rows} fila/s).");
                        }
                    }
                }
            }
        }
    }
}

using System;

namespace TaskApiClient_WPF.Models
{
    public class TaskExecutionDone
    {
        public string UnitCode { get; set; }
        public string TaskCode { get; set; } 
        public string FunCode { get; set; } 
        public DateTime TaskExecutionDate { get; set; }
        public string TaskExecutionStatusCode { get; set; } 
        public byte[]? Imagen { get; set; }
    }
}

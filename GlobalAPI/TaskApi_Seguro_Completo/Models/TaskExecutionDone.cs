namespace TaskApi_EOLO.Models
{
    public class TaskExecutionDone
    {
        public string UnitCode { get; set; } = string.Empty; // varchar(10)
        public string TaskCode { get; set; } = string.Empty; // varchar(10)
        public string FunCode { get; set; } = string.Empty; // varchar(30)
        public DateTime TaskExecutionDate { get; set; }
        public string TaskExecutionStatusCode { get; set; } = string.Empty; // varchar(10)
        public byte[]? Imagen { get; set; } // optional
    }


}

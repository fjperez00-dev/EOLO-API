namespace TaskApi_Seguro.Models
{
    public class TaskExecution
    {
        public required string UnitCode { get; set; }
        public required string TaskCode { get; set; }
        public required string FunCode { get; set; }
        public required DateTime? PlannedDate { get; set; }
        public string? TaskExecutionStatusCode { get; set; }
    }
}

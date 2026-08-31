namespace LarvaX.Core.Entities
{
    public class SmsCommand
    {
        public int Id { get; set; }
        public string SenderNumber { get; set; } = string.Empty;
        public string CommandText { get; set; } = string.Empty;
        public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
        public bool Processed { get; set; } = false;
        public string ResponseMessage { get; set; } = string.Empty;
    }
}

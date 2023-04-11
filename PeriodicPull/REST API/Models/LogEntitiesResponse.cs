namespace RestApi.Models
{
    public class LogEntitiesResponse
    {
        public List<LogEntity> LogEntities { get; set; } = new List<LogEntity>();

        public string? ContinuationToken { get; set; }
    }
}

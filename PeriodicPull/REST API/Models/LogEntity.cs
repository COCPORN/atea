using System.Net;

namespace RestApi.Models
{
    public class LogEntity
    {
        public DateTimeOffset? Date { get; set; }

        /// <summary>
        /// If the call was not successful over HTTP, this will be Guid.Empty
        /// </summary>
        public Guid BlobId { get; set; }

        public HttpStatusCode HttpStatusCode { get; set; }

        public bool IsSuccessStatusCode { get; set; }

    }
}

using Cosmos.Common.Models;

namespace Cosmos.Cms.Api.Models
{
    public class MessageViewModel : ContactViewModel
    {
        public string Subject { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string JoinMailingList { get; set; } = "false";
    }
}

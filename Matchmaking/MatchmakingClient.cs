using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OwlTree.Matchmaking
{
    /// <summary>
    /// Use to send matchmaking requests to your matchmaking endpoint.
    /// </summary>
    public class MatchmakingClient
    {
        /// <summary>
        /// The URL this client will send requests to.
        /// </summary>
        public string EndpointUrl { get; private set; }

        private HttpClient _client;

        /// <summary>
        /// Use to send matchmaking requests to your matchmaking endpoint.
        /// </summary>
        public MatchmakingClient(string endpointUrl)
        {
            EndpointUrl = endpointUrl;
            _client = new HttpClient();
        }

        /// <summary>
        /// Create a hosted relay session.
        /// </summary>
        public async Task<SessionCreationResponse> CreateSession(SessionCreationRequest request)
        {
            return await SendRequest<SessionCreationRequest, SessionCreationResponse>(request, Uris.CreateSession);
        }

        /// <summary>
        /// Publish a peer-to-peer session.
        /// </summary>
        public async Task<SessionPublishResponse> PublishSession(SessionPublishRequest request)
        {
            return await SendRequest<SessionPublishRequest, SessionPublishResponse>(request, Uris.PublishSession);
        }

        /// <summary>
        /// Get the connection args for a session.
        /// </summary>
        public async Task<SessionDataResponse> GetSession(SessionDataRequest request)
        {
            return await SendRequest<SessionDataRequest, SessionDataResponse>(request, Uris.SessionData);
        }

        /// <summary>
        /// Join the matchmaking queue.
        /// </summary>
        public async Task<MatchmakingTicketResponse> GetTicket(MatchmakingTicketRequest request)
        {
            return await SendRequest<MatchmakingTicketRequest, MatchmakingTicketResponse>(request, Uris.GetTicket);
        }

        /// <summary>
        /// Get the status of an existing matchmaking ticket.
        /// </summary>
        public async Task<TicketStatusResponse> GetTicketStatus(TicketStatusRequest request)
        {
            return await SendRequest<TicketStatusRequest, TicketStatusResponse>(request, Uris.TicketStatus);
        }

        private async Task<B> SendRequest<A, B>(A request, string uri) where A : HttpRequest<A> where B : HttpResponse<B>, new()
        {
            try
            {
                var requestStr = request.Serialize();
                var content = new StringContent(requestStr, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync(EndpointUrl + uri, content);

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    return HttpResponse<B>.Deserialize(responseContent);
                }
                else
                {
                    switch ((int)response.StatusCode)
                    {
                        case (int)ResponseCodes.RequestRejected: return HttpResponse<B>.GetRequestRejected<B>();
                        case (int)ResponseCodes.NotFound:
                        default:
                            return HttpResponse<B>.GetNotFound<B>();
                    }
                }
            }
            catch
            {
                return HttpResponse<B>.GetExceptionThrow<B>();
            }
        }
    }
}
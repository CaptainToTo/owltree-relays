using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OwlTree.Matchmaking
{
    /// <summary>
    /// Matchmaking HTTP response codes.
    /// </summary>
    public enum ResponseCodes
    {
        /// <summary>
        /// An invalid response code, this should not be returned.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// The matchmaking request was accepted.
        /// </summary>
        RequestAccepted = 200,
        TicketInQueue = 201,
        TicketExpired = 202,

        /// <summary>
        /// The endpoint or URI was not found.
        /// </summary>
        NotFound = 404,
        /// <summary>
        /// The request failed to send.
        /// </summary>
        ExceptionThrow = 410,
        /// <summary>
        /// The endpoint rejected the matchmaking request.
        /// </summary>
        RequestRejected = 411
    }

    public abstract class HttpRequest<T>
    {
        public Dictionary<string, string> args { get; set; }

        /// <summary>
        /// Serializes the request to a JSON string.
        /// </summary>
        public string Serialize() => JsonSerializer.Serialize(this);

        /// <summary>
        /// Deserializes a request from a JSON string.
        /// </summary>
        public static T Deserialize(string data) => JsonSerializer.Deserialize<T>(data);

        /// <summary>
        /// Deserializes a response from a JSON string.
        /// </summary>
        public static K Deserialize<K>(string data) => JsonSerializer.Deserialize<K>(data);
    }

    public abstract class HttpResponse<T> where T : HttpResponse<T>, new()
    {
        public ResponseCodes responseCode { get; set; }

        /// <summary>
        /// Returns true if the response has a successful response code.
        /// </summary>
        [JsonIgnore]
        public bool RequestSuccessful => 200 <= (int)responseCode && (int)responseCode <= 299;
        /// <summary>
        /// Returns true if the response has a failure response code.
        /// </summary>
        [JsonIgnore]
        public bool RequestFailed => 400 <= (int)responseCode && (int)responseCode <= 499;

        /// <summary>
        /// Serializes the response to a JSON string.
        /// </summary>
        public string Serialize() => JsonSerializer.Serialize(this);

        /// <summary>
        /// Deserializes a response from a JSON string.
        /// </summary>
        public static T Deserialize(string data) => JsonSerializer.Deserialize<T>(data);


        public static T NotFound = new T { responseCode = ResponseCodes.NotFound };
        public static T ExceptionThrow = new T { responseCode = ResponseCodes.ExceptionThrow };
        public static T RequestRejected = new T { responseCode = ResponseCodes.RequestRejected };

        /// <summary>
        /// Deserializes a response from a JSON string.
        /// </summary>
        public static K Deserialize<K>(string data) => JsonSerializer.Deserialize<K>(data);

        public static K GetNotFound<K>() where K : HttpResponse<K>, new() =>
            new K { responseCode = ResponseCodes.NotFound };
        public static K GetExceptionThrow<K>() where K : HttpResponse<K>, new() =>
            new K { responseCode = ResponseCodes.ExceptionThrow };
        public static K GetRequestRejected<K>() where K : HttpResponse<K>, new() =>
            new K { responseCode = ResponseCodes.RequestRejected };
    }
}
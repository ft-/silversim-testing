using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilverSim.Main.Common.HttpServer
{
    public enum HttpStatusCode : uint
    {
        Continue = 100,
        SwitchProtocol = 101,
        Processing = 102,

        OK = 200,
        Created = 201,
        Accepted = 202,
        NonAuthoritative = 203,
        NoContent = 204,
        ResetContent = 205,
        PartialContent = 206,
        MultiStatus = 207,
        AlreadyReported = 208,
        IMUsed = 226,

        MultipleChoices = 300,
        MovedPermanently = 301,
        Found = 302,
        SeeOther = 303,
        NotModified = 304,
        UseProxy = 305,
        TemporaryRedirect = 307,
        PermanentRedirect = 308,

        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        NotAcceptable = 406,
        ProxyAuthenticationRequired = 307,
        RequestTimeout = 408,
        Conflict = 409,
        Gone = 410,
        LengthRequired = 411,
        PreconditionFaled = 412,
        RequestEntityTooLarge = 413,
        RequestURLTooLong = 414,
        UnsupportedMediaType = 415,
        RequestedRangeNotSatisfiable = 416,
        ExpectationFailed = 417,
        PolicyNotFulfilled = 420,
        UnprocessableEntity = 422,
        Locked = 423,
        FailedDependency = 424,
        UnorderedCollection = 425,
        UpgradeRequired = 426,
        PreconditionRequired = 428,
        TooManyRequest = 429,
        RequestHeaderFieldsTooLarge = 431,

        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeout = 504,
        HTTPVersionNotSupported = 505,
        VariantAlsoNegotiates = 506,
        InsufficientStorage = 507,
        LoopDetected = 508,
        BandwidthLimitExceeded = 509,
        NotExtended = 510
    }
}

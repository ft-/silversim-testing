// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common.HttpServer;
using SilverSim.Threading;
using SilverSim.Types;
using System;

namespace SilverSim.Main.Common
{
    public enum AdminWebIfErrorResult
    {
        NotLoggedIn = 1,
        NotFound = 2,
        InsufficientRights = 3,
        InvalidRequest = 4,
        AlreadyExists = 5,
        NotPossible = 6,
        InUse = 7,
        MissingSessionId = 8,
        MissingMethod = 9,
        InvalidSession = 10,
        InvalidUserAndOrPassword = 11,
        UnknownMethod = 12,
        AlreadyStarted = 13,
        FailedToStart = 14,
        NotRunning = 15,
        IsRunning = 16,
        InvalidParameter = 17,
        NoEstates = 18
    };

    public interface IAdminWebIF
    {
        RwLockedDictionaryAutoAdd<string, RwLockedList<string>> AutoGrantRights
        {
            get;
        }

        RwLockedDictionary<string, Action<HttpRequest, Map>> JsonMethods
        {
            get;
        }

        void SuccessResponse(HttpRequest req, Map m);
        void ErrorResponse(HttpRequest req, AdminWebIfErrorResult reason);

        UUI ResolveName(UUI uui);
        bool TranslateToUUI(string arg, out UUI uui);

        UUID GetSelectedRegion(HttpRequest req, Map jsonreq);
        void SetSelectedRegion(HttpRequest req, Map jsonreq, UUID sceneID);
    }
}

// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

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
    }

    public interface IAdminWebIF
    {
        RwLockedDictionaryAutoAdd<string, RwLockedList<string>> AutoGrantRights { get; }

        RwLockedDictionary<string, Action<HttpRequest, Map>> JsonMethods { get; }

        RwLockedList<string> ModuleNames { get; }

        void SuccessResponse(HttpRequest req, Map m);
        void ErrorResponse(HttpRequest req, AdminWebIfErrorResult reason);

        UUI ResolveName(UUI uui);
        bool TranslateToUUI(string arg, out UUI uui);

        UUID GetSelectedRegion(HttpRequest req, Map jsonreq);
        void SetSelectedRegion(HttpRequest req, Map jsonreq, UUID sceneID);
    }
}

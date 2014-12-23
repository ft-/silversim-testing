/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SilverSim.Types;
using SilverSim.Types.Economy;

namespace SilverSim.ServiceInterfaces.Economy
{
    public abstract class EconomyServiceInterface
    {
        public enum TransactionType
        {
            None = 0,
            FailSimulatorTimeout = 1,
            FailDataserverTimeout = 2,
            ObjectClaim = 1000,
            LandClaim = 1001,
            GroupCreate = 1002,
            ObjectPublicClaim = 1003,
            GroupJoin = 1004,
            TeleportCharge = 1100,
            UploadCharge = 1101,
            LandAuction = 1102,
            ClassifiedCharge = 1103,
            ObjectTax = 2000,
            LandTax = 2001,
            LightTax = 2002,
            ParcelDirFee = 2003,
            GroupTax = 2004,
            ClassifiedRenew = 2005,
            GiveInventory = 3000,
            ObjectSale = 5000,
            Gift = 5001,
            LandSale = 5002,
            ReferBonus = 5003,
            InventorySale = 5004,
            RefundPurchase = 5005,
            LandPassSale = 5006,
            DwellBonus = 5007,
            PayObject = 5008,
            ObjectPays = 5009,
            GroupLandDeed = 6001,
            GroupObjectDeed = 6002,
            GroupLiability = 6003,
            GroupDividend = 6004,
            GroupMembershipDues = 6005,
            ObjectRelease = 8000,
            LandRelease = 8001,
            ObjectDelete = 8002,
            ObjectPublicDecay = 8003,
            ObjectPublicDelete = 8004,
            LindenAdjustment = 9000,
            LindenGrant = 9001,
            LindenPenalty = 9002,
            EventFee = 9003,
            EventPrize = 9004,
            StipendBasic = 10000,
            StipendDeveloper = 10001,
            StipendAlways = 10002,
            StipendDaily = 10003,
            StipendRating = 10004,
            StipendDelta = 10005,
        }

        public interface MoneyBalanceAccessor
        {
            Int32 this[UUI agentID] { get; set; }
        }

        public EconomyServiceInterface()
        {

        }

        public abstract void Login(UUI agentID, UUID sessionID, UUID secureSessionID);

        public abstract void Logout(UUI agentID, UUID sessionID, UUID secureSessionID);

        public abstract MoneyBalanceAccessor MoneyBalance { get; }

        public abstract void ChargeAmount(UUI agentID, TransactionType type, int amount, Action processOperation); /* exception from action results into abort */

        public abstract void IncreaseAmount(UUI agentID, TransactionType type, int amount);
    }
}

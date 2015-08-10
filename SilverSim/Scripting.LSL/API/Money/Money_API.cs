// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Main.Common;
using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.ServiceInterfaces.Economy;
using SilverSim.Types;
using SilverSim.Types.Script;
using System;
using System.Runtime.Remoting.Messaging;

namespace SilverSim.Scripting.LSL.API.Money
{
    [ScriptApiName("Money")]
    [LSLImplementation]
    public partial class Money_API : MarshalByRefObject, IScriptApi, IPlugin
    {
        public Money_API()
        {

        }

        public void Startup(ConfigurationLoader loader)
        {

        }

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void transaction_result(LSLKey id, int success, string data);

        delegate void TransferMoneyDelegate(UUID transactionID, UUI sourceid, 
            UUI destinationid, int amount, ScriptInstance instance);

        public void TransferMoney(UUID transactionID, UUI sourceid,
            UUI destinationid, int amount, ScriptInstance instance)
        {
            EconomyServiceInterface sourceservice = null;
            EconomyServiceInterface destinationservice = null;
            TransactionResultEvent ev = new TransactionResultEvent();
            ev.Success = false;
            ev.TransactionID = transactionID;

            if(sourceservice == null ||
                destinationservice == null ||
                destinationid == UUI.Unknown)
            {
                if (instance != null)
                {
                    instance.PostEvent(ev);
                }
            }
            else
            {
                try
                {
                    sourceservice.ChargeAmount(sourceid, EconomyServiceInterface.TransactionType.ObjectPays, amount,
                        delegate()
                        {
                            destinationservice.IncreaseAmount(destinationid, EconomyServiceInterface.TransactionType.ObjectPays, amount);
                        });
                    ev.Success = true;
                }
                catch
                {

                }
                if (instance != null)
                {
                    instance.PostEvent(ev);
                }
            }
        }

        void TransferMoneyEnd(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            TransferMoneyDelegate caller = (TransferMoneyDelegate)result.AsyncDelegate;
            caller.EndInvoke(ar);
        }

        void InvokeTransferMoney(UUID transactionID, UUI sourceid,
            UUI destinationid, int amount, ScriptInstance instance)
        {
            TransferMoneyDelegate d = TransferMoney;
            d.BeginInvoke(transactionID, sourceid, destinationid, amount, instance, TransferMoneyEnd, this);
        }

        [APILevel(APIFlags.LSL)]
        public void llGiveMoney(ScriptInstance instance, LSLKey destination, int amount)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if ((grantinfo.PermsMask & ScriptPermissions.Debit) == 0 ||
                grantinfo.PermsGranter != instance.Part.Owner ||
                amount < 0)
            {
                return;
            }
        }

        [APILevel(APIFlags.LSL)]
        public LSLKey llTransferLindenDollars(ScriptInstance instance, LSLKey destination, int amount)
        {
            ObjectPartInventoryItem.PermsGranterInfo grantinfo = instance.Item.PermsGranter;
            if ((grantinfo.PermsMask & ScriptPermissions.Debit) == 0 ||
                grantinfo.PermsGranter != instance.Part.Owner ||
                amount < 0)
            {
                return UUID.Zero;
            }
            return UUID.Zero;
        }
    }
}

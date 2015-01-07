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

using SilverSim.Main.Common;
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
        public const int PERMISSION_DEBIT = 0x2;

        [APILevel(APIFlags.LSL)]
        [StateEventDelegate]
        public delegate void transaction_result(UUID id, int success, string data);

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
        public void llGiveMoney(ScriptInstance instance, UUID destination, int amount)
        {
            Script script = (Script)instance;
            if((script.m_ScriptPermissions & ScriptPermissions.Debit) == 0 ||
                script.m_ScriptPermissionsKey != script.Part.Owner.ID ||
                amount < 0)
            {
                return;
            }
        }

        [APILevel(APIFlags.LSL)]
        public UUID llTransferLindenDollars(ScriptInstance instance, UUID destination, int amount)
        {
            Script script = (Script)instance;
            if ((script.m_ScriptPermissions & ScriptPermissions.Debit) == 0 ||
                script.m_ScriptPermissionsKey != script.Part.Owner.ID ||
                amount < 0)
            {
                return UUID.Zero;
            }
            return UUID.Zero;
        }
    }
}

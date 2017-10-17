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

using SilverSim.Types;
using SilverSim.Types.Economy.Transactions;
using System;

namespace SilverSim.ServiceInterfaces.Economy
{
    public abstract class EconomyServiceInterface
    {
        public interface IActiveTransaction
        {
            void Commit();
            void Rollback(Exception exception);
        }

        public interface IMoneyBalanceAccessor
        {
            Int32 this[UUI agentID] { get; set; }

            bool TryGetValue(UUI agentID, out Int32 balance);
        }

        public abstract void Login(UUI agentID, UUID sessionID, UUID secureSessionID);

        public abstract void Logout(UUI agentID, UUID sessionID, UUID secureSessionID);

        public abstract IMoneyBalanceAccessor MoneyBalance { get; }

        public abstract IActiveTransaction BeginTransaction(UUI agentID, ITransaction transactionData, int amount);
        public abstract IActiveTransaction BeginTransaction(UUI sourceID, UUI destinationID, ITransaction transactionData, int amount);

        public void ChargeAmount(UUI agentID, ITransaction transactionData, int amount, Action processOperation)
        {
            IActiveTransaction transaction = BeginTransaction(agentID, transactionData, amount);
            try
            {
                processOperation();
            }
            catch(Exception e)
            {
                transaction.Rollback(e);
                throw;
            }
            transaction.Commit();
        }

        public void TransferMoney(UUI sourceID, UUI destinationID, ITransaction transactionData, int amount, Action processOperation)
        {
            IActiveTransaction transaction = BeginTransaction(sourceID, destinationID, transactionData, amount);
            try
            {
                processOperation();
            }
            catch(Exception e)
            {
                transaction.Rollback(e);
                throw;
            }
            transaction.Commit();
        }
    }
}

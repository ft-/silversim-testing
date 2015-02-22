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

using SilverSim.Types;
using System;
using System.Threading;
using ThreadedClasses;

namespace SilverSim.Main.Common.Transfer
{
    public class AssetTransferer
    {
        private BlockingQueue<AssetTransferWorkItem> m_WorkQueue = new BlockingQueue<AssetTransferWorkItem>();
        private bool m_AbortRequestActive = false;

        private class AbortTransfererThread : AssetTransferWorkItem
        {
            public AbortTransfererThread()
                : base(null, null, UUID.Zero, ReferenceSource.Destination)
            {

            }

            public override void AssetTransferComplete()
            {

            }

            public override void AssetTransferFailed(Exception e)
            {

            }
        }

        public class AssetTransfererStoppedException : Exception
        {
            public AssetTransfererStoppedException()
            {

            }
        }

        public AssetTransferer()
        {
            new Thread(ThreadMain).Start();
        }

        public void Enqueue(AssetTransferWorkItem wi)
        {
            if(m_AbortRequestActive)
            {
                if (!(wi is AbortTransfererThread))
                {
                    wi.AssetTransferFailed(new AssetTransfererStoppedException());
                }
                return;
            }
            if (wi is AbortTransfererThread)
            {
                m_AbortRequestActive = true;
            }
            m_WorkQueue.Enqueue(wi);
        }

        public void Stop()
        {
            Enqueue(new AbortTransfererThread());
        }

        private void ThreadMain()
        {
            for(;;)
            {
                AssetTransferWorkItem wi = m_WorkQueue.Dequeue();
                if(wi is AbortTransfererThread)
                {
                    break;
                }
                wi.ProcessAssetTransfer();
            }

            for(;;)
            {
                AssetTransferWorkItem wi = m_WorkQueue.Dequeue(1);
                if (!(wi is AbortTransfererThread))
                {
                    wi.AssetTransferFailed(new AssetTransfererStoppedException());
                }
            }
        }
    }
}

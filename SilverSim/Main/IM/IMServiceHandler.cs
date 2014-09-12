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
using SilverSim.Main.Common.HttpServer;
using SilverSim.Scene.Management.IM;
using SilverSim.ServiceInterfaces.IM;
using SilverSim.Types.IM;
using SilverSim.Types;
using log4net;
using Nini.Config;
using System.Collections;
using System.Reflection;
using System.Threading;
using ThreadedClasses;
using Nwc.XmlRpc;
using System;

namespace SilverSim.Main.IM
{
    #region Service Implementation
    public class IMServiceHandler : IMServiceInterface, IPlugin, IPluginShutdown
    {
        protected internal BlockingQueue<GridInstantMessage> m_Queue = new BlockingQueue<GridInstantMessage>();
        protected internal RwLockedList<Thread> m_Threads = new RwLockedList<Thread>();
        private uint m_MaxThreads;
        private static readonly ILog m_Log = LogManager.GetLogger("IM SERVICE");

        void IMSendThread(object s)
        {
            IMServiceHandler service = (IMServiceHandler)s;
            Thread thread = Thread.CurrentThread;
            thread.Name = "IM:Send Thread";
            service.m_Threads.Add(thread);
            try
            {
                while(true)
                {
                    GridInstantMessage im = m_Queue.Dequeue(1000);
                    try
                    {
                        IMRouter.SendAsync(im);
                    }
                    catch
                    {
                        im.OnResult(im, false);
                    }
                }
            }
            catch
            {
                service.m_Threads.Remove(thread);
            }
        }
        
        #region Constructor
        public IMServiceHandler(uint maxThreads)
        {
            m_MaxThreads = maxThreads;
        }

        private HttpXmlRpcHandler m_XmlRpcServer;

        public void Startup(ConfigurationLoader loader)
        {
            m_XmlRpcServer = loader.GetService<HttpXmlRpcHandler>("XmlRpcServer");
            m_XmlRpcServer.XmlRpcMethods.Add("grid_instant_message", XmlRpc_IM_Handler);
        }

        public void Shutdown()
        {
            m_XmlRpcServer.XmlRpcMethods.Remove("grid_instant_message");
        }

        public ShutdownOrder ShutdownOrder
        {
            get
            {
                return ShutdownOrder.Any;
            }
        }
        #endregion

        #region IM Service
        public override void Send(GridInstantMessage im)
        {
            m_Queue.Enqueue(im);
            lock(this)
            {
                if(m_Queue.Count > m_Threads.Count && m_Threads.Count < m_MaxThreads)
                {
                    new Thread(IMSendThread).Start(this);
                }
            }
        }

        public XmlRpcResponse XmlRpc_IM_Handler(XmlRpcRequest req)
        {
            GridInstantMessage gim = new GridInstantMessage();
            Hashtable requestData = (Hashtable)req.Params[0];
            bool successful = false;

            try
            {
                UUID.TryParse((string)requestData["from_agent_id"], out gim.FromAgent.ID);
                UUID.TryParse((string)requestData["to_agent_id"], out gim.ToAgent.ID);
                UUID.TryParse((string)requestData["im_session_id"], out gim.IMSessionID);
                UUID.TryParse((string)requestData["region_id"], out gim.RegionID);

                try
                {
                    gim.Timestamp = Date.UnixTimeToDateTime(uint.Parse((string)requestData["timestamp"]));
                }
                catch (ArgumentException) { }
                catch (FormatException) { }
                catch (OverflowException) { }

                gim.FromAgent.FullName = (string)requestData["from_agent_name"];
                gim.Message = (string)requestData["message"];
                if(gim.Message == null)
                {
                    gim.Message = string.Empty;
                }

                string dialog = (string)requestData["dialog"];
                if(string.IsNullOrEmpty(dialog))
                {
                    gim.Dialog = GridInstantMessageDialog.MessageFromAgent;
                }
                else
                {
                    byte[] dialogdata = Convert.FromBase64String(dialog);
                    gim.Dialog = (GridInstantMessageDialog)dialogdata[0];
                }

                if((string)requestData["from_group"] == "TRUE")
                {
                    gim.IsFromGroup = true;
                }

                string offline = (string)requestData["offline"];
                if(string.IsNullOrEmpty(offline))
                {
                    gim.IsOffline = false;
                }
                else
                {
                    byte[] offlinedata = Convert.FromBase64String(offline);
                    gim.IsOffline = offlinedata[0] != 0;
                }

                try
                {
                    gim.ParentEstateID = uint.Parse((string)requestData["parent_estate_id"]);
                }
                catch (ArgumentException) { }
                catch (FormatException) { }
                catch (OverflowException) { }

                try
                {
                    gim.Position.X = float.Parse((string)requestData["position_x"]);
                }
                catch (ArgumentException) { }
                catch (FormatException) { }
                catch (OverflowException) { }

                try
                {
                    gim.Position.Y = float.Parse((string)requestData["position_y"]);
                }
                catch (ArgumentException) { }
                catch (FormatException) { }
                catch (OverflowException) { }

                try
                {
                    gim.Position.Z = float.Parse((string)requestData["position_z"]);
                }
                catch (ArgumentException) { }
                catch (FormatException) { }
                catch (OverflowException) { }

                string binbucket = (string)requestData["binary_bucket"];
                if(string.IsNullOrEmpty(binbucket))
                {
                    gim.BinaryBucket = new byte[0];
                }
                else
                {
                    gim.BinaryBucket = Convert.FromBase64String(binbucket);
                }

                successful = IMRouter.SendSync(gim);
            }
            catch(Exception e)
            {
                m_Log.Debug("Unexpected exception caught", e);
                successful = false;
            }
            Hashtable respdata = new Hashtable();
            if(successful)
            {
                respdata["success"] = "TRUE";
            }
            else
            {
                respdata["success"] = "FALSE";
            }
            XmlRpcResponse resp = new XmlRpcResponse();
            resp.Value = respdata;
            return resp;
        }
        #endregion
    }
    #endregion

    #region Factory
    public class IMServiceHandlerFactory : IPluginFactory
    {
        private static readonly ILog m_Log = LogManager.GetLogger("IM SERVICE");
        public IMServiceHandlerFactory()
        {

        }

        public IPlugin Initialize(ConfigurationLoader loader, IConfig ownSection)
        {
            return new IMServiceHandler((uint)ownSection.GetInt("MaxThreads"));
        }
    }
    #endregion
}

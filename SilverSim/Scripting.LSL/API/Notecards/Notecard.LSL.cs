// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Scene;
using SilverSim.Scene.Types.Script;
using SilverSim.Scene.Types.Script.Events;
using SilverSim.Types;
using SilverSim.Types.Asset.Format;
using SilverSim.Types.Inventory;
using System;
using System.Runtime.Remoting.Messaging;

namespace SilverSim.Scripting.LSL.API.Notecards
{
    public partial class Notecard_API
    {
        public const string EOF = "\n\n\n";

        #region llGetNotecardLine
        delegate void getNotecardLineDelegate(ObjectPart part, UUID queryID, UUID assetID, int line);

        void getNotecardLine(ObjectPart part, UUID queryID, UUID assetID, int line)
        {
            Notecard nc = part.ObjectGroup.Scene.GetService<NotecardCache>()[assetID];
            string[] lines = nc.Text.Split('\n');
            DataserverEvent e = new DataserverEvent();
            if (line >= lines.Length || line < 0)
            {
                e.Data = EOF;
                e.QueryID = queryID;
                part.PostEvent(e);
            }

            e.Data = lines[line];
            e.QueryID = queryID;
            part.PostEvent(e);
        }

        void getNotecardLineEnd(IAsyncResult ar)
        {
            AsyncResult r = (AsyncResult)ar;
            getNotecardLineDelegate caller = (getNotecardLineDelegate)r.AsyncDelegate;
            caller.EndInvoke(ar);
        }

        void getNotecardLineAsync(ScriptInstance Instance, UUID queryID, UUID assetID, int line)
        {
            getNotecardLineDelegate del = getNotecardLine;
            del.BeginInvoke(Instance.Part, queryID, assetID, line, getNotecardLineEnd, this);
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(0.1)]
        public LSLKey llGetNotecardLine(ScriptInstance Instance, string name, int line)
        {
            lock (Instance)
            {
                ObjectPartInventoryItem item;
                if (Instance.Part.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        UUID query = UUID.Random;
                        getNotecardLineAsync(Instance, query, item.AssetID, line);
                        return query;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion

        #region llGetNumberOfNotecardLines
        delegate void getNumberOfNotecardLinesDelegate(ObjectPart part, UUID queryID, UUID assetID);

        void getNumberOfNotecardLines(ObjectPart part, UUID queryID, UUID assetID)
        {
            Notecard nc = part.ObjectGroup.Scene.GetService<NotecardCache>()[assetID];
            DataserverEvent e = new DataserverEvent();
            int n = 1;
            foreach (char c in nc.Text)
            {
                if (c == '\n')
                {
                    ++n;
                }
            }
            e.Data = n.ToString();
            e.QueryID = queryID;
            part.PostEvent(e);
        }

        void getNumberOfNotecardLinesEnd(IAsyncResult ar)
        {
            AsyncResult r = (AsyncResult)ar;
            getNotecardLineDelegate caller = (getNotecardLineDelegate)r.AsyncDelegate;
            caller.EndInvoke(ar);
        }

        void getNumberOfNotecardLinesAsync(ScriptInstance Instance, UUID queryID, UUID assetID)
        {
            getNumberOfNotecardLinesDelegate del = getNumberOfNotecardLines;
            del.BeginInvoke(Instance.Part, queryID, assetID, getNumberOfNotecardLinesEnd, this);
        }

        [APILevel(APIFlags.LSL)]
        [ForcedSleep(0.1)]
        public LSLKey llGetNumberOfNotecardLines(ScriptInstance Instance, string name)
        {
            ObjectPartInventoryItem item;
            lock (Instance)
            {
                if (Instance.Part.Inventory.TryGetValue(name, out item))
                {
                    if (item.InventoryType != InventoryType.Notecard)
                    {
                        throw new Exception(string.Format("Inventory item {0} is not a notecard", name));
                    }
                    else
                    {
                        UUID query = UUID.Random;
                        getNumberOfNotecardLinesAsync(Instance, query, item.AssetID);
                        return query;
                    }
                }
                else
                {
                    throw new Exception(string.Format("Inventory item {0} does not exist", name));
                }
            }
        }
        #endregion
    }
}

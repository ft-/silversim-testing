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

using log4net;
using SilverSim.Main.Common;
using SilverSim.Tests.Extensions;
using SilverSim.Types;
using SilverSim.Types.Asset;
using SilverSim.Types.Asset.Format;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SilverSim.Tests.Assets.Formats
{
    public class GestureFormat : ITest
    {
        private static readonly ILog m_Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public bool Run()
        {
            AssetData assetdata;
            Gesture gestureserialized;
            Gesture gesture;

            UUI theCreator = new UUI();
            theCreator.ID = UUID.Random;
            theCreator.HomeURI = new Uri("http://example.com/");
            theCreator.FirstName = "The";
            theCreator.LastName = "Creator";

            UUID animID = UUID.Random;
            UUID soundID = UUID.Random;

            gesture = new Gesture();
            gesture.Sequence.Add(new Gesture.StepAnimation("Animation", animID, true));
            gesture.Sequence.Add(new Gesture.StepChat("Hello"));
            gesture.Sequence.Add(new Gesture.StepSound("Sound", soundID));
            gesture.Sequence.Add(new Gesture.StepWait(1, true, true));
            gesture.Sequence.Add(new Gesture.StepEndOfGesture());

            assetdata = gesture.Asset();
            gestureserialized = new Gesture(assetdata);

            if(gesture.Sequence.Count != gestureserialized.Sequence.Count)
            {
                m_Log.FatalFormat("Gesture sequence count not identical ({0} != {1})", gesture.Sequence.Count, gestureserialized.Sequence.Count);
                return false;
            }

            for (int i = 0; i < gesture.Sequence.Count; ++i)
            {
                if(gesture.Sequence[i].GetType() != gestureserialized.Sequence[i].GetType())
                {
                    m_Log.Fatal("Gesture sequence item not identical");
                    return false;
                }

                if(gesture.Sequence[i] is Gesture.StepAnimation)
                {
                    Gesture.StepAnimation anim1 = (Gesture.StepAnimation)gesture.Sequence[i];
                    Gesture.StepAnimation anim2 = (Gesture.StepAnimation)gestureserialized.Sequence[i];
                    if(anim1.AnimationStart != anim2.AnimationStart)
                    {
                        m_Log.Fatal("Gesture sequence item Animation not identical");
                        return false;
                    }
                    if (anim1.AssetID != anim2.AssetID)
                    {
                        m_Log.Fatal("Gesture sequence item Animation not identical");
                        return false;
                    }
                    if (anim1.Name != anim2.Name)
                    {
                        m_Log.Fatal("Gesture sequence item Animation not identical");
                        return false;
                    }
                }
                if (gesture.Sequence[i] is Gesture.StepChat)
                {
                    Gesture.StepChat chat1 = (Gesture.StepChat)gesture.Sequence[i];
                    Gesture.StepChat chat2 = (Gesture.StepChat)gestureserialized.Sequence[i];

                    if(chat1.Text != chat2.Text)
                    {
                        m_Log.Fatal("Gesture sequence item Chat not identical");
                        return false;
                    }
                }
                if (gesture.Sequence[i] is Gesture.StepSound)
                {
                    Gesture.StepSound sound1 = (Gesture.StepSound)gesture.Sequence[i];
                    Gesture.StepSound sound2 = (Gesture.StepSound)gestureserialized.Sequence[i];

                    if (sound1.AssetID != sound2.AssetID)
                    {
                        m_Log.Fatal("Gesture sequence item Sound not identical");
                        return false;
                    }
                    if (sound1.Name != sound2.Name)
                    {
                        m_Log.Fatal("Gesture sequence item Sound not identical");
                        return false;
                    }
                }
                if (gesture.Sequence[i] is Gesture.StepWait)
                {
                    Gesture.StepWait wait1 = (Gesture.StepWait)gesture.Sequence[i];
                    Gesture.StepWait wait2 = (Gesture.StepWait)gestureserialized.Sequence[i];

                    if (wait1.WaitTime != wait2.WaitTime)
                    {
                        m_Log.Fatal("Gesture sequence item Wait not identical");
                        return false;
                    }
                    if (wait1.WaitForTime != wait2.WaitForTime)
                    {
                        m_Log.Fatal("Gesture sequence item Wait not identical");
                        return false;
                    }
                    if (wait1.WaitForAnimation != wait2.WaitForAnimation)
                    {
                        m_Log.Fatal("Gesture sequence item Wait not identical");
                        return false;
                    }
                }
            }

            m_Log.Info("Testing references");
            List<UUID> refs = gestureserialized.References;

            if (refs.Count != 2)
            {
                m_Log.Fatal("Gesture Item Reference count is wrong");
                return false;
            }

            if (!refs.Contains(soundID))
            {
                m_Log.FatalFormat("Gesture Inventory Item AssetID {0} is not referenced", soundID);
                return false;
            }

            if (!refs.Contains(animID))
            {
                m_Log.FatalFormat("Gesture Inventory Item AssetID {0} is not referenced", animID);
                return false;
            }

            return true;
        }

        public void Startup(ConfigurationLoader loader)
        {
        }
    }
}

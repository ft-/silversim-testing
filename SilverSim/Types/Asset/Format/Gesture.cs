// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SilverSim.Types.Asset.Format
{
    public class Gesture : IReferencesAccessor
    {
        public enum StepType
        {
            Animation = 0,
            Sound = 1,
            Chat = 2,
            Wait = 3,
            EndOfGesture = 4
        };

        #region Steps
        public interface IStep
        {
            StepType Type { get; }
            string Serialize();
        }

        public class StepAnimation : IStep
        {
            public StepType Type
            {
                get
                {
                    return StepType.Animation;
                }
            }

            public bool AnimationStart = true;
            public UUID AssetID = UUID.Zero;
            public string Name;

            public string Serialize()
            {
                return string.Format("{0}\n{1}\n{2}\n{3}\n",
                    (int)Type, Name, AssetID, AnimationStart ? "1" : "0");
            }

            public StepAnimation()
            {

            }

            public StepAnimation(string name, UUID assetID, bool animStart)
            {
                Name = name;
                AssetID = assetID;
                AnimationStart = animStart;
            }
        }

        public class StepSound : IStep
        {
            public StepType Type
            {
                get
                {
                    return StepType.Sound;
                }
            }

            public UUID AssetID = UUID.Zero;
            public string Name;

            public string Serialize()
            {
                return string.Format("{0}\n{1}\n{2}\n",
                    (int)Type, Name, AssetID);
            }

            public StepSound()
            {

            }

            public StepSound(string name, UUID assetID)
            {
                Name = name;
                AssetID = assetID;
            }
        }

        public class StepChat : IStep
        {
            public StepType Type
            {
                get
                {
                    return StepType.Chat;
                }
            }

            public string Text;

            public string Serialize()
            {
                return string.Format("{0}\n{1}\n0\n",
                    (int)Type, Text);
            }

            public StepChat()
            {

            }

            public StepChat(string text)
            {
                Text = text;
            }
        }

        public class StepWait : IStep
        {
            public StepType Type
            {
                get
                {
                    return StepType.Wait;
                }
            }

            public bool WaitForAnimation;
            public bool WaitForTime;
            public float WaitTime;


            public string Serialize()
            {
                int waitFlags = 0;
                if(WaitForTime)
                {
                    waitFlags |= 0x01;
                }
                if(WaitForAnimation)
                {
                    waitFlags |= 0x02;
                }
                return string.Format(CultureInfo.InvariantCulture, "{0}\n{1:0.000000}\n{2}\n",
                    (int)Type, WaitTime, waitFlags);
            }

            public StepWait()
            {

            }

            public StepWait(float waitTime, bool waitForAnim, bool waitForTime)
            {
                WaitTime = waitTime;
                WaitForAnimation = waitForAnim;
                WaitForTime = waitForTime;
            }
        }

        public class StepEndOfGesture : IStep
        {
            public StepType Type
            {
                get
                {
                    return StepType.EndOfGesture;
                }
            }

            public string Serialize()
            {
                return ((int)Type).ToString();
            }

            public StepEndOfGesture()
            {

            }
        }

        #endregion

        #region Fields
        public byte TriggerKey;
        public uint TriggerKeyMask;
        public string Trigger;
        public string ReplaceWith;
        public List<IStep> Sequence = new List<IStep>();
        #endregion

        #region Constructors
        public Gesture()
        {

        }

        public Gesture(AssetData asset)
        {
            string input = asset.Data.FromUTF8Bytes();
            input = input.Replace('\t', ' ');
            List<string> lines = new List<string>(input.Split('\n'));
            using (List<string>.Enumerator e = lines.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw new NotAGestureFormatException();
                }
                int i;

                if(!int.TryParse(e.Current, out i))
                {
                    throw new NotAGestureFormatException();
                }
                if (i != 2)
                {
                    throw new NotAGestureFormatException();
                }

                if (!e.MoveNext())
                {
                    throw new NotAGestureFormatException();
                }

                if(!byte.TryParse(e.Current, out TriggerKey))
                {
                    throw new NotAGestureFormatException();
                }

                if (!e.MoveNext())
                {
                    throw new NotAGestureFormatException();
                }
                
                if (!uint.TryParse(e.Current, out TriggerKeyMask))
                {
                    throw new NotAGestureFormatException();
                }

                if (!e.MoveNext())
                {
                    throw new NotAGestureFormatException();
                }
                Trigger = e.Current;

                if (!e.MoveNext())
                {
                    throw new NotAGestureFormatException();
                }
                ReplaceWith = e.Current;

                if (!e.MoveNext())
                {
                    throw new NotAGestureFormatException();
                }
                int count;
                if(!int.TryParse(e.Current, out count))
                {
                    throw new NotAGestureFormatException();
                }

                if (count < 0)
                {
                    throw new NotAGestureFormatException();
                }

                for (int idx = 0; idx < count; ++idx)
                {
                    if (!e.MoveNext())
                    {
                        throw new NotAGestureFormatException();
                    }

                    int intval;
                    if (!int.TryParse(e.Current, out intval))
                    {
                        throw new NotAGestureFormatException();
                    }
                    StepType type = (StepType)intval;

                    switch (type)
                    {
                        case StepType.EndOfGesture:
                            Sequence.Add(new StepEndOfGesture());
                            return;

                        case StepType.Animation:
                            {
                                StepAnimation step = new StepAnimation();
                                if (e.MoveNext())
                                {
                                    step.Name = e.Current;
                                }
                                if (e.MoveNext() && 
                                    !UUID.TryParse(e.Current, out step.AssetID))
                                {
                                    throw new NotAGestureFormatException();
                                }
                                if (e.MoveNext() && 
                                    !int.TryParse(e.Current, out intval))
                                {
                                    throw new NotAGestureFormatException();
                                }
                                step.AnimationStart = intval != 0;
                                Sequence.Add(step);
                            }
                            break;

                        case StepType.Sound:
                            {
                                StepSound step = new StepSound();
                                if (e.MoveNext())
                                {
                                    step.Name = e.Current;
                                }
                                if (e.MoveNext() &&
                                    !UUID.TryParse(e.Current, out step.AssetID))
                                {
                                    throw new NotAGestureFormatException();
                                }
                                Sequence.Add(step);
                            }
                            break;

                        case StepType.Chat:
                            {
                                StepChat step = new StepChat();
                                if (e.MoveNext())
                                {
                                    step.Text = e.Current;
                                }
                                e.MoveNext();
                                Sequence.Add(step);
                            }
                            break;

                        case StepType.Wait:
                            {
                                StepWait step = new StepWait();
                                if (e.MoveNext() &&
                                    !float.TryParse(e.Current, NumberStyles.Float, CultureInfo.InvariantCulture, out step.WaitTime))
                                {
                                    throw new NotAGestureFormatException();
                                }
                                if (e.MoveNext())
                                {
                                    int flags;
                                    if(!int.TryParse(e.Current, out flags))
                                    {
                                        throw new NotAGestureFormatException();
                                    }
                                    step.WaitForTime = (flags & 0x01) != 0;
                                    step.WaitForAnimation = (flags & 0x02) != 0;
                                }
                                Sequence.Add(step);
                            }
                            break;

                        default:
                            throw new NotAGestureFormatException();
                    }
                }
            }
        }
        #endregion

        #region References interface
        public List<UUID> References
        {
            get
            {
                List<UUID> refs = new List<UUID>();
                foreach(IStep step in Sequence)
                {
                    StepSound stepsound;
                    StepAnimation stepanim;
                    stepsound = step as StepSound;
                    if(null != stepsound)
                    {
                        if (!refs.Contains(stepsound.AssetID))
                        {
                            refs.Add(stepsound.AssetID);
                        }
                        continue;
                    }
                    
                    stepanim = step as StepAnimation;
                    if(null != (stepanim))
                    {
                        if (!refs.Contains(stepanim.AssetID))
                        {
                            refs.Add(stepanim.AssetID);
                        }
                        continue;
                    }
                }
                return refs;
            }
        }
        #endregion

        #region Operators

        public AssetData Asset()
        {
            return (AssetData)this;
        }

        public static implicit operator AssetData(Gesture v)
        {
            AssetData asset = new AssetData();
            StringBuilder sb = new StringBuilder();
            sb.Append("2\n");
            sb.Append(v.TriggerKey.ToString() + "\n");
            sb.Append(v.TriggerKeyMask.ToString() + "\n");
            sb.Append(v.Trigger + "\n");
            sb.Append(v.ReplaceWith + "\n");

            int count = 0;

            if(v.Sequence != null)
            {
                count = v.Sequence.Count;
                sb.Append(count.ToString() + "\n");
            }

            for (int i = 0; i < count; ++i)
            {
                IStep s = v.Sequence[i];
                sb.Append(s.Serialize());
            }

            asset.Data = sb.ToString().ToUTF8Bytes();
            asset.Type = AssetType.Gesture;
            asset.Name = "Gesture";
            return asset;
        }

        #endregion
    }
}

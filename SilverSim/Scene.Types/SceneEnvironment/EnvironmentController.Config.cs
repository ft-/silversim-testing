// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3


using System.IO;
using SilverSim.Types;
using System.Xml;
using System;
using System.Runtime.Serialization;
using System.Linq;

namespace SilverSim.Scene.Types.SceneEnvironment
{
    public partial class EnvironmentController
    {
        public event Action<byte[]> OnEnvironmentControllerChangeParams;
        readonly object m_SerializationLock = new object();
        bool m_InDeserialization;

        public void TriggerOnEnvironmentControllerChange()
        {
            if (!m_InDeserialization)
            {
                byte[] data = Serialization;
                var ev = OnEnvironmentControllerChangeParams; /* events are not exactly thread-safe */
                if (ev != null)
                {
                    foreach (Action<byte[]> del in ev.GetInvocationList().OfType<Action<byte[]>>())
                    {
                        del(data);
                    }
                }
            }
        }

        public void ResetToDefaults()
        {
            uint hoursInSec = 3600;
            AverageSunTilt = -0.25 * Math.PI;
            SeasonalSunTilt = 0.03 * Math.PI;
            SunNormalizedOffset = 0.45;
            SetSunDurationParams(4 * hoursInSec, 11);
            MoonPhaseOffset = 0;
            MoonPeriodLengthInSecs = 2.1 * hoursInSec;
            this[BooleanWaterParams.EnableTideControl] = false;
            this[FloatWaterParams.TidalBase] = 20;
            this[FloatWaterParams.TidalMoonAmplitude] = 0.5;
            this[FloatWaterParams.TidalSunAmplitude] = 0.1;
            SunUpdateEveryMsecs = 10000;
            SendSimTimeEveryNthSunUpdate = 10;
            UpdateWindModelEveryMsecs = 10000;
            UpdateTidalModelEveryMsecs = 60000;
        }

        public byte[] Serialization
        {
            get
            {
                lock(m_SerializationLock)
                {
                    try
                    {
                        m_InDeserialization = true;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (XmlTextWriter writer = ms.UTF8XmlTextWriter())
                            {
                                writer.WriteStartElement("EnvironmentController");
                                {
                                    writer.WriteStartElement("Wind");
                                    {
                                        writer.WriteNamedValue("UpdateWindModelEveryMsecs", UpdateWindModelEveryMsecs);
                                    }
                                    writer.WriteEndElement();
                                    writer.WriteStartElement("Sun");
                                    {
                                        writer.WriteNamedValue("SunUpdateEveryMsecs", SunUpdateEveryMsecs);
                                        writer.WriteNamedValue("SendSimTimeEveryNthSunUpdate", SendSimTimeEveryNthSunUpdate);
                                        writer.WriteNamedValue("AverageSunTilt", AverageSunTilt);
                                        writer.WriteNamedValue("SeasonalSunTilt", SeasonalSunTilt);
                                        writer.WriteNamedValue("NormalizedOffset", SunNormalizedOffset);
                                        uint sun_secperday;
                                        uint sun_daysperyear;
                                        GetSunDurationParams(out sun_secperday, out sun_daysperyear);
                                        writer.WriteNamedValue("SecondsPerDay", sun_secperday);
                                        writer.WriteNamedValue("DaysPerYear", sun_daysperyear);
                                    }
                                    writer.WriteEndElement();
                                    writer.WriteStartElement("Moon");
                                    {
                                        writer.WriteNamedValue("PhaseOffset", MoonPhaseOffset);
                                        writer.WriteNamedValue("PeriodLengthInSeconds", MoonPeriodLengthInSecs);
                                    }
                                    writer.WriteEndElement();
                                    writer.WriteStartElement("Water");
                                    {
                                        writer.WriteNamedValue("EnableTideControl", this[BooleanWaterParams.EnableTideControl]);
                                        writer.WriteNamedValue("UpdateTidalModelEveryMsecs", UpdateTidalModelEveryMsecs);
                                        writer.WriteNamedValue("MoonTidalAmplitude", this[FloatWaterParams.TidalMoonAmplitude]);
                                        writer.WriteNamedValue("SunTidalAmplitude", this[FloatWaterParams.TidalSunAmplitude]);
                                    }
                                    writer.WriteEndElement();
                                    writer.WriteStartElement("Weather");
                                    {
                                        writer.WriteNamedValue("EnableLightShare", this[BooleanWeatherParams.EnableLightShare]);
                                    }
                                    writer.WriteEndElement();
                                }
                                writer.WriteEndElement();
                            }
                            return ms.GetBuffer();
                        }
                    }
                    finally
                    {
                        m_InDeserialization = false;
                    }
                }
            }
            set
            {
                lock(m_SerializationLock)
                {
                    using (MemoryStream ms = new MemoryStream(value))
                    {
                        using (XmlTextReader reader = new XmlTextReader(ms))
                        {
                            DeserializeRoot(reader);
                        }
                    }
                }
            }
        }

        public class InvalidEnvironmentControllerSerializationException : Exception
        {
            public InvalidEnvironmentControllerSerializationException()
            {

            }

            public InvalidEnvironmentControllerSerializationException(string message)
                : base(message)
            {

            }
            public InvalidEnvironmentControllerSerializationException(string message, Exception innerException)
                : base(message, innerException)
            {

            }

            protected InvalidEnvironmentControllerSerializationException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {

            }

        }

        void DeserializeRoot(XmlTextReader reader)
        {
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidEnvironmentControllerSerializationException();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.Name != "EnvironmentController")
                        {
                            throw new InvalidEnvironmentControllerSerializationException();
                        }
                        if(reader.IsEmptyElement)
                        {
                            return;
                        }
                        DeserializeEnvironmentController(reader);
                        return;

                    default:
                        break;
                }
            }
        }

        void DeserializeEnvironmentController(XmlTextReader reader)
        {
            for(;;)
            {
                if(!reader.Read())
                {
                    throw new InvalidEnvironmentControllerSerializationException();
                }

                switch(reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if(reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch(reader.Name)
                        {
                            case "Wind":
                                DeserializeEnvironmentController_Wind(reader);
                                break;

                            case "Sun":
                                DeserializeEnvironmentController_Sun(reader);
                                break;

                            case "Moon":
                                DeserializeEnvironmentController_Moon(reader);
                                break;

                            case "Water":
                                DeserializeEnvironmentController_Water(reader);
                                break;

                            case "Weather":
                                DeserializeEnvironmentController_Weather(reader);
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if(reader.Name != "EnvironmentController")
                        {
                            throw new InvalidEnvironmentControllerSerializationException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        void DeserializeEnvironmentController_Wind(XmlTextReader reader)
        {
            uint sun_secperday;
            uint sun_daysperyear;
            GetSunDurationParams(out sun_secperday, out sun_daysperyear);

            for (;;)
            {
                if (!reader.Read())
                {
                    throw new InvalidEnvironmentControllerSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch (reader.Name)
                        {
                            case "UpdateWindModelEveryMsecs":
                                UpdateWindModelEveryMsecs = reader.ReadElementValueAsInt();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Wind")
                        {
                            throw new InvalidEnvironmentControllerSerializationException();
                        }
                        SetSunDurationParams(sun_secperday, sun_daysperyear);
                        return;

                    default:
                        break;
                }
            }
        }

        void DeserializeEnvironmentController_Sun(XmlTextReader reader)
        {
            uint sun_secperday;
            uint sun_daysperyear;
            GetSunDurationParams(out sun_secperday, out sun_daysperyear);

            for (;;)
            {
                if (!reader.Read())
                {
                    throw new InvalidEnvironmentControllerSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch(reader.Name)
                        {
                            case "SunUpdateEveryMsecs":
                                SunUpdateEveryMsecs = reader.ReadElementValueAsInt();
                                break;

                            case "SendSimTimeEveryNthSunUpdate":
                                SendSimTimeEveryNthSunUpdate = reader.ReadElementValueAsUInt();
                                break;

                            case "AverageSunTilt":
                                AverageSunTilt = reader.ReadElementValueAsDouble();
                                break;

                            case "SeasonalSunTilt":
                                SeasonalSunTilt = reader.ReadElementValueAsDouble();
                                break;

                            case "NormalizedOffset":
                                SunNormalizedOffset = reader.ReadElementValueAsDouble();
                                break;

                            case "SecondsPerDay":
                                sun_secperday = reader.ReadElementValueAsUInt();
                                break;

                            case "DaysPerYear":
                                sun_daysperyear = reader.ReadElementValueAsUInt();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Sun")
                        {
                            throw new InvalidEnvironmentControllerSerializationException();
                        }
                        SetSunDurationParams(sun_secperday, sun_daysperyear);
                        return;

                    default:
                        break;
                }
            }
        }

        void DeserializeEnvironmentController_Moon(XmlTextReader reader)
        {
            for (;;)
            {
                if (!reader.Read())
                {
                    throw new InvalidEnvironmentControllerSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch (reader.Name)
                        {
                            case "PhaseOffset":
                                MoonPhaseOffset = reader.ReadElementValueAsDouble();
                                break;

                            case "PeriodLengthInSeconds":
                                MoonPeriodLengthInSecs = reader.ReadElementValueAsDouble();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Moon")
                        {
                            throw new InvalidEnvironmentControllerSerializationException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        void DeserializeEnvironmentController_Water(XmlTextReader reader)
        {
            for (;;)
            {
                if (!reader.Read())
                {
                    throw new InvalidEnvironmentControllerSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch (reader.Name)
                        {
                            case "EnableTideControl":
                                this[BooleanWaterParams.EnableTideControl] = reader.ReadElementValueAsBoolean();
                                break;

                            case "UpdateTidalModelEveryMsecs":
                                UpdateTidalModelEveryMsecs = reader.ReadElementValueAsInt();
                                break;

                            case "MoonTidalAmplitude":
                                this[FloatWaterParams.TidalMoonAmplitude] = reader.ReadElementValueAsDouble();
                                break;

                            case "SunTidalAmplitude":
                                this[FloatWaterParams.TidalSunAmplitude] = reader.ReadElementValueAsDouble();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Water")
                        {
                            throw new InvalidEnvironmentControllerSerializationException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }

        void DeserializeEnvironmentController_Weather(XmlTextReader reader)
        {
            for (;;)
            {
                if (!reader.Read())
                {
                    throw new InvalidEnvironmentControllerSerializationException();
                }

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.IsEmptyElement)
                        {
                            break;
                        }
                        switch (reader.Name)
                        {
                            case "EnableLightShare":
                                this[BooleanWeatherParams.EnableLightShare] = reader.ReadElementValueAsBoolean();
                                break;

                            default:
                                reader.ReadToEndElement();
                                break;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        if (reader.Name != "Weather")
                        {
                            throw new InvalidEnvironmentControllerSerializationException();
                        }
                        return;

                    default:
                        break;
                }
            }
        }
    }
}

﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Types.Parcel
{
    public class ParcelAccessEntry
    {
        public UUID ParcelID;
        public UUI Accessor;
        public Date ExpiresAt = null;

        public ParcelAccessEntry()
        {

        }
    }
}

﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

namespace SilverSim.Scene.ServiceInterfaces.Chat
{
    public abstract class ChatServiceFactoryInterface
    {
        protected ChatServiceFactoryInterface()
        {

        }

        public abstract ChatServiceInterface Instantiate();
    }
}

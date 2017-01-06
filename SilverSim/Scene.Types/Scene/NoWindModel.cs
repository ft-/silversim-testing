﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using System;
using SilverSim.Types;
using EnvironmentController = SilverSim.Scene.Types.SceneEnvironment.EnvironmentController;

namespace SilverSim.Scene.Types.Scene
{
    /* this class provides a simple no wind model for having at least some Wind model hooked to EnvironmentController */
    public class NoWindModel : IWindModel, IWindModelPreset
    {
        public Vector3 this[Vector3 pos]
        {
            get
            {
                return new Vector3();
            }

            set
            {
                /* intentionally left empty */
            }
        }

        public IWindModelPreset PresetWind
        {
            get
            {
                return this;
            }
        }

        public Vector3 PrevailingWind
        {
            get
            {
                return new Vector3();
            }
        }

        public void UpdateModel(EnvironmentController.SunData sunData, double dt)
        {
            /* intentionally left empty */
        }
    }
}

﻿// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3

using SilverSim.Types;
using EnvironmentController = SilverSim.Scene.Types.SceneEnvironment.EnvironmentController;

namespace SilverSim.Scene.Types.Scene
{
    public interface IWindModelPreset
    {
        Vector3 this[Vector3 pos] { get; set; }
    }

    public interface IWindModel
    {
        Vector3 this[Vector3 pos] { get; set; }
        Vector3 PrevailingWind { get; }

        IWindModelPreset PresetWind { get; }

        void UpdateModel(EnvironmentController.SunData sunData, double dt);
    }

    public interface IWindModelFactory
    {
        IWindModel Instantiate(SceneInterface scene);
    }
}

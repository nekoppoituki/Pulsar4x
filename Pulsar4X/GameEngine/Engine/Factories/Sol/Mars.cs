﻿using Pulsar4X.Orbital;
using System;
using System.Collections.Generic;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Blueprints;
using Pulsar4X.Extensions;

namespace Pulsar4X.Engine.Sol
{
    public static partial class SolEntities
    {
        /// <summary>
        /// Creates Mars
        /// </summary>
        /// <remarks>https://en.wikipedia.org/wiki/Mars</remarks>
        public static Entity Mars(Game game, StarSystem sol, Entity sun, DateTime epoch, SensorProfileDB sensorProfile)
        {
            MassVolumeDB sunMVDB = sun.GetDataBlob<MassVolumeDB>();

            SystemBodyInfoDB planetBodyDB = new SystemBodyInfoDB { BodyType = BodyType.Terrestrial, SupportsPopulations = true, Albedo = 0.25f };
            MassVolumeDB planetMVDB = MassVolumeDB.NewFromMassAndRadius_AU(0.64174E24, Distance.KmToAU(3396.2));
            NameDB planetNameDB = new NameDB("Mars");

            double planetSemiMajorAxisAU = Distance.KmToAU(227.92E6);
            double planetEccentricity = 0.0934;
            double planetEclipticInclination = 0;   // 1.85
            double planetLoAN = 49.57854;
            double planetAoP = 336.04084;
            double planetMeanAnomaly = 355.45332;

            OrbitDB planetOrbitDB = OrbitDB.FromMajorPlanetFormat(sun, sunMVDB.MassDry, planetMVDB.MassDry, planetSemiMajorAxisAU, planetEccentricity, planetEclipticInclination, planetLoAN, planetAoP, planetMeanAnomaly, epoch);
            planetBodyDB.BaseTemperature = (float)SystemBodyFactory.CalculateBaseTemperatureOfBody(sun, planetOrbitDB);
            planetBodyDB.Tectonics = TectonicActivity.EarthLike;
            PositionDB planetPositionDB = new PositionDB(planetOrbitDB.GetPosition(game.TimePulse.GameGlobalDateTime), sol.Guid, sun);

            var pressureAtm = Pressure.KPaToAtm(0.87f);
            var atmoGasses = new Dictionary<GasBlueprint, float>
            {
                { game.GetGasBySymbol("CO2"), 0.9597f * pressureAtm },
                { game.GetGasBySymbol("Ar"), 0.0193f * pressureAtm },
                { game.GetGasBySymbol("N2"), 0.0189f * pressureAtm },
                { game.GetGasBySymbol("O2"), 0.00146f * pressureAtm },
                { game.GetGasBySymbol("CO"), 0.000557f * pressureAtm },
                { game.GetGasBySymbol("H2O"), 0.000210f * pressureAtm },
                { game.GetGasBySymbol("NO"), 0.0001f * pressureAtm },
                { game.GetGasBySymbol("Ne"), 0.0000025f * pressureAtm },
                { game.GetGasBySymbol("Kr"), 0.0000003f * pressureAtm },
                { game.GetGasBySymbol("Xe"), 0.0000001f * pressureAtm }
            };
            AtmosphereDB planetAtmosphereDB = new AtmosphereDB(pressureAtm, false, 0, 0, 0, -63, atmoGasses);

            Entity planet = Entity.Create();
            sol.AddEntity(planet, new List<BaseDataBlob> { sensorProfile, planetPositionDB, planetBodyDB, planetMVDB, planetNameDB, planetOrbitDB, planetAtmosphereDB });
            SensorTools.PlanetEmmisionSig(sensorProfile, planetBodyDB, planetMVDB);
            return planet;
        }
    }
}

﻿using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public static class ShipFactory
    {
        public static Entity CreateShip(Entity classEntity, EntityManager systemEntityManager, Entity ownerFaction, Vector4 pos, StarSystem starsys, string shipName = null)
        {
            // @todo replace ownerFaction with formationDB later. Now ownerFaction used just to add name 
            ProtoEntity protoShip = classEntity.Clone();

            ShipInfoDB shipInfoDB = protoShip.GetDataBlob<ShipInfoDB>();
            shipInfoDB.ShipClassDefinition = classEntity.Guid;

            if (shipName == null)
            {
                shipName = "Ship Name";
            } 

            NameDB nameDB = new NameDB(shipName);
            protoShip.SetDataBlob(nameDB);

            var OwnedDB = new OwnedDB(ownerFaction);           
            protoShip.SetDataBlob(OwnedDB);

            PositionDB position = new PositionDB(pos, starsys.Guid);
            protoShip.SetDataBlob(position);

            Entity shipEntity = new Entity(systemEntityManager, protoShip);

            //replace the ships references to the design's specific instances with shiny new specific instances
            ComponentInstancesDB componentInstances = shipEntity.GetDataBlob<ComponentInstancesDB>();
            var newSpecificInstances = new Dictionary<Entity, List<Entity>>();
            foreach (var kvp in componentInstances.SpecificInstances)
            {
                newSpecificInstances.Add(kvp.Key, new List<Entity>());
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    newSpecificInstances[kvp.Key].Add(ComponentInstanceFactory.NewInstanceFromDesignEntity(kvp.Key, ownerFaction));
                }
            }
            componentInstances.SpecificInstances = newSpecificInstances;


            foreach (var componentType in shipEntity.GetDataBlob<ComponentInstancesDB>().SpecificInstances)
            {
                foreach (var componentInstance in componentType.Value)
                {
                    AttributeToAbilityMap.AddAbility(shipEntity, componentType.Key, componentInstance);
                }
            }

            ReCalcProcessor.ReCalcAbilities(shipEntity);
            return shipEntity;
        }

        public static Entity CreateNewShipClass(Game game, Entity faction, string className = null)
        {
            //check className before any to use it in NameDB constructor
            if (string.IsNullOrEmpty(className))
            {
                ///< @todo source the class name from faction theme.
                className = "New Class"; // <- Hack for now.
            }

            // lets start by creating all the Datablobs that make up a ship class: TODO only need to add datablobs for compoents it has abilites for.
            var shipInfo = new ShipInfoDB();
            var armor = new ArmorDB();
            var buildCost = new BuildCostDB();
            var cargotype = new CargoTypeDB();
            var crew = new CrewDB();
            var damage = new DamageDB();
            var maintenance = new MaintenanceDB();
            var sensorProfile = new SensorProfileDB();
            var sensors = new SensorsDB();
            var name = new NameDB(className);
            var componentInstancesDB = new ComponentInstancesDB();
            // now lets create a list of all these datablobs so we can create our new entity:
            List<BaseDataBlob> shipDBList = new List<BaseDataBlob>()
            {
                shipInfo,
                armor,
                buildCost,
                cargotype,
                crew,
                damage,
                maintenance,
                sensorProfile,
                sensors,
                name,
                componentInstancesDB
            };

            // now lets create the ship class:
            Entity shipClassEntity = new Entity(game.GlobalManager, shipDBList); 

            // also gets factionDB:
            FactionInfoDB factionDB = faction.GetDataBlob<FactionInfoDB>();

            // and add it to the faction:
            factionDB.ShipClasses.Add(shipClassEntity);

            // now lets set some ship info:
            shipInfo.ShipClassDefinition = Guid.Empty; // just make sure it is marked as a class and not a ship.

            // now lets add some components:
            ///< @todo Add ship components
            // -- basic armour of current faction tech level
            // -- minimum crew quaters defaulting to 3 months deployment time.
            // -- a bridge
            // -- an engineering space
            // -- a fuel tank
            
            // now update the ship system DBs to reflect the components:
            ///< @todo update ship to reflect added components

            return shipClassEntity;
        }


    }
}

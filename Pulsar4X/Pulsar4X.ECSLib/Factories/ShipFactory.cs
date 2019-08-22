﻿using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.ECSLib.ComponentFeatureSets.Damage;

namespace Pulsar4X.ECSLib
{
    public static class ShipFactory
    {
        public static Entity CreateShip(Entity classEntity,  Entity ownerFaction, Entity parent, StarSystem starsys, string shipName = null)
        {
            Vector3 position = parent.GetDataBlob<PositionDB>().AbsolutePosition_m;
            var distanceFromParent = Distance.AuToMt( parent.GetDataBlob<MassVolumeDB>().Radius * 2);
            position.X += distanceFromParent;
            Entity ship = CreateShip(classEntity, ownerFaction, position, starsys, shipName);
            ship.GetDataBlob<PositionDB>().SetParent(parent);
            var orbitDB = OrbitDB.FromPosition(parent, ship, starsys.ManagerSubpulses.StarSysDateTime);
            ship.SetDataBlob(orbitDB);
            return ship;
        }

        public static Entity CreateShip(Entity classEntity, Entity ownerFaction, Vector3 pos, StarSystem starsys, string shipName = null)
        {
            // @todo replace ownerFaction with formationDB later. Now ownerFaction used just to add name 
            // @todo: make sure each component design and component instance is unique, not duplicated
            ProtoEntity protoShip = classEntity.Clone();

            ShipInfoDB shipInfoDB = protoShip.GetDataBlob<ShipInfoDB>();
            shipInfoDB.ShipClassDefinition = classEntity.Guid;

            if (shipName == null)
            {
                shipName = "Ship Name";
            } 

            NameDB nameDB = new NameDB(shipName);
            nameDB.SetName(ownerFaction.Guid, shipName);
            protoShip.SetDataBlob(nameDB);

            OrderableDB orderableDB = new OrderableDB();
            protoShip.SetDataBlob(orderableDB);

            PositionDB position = new PositionDB(Distance.MToAU(pos), starsys.Guid);
            protoShip.SetDataBlob(position);


            protoShip.SetDataBlob(new DesignInfoDB(classEntity));

            //replace the ships references to the design's specific instances with shiny new specific instances

            ComponentInstancesDB classInstances = classEntity.GetDataBlob<ComponentInstancesDB>();


            Entity shipEntity = new Entity(starsys, ownerFaction.Guid, protoShip);
            shipEntity.RemoveDataBlob<ComponentInstancesDB>();
            shipEntity.SetDataBlob(new ComponentInstancesDB());
            if(shipEntity.HasDataBlob<FireControlAbilityDB>())
                shipEntity.RemoveDataBlob<FireControlAbilityDB>();

            foreach (var designKVP in classInstances.DesignsAndComponentCount)
            {
                for (int i = 0; i < designKVP.Value; i++)
                {
                    var newInstance = new ComponentInstance(designKVP.Key); 
                    EntityManipulation.AddComponentToEntity(shipEntity, newInstance);
                }
            }



            FactionOwnerDB factionOwner = ownerFaction.GetDataBlob<FactionOwnerDB>();
            factionOwner.SetOwned(shipEntity);
            ComponentInstancesDB shipComponentInstanceDB = shipEntity.GetDataBlob<ComponentInstancesDB>();

            //TODO: do this somewhere else, recalcprocessor maybe?
            foreach (var design in shipComponentInstanceDB.GetDesignsByType(typeof(SensorReceverAtbDB)))
            {
                foreach (var instance in shipComponentInstanceDB.GetComponentsBySpecificDesign(design.Guid))
                {
                    SensorReceverAtbDB sensor = (SensorReceverAtbDB)design.AttributesByType[typeof(SensorReceverAtbDB)];
                    DateTime nextDatetime = shipEntity.Manager.ManagerSubpulses.StarSysDateTime + TimeSpan.FromSeconds(sensor.ScanTime);
                    shipEntity.Manager.ManagerSubpulses.AddEntityInterupt(nextDatetime, new SensorScan().TypeName, instance.ParentEntity);
                }
            }   
            

            ReCalcProcessor.ReCalcAbilities(shipEntity);
            return shipEntity;
        }

        public class ShipDesign
        {
            private Guid DesignID;
            public string DesignName;
            public int DesignVersion;
            public bool IsObsolete = false;
            public double Mass;
            public double Volume;
            public List<(ComponentDesign design, int count)> Components;
            public (string name, double density, float thickness) Armor;
            public Dictionary<MineralSD, int> MineralCost;
            public Dictionary<ProcessedMaterialSD, int> MaterialCost;
            public Dictionary<ComponentDesign, int> ComponentCost;
            public int CrewReq;
            public int BuildPointCost;
            public int CreditCost;
            public EntityDamageProfileDB DamageProfileDB;


            public ShipDesign(FactionInfoDB factionInfoDB )
            {
                DesignID = Guid.NewGuid();
                factionInfoDB.ShipDesigns.Add(this);
                
            }

        }

        public static Entity ShipFromDesign(ShipDesign design, Entity ownerFaction, Entity parent, StarSystem starsys, string shipName = null)
        {
            Vector3 position = parent.GetDataBlob<PositionDB>().AbsolutePosition_m;
            var distanceFromParent = Distance.AuToMt( parent.GetDataBlob<MassVolumeDB>().Radius * 2);
            position.X += distanceFromParent;
            List<BaseDataBlob> dataBlobs = new List<BaseDataBlob>();

            
            var mvdb = MassVolumeDB.NewFromMassAndVolume(design.Mass, design.Volume);
            dataBlobs.Add(mvdb);
            PositionDB posdb = new PositionDB(position, starsys.Guid, parent);
            dataBlobs.Add(posdb);
            EntityDamageProfileDB damagedb = (EntityDamageProfileDB)design.DamageProfileDB.Clone(); 
            dataBlobs.Add(damagedb);
            ComponentInstancesDB compInstances = new ComponentInstancesDB();
            dataBlobs.Add(compInstances);

            var ship = Entity.Create(starsys, ownerFaction.Guid, dataBlobs);
            
            //some DB's need tobe created after the entity.
            var namedb = new NameDB(ship.Guid.ToString());
            namedb.SetName(ownerFaction.Guid, shipName);
            OrbitDB orbit = OrbitDB.FromPosition(parent, ship, starsys.ManagerSubpulses.StarSysDateTime);
            ship.SetDataBlob(namedb);
            ship.SetDataBlob(orbit);

            foreach (var item in design.Components)
            {
                EntityManipulation.AddComponentToEntity(ship, item.design, item.count);
            }
            
            return ship;
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
            var cargotype = new CargoAbleTypeDB();
            var crew = new CrewDB();
            var damage = new DamageDB();
            var maintenance = new MaintenanceDB();
            var sensorProfile = new SensorProfileDB();
            var name = new NameDB(className);
            name.SetName(faction.Guid, className);
            var componentInstancesDB = new ComponentInstancesDB();
            var massVolumeDB = new MassVolumeDB();

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
                name,
                componentInstancesDB,
                massVolumeDB,
            };

            // now lets create the ship class:
            Entity shipClassEntity = new Entity(game.GlobalManager, faction.Guid, shipDBList); 
            FactionOwnerDB factionOwner = faction.GetDataBlob<FactionOwnerDB>();
            factionOwner.SetOwned(shipClassEntity);
            // also gets factionDB:
            FactionInfoDB factionDB = faction.GetDataBlob<FactionInfoDB>();

            // and add it to the faction:
            factionDB.ShipClasses.Add(shipClassEntity);

            // now lets set some ship info:
            shipInfo.ShipClassDefinition = Guid.Empty; // just make sure it is marked as a class and not a ship.
            
            return shipClassEntity;
        }


    }
}

using System;
using System.Collections.Generic;
using Pulsar4X.Orbital;
using Pulsar4X.Engine;
using Pulsar4X.Interfaces;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.Datablobs
{

    public struct Manuver
    {
        public enum ManuverType
        {
            Drift,
            NewtonSimple,
            NewtonThrust,
            Warp,
            Wormhole
        }
        public ManuverType TypeOfManuver;
        public DateTime StartDateTime;
        public DateTime EndDateTime;
        public KeplerElements StartKepler;
        public KeplerElements EndKepler;
        public Entity StartSOIParent;
        public Entity EndSOIParent;
    }
    public class NavSequenceDB : BaseDataBlob
    {
        public string CurrentActivity { get; internal set; }
        public List<Manuver> ManuverNodes = new List<Manuver>();

        internal void AddManuver(Manuver.ManuverType type, DateTime startDate, Entity StartParent, KeplerElements startKE, DateTime endDate, Entity EndParent, KeplerElements endKE)
        {
            var node = new Manuver()
            {
                TypeOfManuver = type,
                StartDateTime = startDate,
                StartSOIParent = StartParent,
                StartKepler = startKE,
                EndDateTime = endDate,
                EndSOIParent = EndParent,
                EndKepler = endKE,
            };
            ManuverNodes.Add(node);
            StartParent.Manager.ManagerSubpulses.AddEntityInterupt(startDate, nameof(NavSequenceProcessor), OwningEntity);
            EndParent.Manager.ManagerSubpulses.AddEntityInterupt(endDate, nameof(NavSequenceProcessor), OwningEntity);
        }
        
        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
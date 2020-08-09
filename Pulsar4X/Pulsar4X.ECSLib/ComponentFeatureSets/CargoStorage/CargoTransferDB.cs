﻿using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class CargoTransferDB : BaseDataBlob
    {
        internal Guid TransferJobID { get; } = Guid.NewGuid();

        internal Entity CargoFromEntity { get; set; }
        [JsonIgnore]
        internal VolumeStorageDB CargoFromDB { get; set; }

        internal Entity CargoToEntity { get; set; }
        [JsonIgnore]
        internal VolumeStorageDB CargoToDB { get; set; }

        internal List<(ICargoable item, long amount)> OrderedToTransfer { get; set; }
        internal List<(ICargoable item, long amount)> ItemsLeftToTransfer;

        internal double DistanceBetweenEntitys { get; set; }
        internal int TransferRateInKG { get; set; } = 1000;

        /// <summary>
        /// Threadsafe gets items left to transfer. don't call this every ui frame!
        /// (or you could cause deadlock slowdowns with the processing)
        /// </summary>
        /// <returns></returns>
        public List<(ICargoable item, long unitCount)> GetItemsToTransfer()
        {
            ICollection ic = ItemsLeftToTransfer;
            lock (ic.SyncRoot) 
            {
                return new List<(ICargoable item, long unitCount)>(ItemsLeftToTransfer);
            }
        }

        public CargoTransferDB()
        {
        }


        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
    
    public class StorageTransferRateAtbDB : IComponentDesignAttribute
    {
        /// <summary>
        /// Gets or sets the transfer rate.
        /// </summary>
        /// <value>The transfer rate in Kg/h</value>
        public int TransferRate_kgh { get; internal set; }
        /// <summary>
        /// Gets or sets the transfer range.
        /// </summary>
        /// <value>DeltaV in m/s, Low Earth Orbit is about 10000m/s</value>
        public double TransferRange_ms { get; internal set; }

        public StorageTransferRateAtbDB(int rate_kgh, double rangeDV_ms)
        {
            TransferRate_kgh = rate_kgh;
            TransferRange_ms = rangeDV_ms;
        }

        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<VolumeStorageDB>())
            {
                var newdb = new VolumeStorageDB();
                parentEntity.SetDataBlob(newdb);
            }
            StorageSpaceProcessor.RecalcVolumeCapacityAndRates(parentEntity);
        }
    }


}

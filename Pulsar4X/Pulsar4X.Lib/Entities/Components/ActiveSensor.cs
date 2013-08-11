﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Pulsar4X.Entities.Components
{
    public class ActiveSensorDefTN : ComponentDefTN
    {

        /// <summary>
        /// Sensor Strength component of range.
        /// </summary>
        private byte ActiveStrength;
        public byte activeStrength
        {
            get { return ActiveStrength; }
        }

        /// <summary>
        /// EM listening portion of sensor.
        /// </summary>
        private byte EMRecv;
        public byte eMRecv
        {
            get { return EMRecv; }
        }

        /// <summary>
        /// Sensor wavelength resolution. What size of ship this sensor is best suited to detecting.
        /// </summary>
        private ushort Resolution;
        public ushort resolution
        {
            get { return Resolution; }
        }

        /// <summary>
        /// Grav Pulse Signature/Strength
        /// </summary>
        private int GPS;
        public int gps
        {
            get { return GPS; }
        }
        
        /// <summary>
        /// Range at which sensor can detect craft of same HS as resolution.
        /// </summary>
        private int MaxRange;
        public int maxRange
        {
            get { return MaxRange; }
        }

        /// <summary>
        /// Lookup table for ship resolutions
        /// </summary>
        private BindingList<int> LookUpST;
        public BindingList<int> lookUpST
        {
            get { return LookUpST; }
        }

        /// <summary>
        /// Lookup table for missile resolutions
        /// </summary>
        private BindingList<int> LookUpMT;
        public BindingList<int> lookUpMT
        {
            get { return LookUpMT; }
        }

        /// <summary>
        /// Is this a Missile fire control or an active sensor? false = sensor, true = MFC. MFCs have 3x the range of search sensors.
        /// </summary>
        private bool IsMFC;
        public bool isMFC
        {
            get { return IsMFC; }
        }

        /// <summary>
        /// Likelyhood of destruction from electronic(microwave) damage.
        /// </summary>
        private float Hardening;
        public float hardening
        {
            get { return Hardening; }
        }

        /// <summary>
        /// ActiveSensorDefTN builds a sensor definition based on the following parameters.
        /// </summary>
        /// <param name="desc">Name of the sensor that will be displayed to the player.</param>
        /// <param name="HS">Size in HS.</param>
        /// <param name="actStr">Active Strength of the sensor.</param>
        /// <param name="EMR">EM Listening portion of the sensor.</param>
        /// <param name="Res">Resolution of the sensor, what size of target is being searched for.</param>
        /// <param name="MFC">Is this sensor a search sensor, or a Missile Fire Control?</param>
        /// <param name="hard">Percent chance of destruction due to electronic damage.</param>
        /// <param name="hardTech">Level of electronic hardening tech. Adjusted downwards by 1, so that level 0 is level 1, and so on.</param>
        public ActiveSensorDefTN(string desc, float HS, byte actStr, byte EMR, ushort Res, bool MFC, float hard, byte hardTech)
        {
            Id = Guid.NewGuid();

            if (MFC == false)
                componentType = ComponentTypeTN.ActiveSensor;
            else if (MFC == true)
                componentType = ComponentTypeTN.MissileFireControl;

            /// <summary>
            /// basic sensor statistics.
            /// </summary>
            Name = desc;
            size = HS;
            ActiveStrength = actStr;
            EMRecv = EMR;
            Resolution = Res;
            IsMFC = MFC;
            Hardening = hard;

            /// <summary>
            /// Crew and cost are related to size, ActiveStrength, and hardening.
            /// </summary>
            crew = (byte)(size * 2.0);
            cost = (decimal)((size * (float)ActiveStrength) + ((size * (float)ActiveStrength) * 0.25f * (float)(hardTech - 1)));


            ///<summary>
            ///Small sensors are civilian, large are military.
            ///</summary>
            if (size <= 1.0)
                isMilitary = false;
            else
                isMilitary = true;

            ///<summary>
            ///HTK is either 1 or 0, because all sensors are very weak to damage, especially electronic damage.
            ///</summary>
            if (size >= 1.0)
                htk = 1;
            else
                htk = 0;

            ///<summary>
            ///GPS is the value that a ship's EM signature will be increased by when this sensor is active.
            ///</summary>
            GPS = (int)((float)ActiveStrength * size * (float)Resolution);

            MaxRange = (int)((float)ActiveStrength * size * (float)Math.Sqrt((double)Resolution) * (float)EMRecv * 10000.0f);

            if (IsMFC == true)
            {
                MaxRange = MaxRange * 3;
                GPS = 0;
            }

            LookUpST = new BindingList<int>();
            LookUpMT = new BindingList<int>();

            ///<summary>
            ///Initialize the ship lookup Table.
            ///</summary>
            for (int loop = 0; loop < Constants.ShipTN.ResolutionMax; loop++)
            {
                ///<summary>
                ///Sensor Resolution can't resolve this target at its MaxRange due to the target's smaller size
                ///</summary>
                if ((loop + 1) < Resolution)
                {
                    int NewRange = (int)((float)MaxRange * (float)Math.Pow(((double)(loop + 1) / (float)Resolution), 2.0f));
                    LookUpST.Add(NewRange);
                }
                else if ((loop + 1) >= Resolution)
                {
                    LookUpST.Add(MaxRange);
                }
            }

            ///<summary>
            ///Initialize the missile lookup Table.
            ///Missile size is in MSP, and no missile may be a fractional MSP in size. Each MSP is 0.05 HS in size.
            ///</summary>
            for (int loop = 0; loop < 15; loop++)
            {
                ///<summary>
                ///Missile size never drops below 0.33, and missiles above 1 HS are atleast 1 HS. if I have to deal with 2HS missiles I can go to LookUpST
                ///</summary>
                if (loop == 0)
                {
                    int NewRange = (int)((float)MaxRange * (float)Math.Pow( ( 0.33 / (float)Resolution ),2.0f));
                    LookUpMT.Add(NewRange);
                }
                else if( loop != 14 )
                {
                    float msp = ((float)loop + 6.0f) * 0.05f;
                    int NewRange = (int)((float)MaxRange * Math.Pow( ( msp / (float)Resolution ),2.0f ));
                    LookUpMT.Add(NewRange);
                }
                else if( loop == 14 )
                {
                    lookUpMT.Add(LookUpST[0]);//size 1 is size 1
                }
            }

            isSalvaged = false;
            isObsolete = false;
            isDivisible = false;

            isElectronic = true;
        }
        ///<summary>
        ///End ActiveSensorDefTN()
        ///</summary>
        
        /// <summary>
        /// Missile active sensor definition
        /// </summary>
        /// <param name="ActiveStr">Active strength value in total.</param>
        /// <param name="EMS">EM sensitivity same as regular active sensors.</param>
        /// <param name="Resolution">Active sensor resolution.</param>
        public ActiveSensorDefTN(float ActiveStr, byte EMS, int Resolution)
        {
            Id = Guid.NewGuid();
            componentType = ComponentTypeTN.TypeCount;
            EMRecv = EMS;

            Name = "MissileActive";

            GPS = (int)(ActiveStrength * Resolution);

            MaxRange = (int)((float)ActiveStr * EMS * (float)Math.Sqrt((double)Resolution) * 10000.0f);

            LookUpST = new BindingList<int>();
            LookUpMT = new BindingList<int>();

            ///<summary>
            ///Initialize the ship lookup Table.
            ///</summary>
            for (int loop = 0; loop < Constants.ShipTN.ResolutionMax; loop++)
            {
                ///<summary>
                ///Sensor Resolution can't resolve this target at its MaxRange due to the target's smaller size
                ///</summary>
                if ((loop + 1) < Resolution)
                {
                    int NewRange = (int)((float)MaxRange * (float)Math.Pow(((double)(loop + 1) / (float)Resolution), 2.0f));
                    LookUpST.Add(NewRange);
                }
                else if ((loop + 1) >= Resolution)
                {
                    LookUpST.Add(MaxRange);
                }
            }

            ///<summary>
            ///Initialize the missile lookup Table.
            ///Missile size is in MSP, and no missile may be a fractional MSP in size. Each MSP is 0.05 HS in size.
            ///</summary>
            for (int loop = 0; loop < 15; loop++)
            {
                ///<summary>
                ///Missile size never drops below 0.33, and missiles above 1 HS are atleast 1 HS. if I have to deal with 2HS missiles I can go to LookUpST
                ///</summary>
                if (loop == 0)
                {
                    int NewRange = (int)((float)MaxRange * (float)Math.Pow((0.33 / (float)Resolution), 2.0f));
                    LookUpMT.Add(NewRange);
                }
                else if (loop != 14)
                {
                    float msp = ((float)loop + 6.0f) * 0.05f;
                    int NewRange = (int)((float)MaxRange * Math.Pow((msp / (float)Resolution), 2.0f));
                    LookUpMT.Add(NewRange);
                }
                else if (loop == 14)
                {
                    lookUpMT.Add(LookUpST[0]);//size 1 is size 1
                }
            }


            ActiveStrength = 0;
            Hardening = 0;
            IsMFC = false;
            crew = 0;
            cost = 0;
            htk = 0;
            size = 0;
            isMilitary = false;
            isObsolete = false;
            isSalvaged = false;
            isDivisible = false;
            isElectronic = false;
        }

        /// <summary>
        /// GetActiveDetectionRange returns the range of either the ship or missile
        /// <param name="TCS">TCS is the ship total cross section. I want the function to return at what range this ship is detected at.</param>
        /// <param name="MSP">MSP is Missile Size Point. How big of a missile am I trying to find with this function.</param>
        /// <returns>Range at which the missile or ship is detected.</returns>
        /// </summary>
        public int GetActiveDetectionRange(int TCS, int MSP)
        {
            ///<summary>
            ///limits of the arrays
            ///</summary>
            if ( (TCS > (Constants.ShipTN.ResolutionMax - 1) || TCS < 0) || (MSP > 14 || MSP < -1 ) )
            {
                return -1;
            }


            int DetRange;
            if (MSP == -1)
            {
                DetRange = LookUpST[TCS];
            }
            else
            {
                DetRange = LookUpMT[MSP];
            }
            return DetRange;
        }
        ///<summary>
        ///End GetActiveDetectionRange
        ///</summary>

    }
    /// <summary>
    /// End Class ActiveSensorDefTN
    /// </summary>

    /// <summary>
    /// Active sensor component definition.
    /// </summary>
    public class ActiveSensorTN : ComponentTN
    {
        /// <summary>
        /// What statistics define this sensor?
        /// </summary>
        private ActiveSensorDefTN ASensorDef;
        public ActiveSensorDefTN aSensorDef
        {
            get { return ASensorDef; }
        }

        /// <summary>
        /// Is this sensor active and thus both searching, and emitting an EM signature?
        /// </summary>
        private bool IsActive;
        public bool isActive
        {
            get { return IsActive; }
            set { IsActive = value; }
        }


        /// <summary>
        /// The active sensor component itself. It is initialized to not destroyed, and not active.
        /// </summary>
        /// <param name="define">Definition for the sensor.</param>
        public ActiveSensorTN(ActiveSensorDefTN define)
        {
            ASensorDef = define;
            isDestroyed = false;
            IsActive = false;
        }
    }
    /// <summary>
    ///End ActiveSensorTN
    /// </summary>
    

    public class MissileFireControlTN : ComponentTN
    {
        /// <summary>
        /// Missile Fire Controls are basically active sensors, this definition has the MFC range data for this sensor.
        /// </summary>
        private ActiveSensorDefTN MFCSensorDef;
        public ActiveSensorDefTN mFCSensorDef
        {
            get { return MFCSensorDef; }
        }

        /// <summary>
        /// ECCMs Linked to this BFC. Update DestroyComponents when eccm is added.
        /// </summary>

        /// <summary>
        /// Weapons linked to this BFC.
        /// </summary>
        private BindingList<MissileLauncherTN> LinkedWeapons;
        public BindingList<MissileLauncherTN> linkedWeapons
        {
            get { return LinkedWeapons; }
        }

        /// <summary>
        /// Target Assigned to this MFC
        /// </summary>
        private ShipTN Target;
        public ShipTN target
        {
            get { return Target; }
        }

        /// <summary>
        /// Whether this FC is authorized to fire on its target.
        /// </summary>
        private bool OpenFire;
        public bool openFire
        {
            get { return OpenFire; }
            set { OpenFire = value; }
        }

        /// <summary>
        /// Point defense state this BFC will fire to.
        /// </summary>
        private PointDefenseState PDState;
        public PointDefenseState pDState
        {
            get { return PDState; }
        }


        /// <summary>
        /// Ordnance in fly but still connected to this MFC.
        /// </summary>
        private BindingList<OrdnanceTN> MissilesInFlight;
        public BindingList<OrdnanceTN> missilesInFlight
        {
            get { return MissilesInFlight; }
        }

        /// <summary>
        /// ECCM needs to be handled eventually
        /// </summary>

        /// <summary>
        /// Constructor for Missile Fire Controls
        /// </summary>
        /// <param name="MFCDef">MFC definition.</param>
        public MissileFireControlTN(ActiveSensorDefTN MFCDef)
        {
            MFCSensorDef = MFCDef;
            isDestroyed = false;

            LinkedWeapons = new BindingList<MissileLauncherTN>();

            MissilesInFlight = new BindingList<OrdnanceTN>();

            OpenFire = false;
            Target = null;
            PDState = PointDefenseState.None;
        }

        /// <summary>
        /// Set the fire control to the desired point defense state.
        /// </summary>
        /// <param name="State">State the MFC is to be set to.</param>
        public void SetPointDefenseMode(PointDefenseState State)
        {
            PDState = State;
        }

        /// <summary>
        /// Simple assignment of a ship as a target to this mfc.
        /// </summary>
        /// <param name="ShipTarget">Ship to be targeted.</param>
        public void assignTarget(ShipTN ShipTarget)
        {
            Target = ShipTarget;
        }

        /// <summary>
        /// Simple launch tube assignment.
        /// </summary>
        /// <param name="tube">launch tube to be assigned.</param>
        public void assignLaunchTube(MissileLauncherTN tube)
        {
            LinkedWeapons.Add(tube);

            if (tube.mFC != this)
                tube.AssignMFC(this);
        }

        /// <summary>
        /// Launch tube removal
        /// </summary>
        /// <param name="tube">tube to be removed.</param>
        public void removeLaunchTube(MissileLauncherTN tube)
        {
            LinkedWeapons.Remove(tube);

            if(tube.mFC == this)
               tube.ClearMFC();
            
        }

        /// <summary>
        /// Clears all assigned launch tubes.
        /// </summary>
        public void ClearAllWeapons()
        {
            for (int loop = 0; loop < LinkedWeapons.Count; loop++)
            {
                LinkedWeapons[loop].ClearMFC();
            }
            LinkedWeapons.Clear();
        }

        /// <summary>
        /// If an MFC is destroyed set all missiles to have no MFC.
        /// </summary>
        public void ClearAllMissiles()
        {
            for (int loop = 0; loop < MissilesInFlight.Count; loop++)
            {
                MissilesInFlight[loop].mFC = null;
            }
        }

        /// <summary>
        /// Simple deassignment of target to this bfc.
        /// </summary>
        public void clearTarget()
        {
            Target = null;
        }

        /// <summary>
        /// Simple return of the target of this BFC.
        /// </summary>
        public ShipTN getTarget()
        {
            return Target;
        }

        public bool FireWeapons()
        {
            // stub for now
            return false;
        }
    }

}

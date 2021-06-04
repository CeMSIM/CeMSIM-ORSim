/* Distributed under the Apache License, Version 2.0.
   See accompanying NOTICE file for details.*/



namespace Pulse.CDM
{
    #region Enum | Actions
    public enum PulseAction : int
    {
        StartCardiacArrest,
        StopCardiacArrest,
        StartHemorrhage,
        StopHemorrhage,
        StartIVBag,
        StopIVBag,
        InjectMorphine,
        TensionPneumothorax,
        EndTensionPneumothorax,
        //ChestOcclusiveDresing,
        NeedleDecompressions,
        StartAirwayObstruction,
        StopAirwayObstruction,
        StartIntubation,
        StopIntubation,
        VentilateIntubation,
        VentilateMask
    }
    #endregion

    #region Enum | Drugs
    public enum Drug : int
    {
        StartSuccinylcholineInfusion,
        StopSuccinylcholineInfusion,
        StartEpinephrineInfusion,
        StopEpinephrineInfusion,
        StartPropofolInfusion,
        StopPropofolInfusion,
        StartRocuroniumInfusion,
        StopRocuroniumInfusion
    }
    #endregion

    #region Class | External Hemorrhage Component
    public class ExternalHemorrhageCmpt
    {
        public static string RightArm { get { return "RightArm"; } }
        public static string LeftArm { get { return "LeftArm"; } }
        public static string RightLeg { get { return "RightLeg"; } }
        public static string LeftLeg { get { return "LeftLeg"; } }

        public static string LeftLung { get { return "LeftLung"; } }
        public static string RightLung { get { return "RightLung"; } }

        public static string Brain { get { return "Brain"; } }
        public static string Aorta { get { return "Aorta"; } }
        public static string VenaCava { get { return "VenaCava"; } }

        public static string RightKidney { get { return "RightKidney"; } }
        public static string LeftKidney { get { return "LeftKidney"; } }
        public static string Liver { get { return "Liver"; } }
        public static string Spleen { get { return "Spleen"; } }
        public static string Splanchnic { get { return "Splanchnic"; } }
        public static string SmallIntestine { get { return "SmallIntestine"; } }
        public static string LargeIntestine { get { return "LargeIntestine"; } }
    }
    #endregion

    #region Class | Internal Hemorrhage Component
    public class InternalHemorrhageCmpt
    {
        public static string RightKidney { get { return "RightKidney"; } }
        public static string LeftKidney { get { return "LeftKidney"; } }
        public static string Liver { get { return "Liver"; } }
        public static string Spleen { get { return "Spleen"; } }
        public static string Splanchnic { get { return "Splanchnic"; } }
        public static string SmallIntestine { get { return "SmallIntestine"; } }
        public static string LargeIntestine { get { return "LargeIntestine"; } }

        public static string Aorta { get { return "Aorta"; } }
        public static string VenaCava { get { return "VenaCava"; } }
    }
    #endregion

    #region Class | Substance
    public class Substance
    {
        public static string Morphine { get { return "Morphine"; } }
        public static string Succinylcholine { get { return "Succinylcholine"; } }

        public static string Propofol { get { return "Propofol"; } }

        public static string Rocuronium { get { return "Rocuronium"; } }

        public static string Epinephrine { get { return "Epinephrine"; } }
    }
    #endregion

    #region Class | Compound
    public class Compound
    {
        public static string Blood { get { return "Blood"; } }
        public static string Saline { get { return "Saline"; } }
        public static string PackedRBC { get { return "PackedRBC"; } }
    }
    #endregion

    public class PulseActionManager : PulseEngineController
    {
        #region Variables
        [UnityEngine.HideInInspector]
        public PulseAction action;
        //[UnityEngine.HideInInspector]
        public float pneumothoraxSeverity;
        [UnityEngine.HideInInspector]
        public Drug drug;
        #endregion

        #region Start
        private void Start()
        {
            driver = this.GetComponent<PulseEngineDriver>();
        }
        #endregion

        #region Events | Subscription
        private void OnEnable()
        {
            PulseEventManager.triggerAction += RunAction;
            PulseEventManager.administerDrug += AdministerDrug;
        }

        private void OnDisable()
        {
            PulseEventManager.triggerAction -= RunAction;
            PulseEventManager.administerDrug -= AdministerDrug;
        }
        #endregion

        #region Functions | Run Action
        public void RunAction()
        {
            switch (action)
            {
                #region Cardiac Arrest
                case PulseAction.StartCardiacArrest:
                    {
                        SECardiacArrest ca = new SECardiacArrest();
                        ca.SetState(eSwitch.On);
                        driver.engine.ProcessAction(ca);
                        break;
                    }
                case PulseAction.StopCardiacArrest:
                    {
                        SECardiacArrest ca = new SECardiacArrest();
                        ca.SetState(eSwitch.Off);
                        break;
                    }
                #endregion

                #region Hemorrhage
                case PulseAction.StartHemorrhage:
                    {
                        SEHemorrhage h = new SEHemorrhage();
                        h.SetCompartment(ExternalHemorrhageCmpt.RightLeg);
                        h.SetType(SEHemorrhage.eType.External);
                        h.GetRate().SetValue(50, VolumePerTimeUnit.mL_Per_min);
                        driver.engine.ProcessAction(h);
                        break;
                    }
                case PulseAction.StopHemorrhage:
                    {
                        SEHemorrhage h = new SEHemorrhage();
                        h.SetCompartment(ExternalHemorrhageCmpt.RightLeg);
                        h.SetType(SEHemorrhage.eType.External);
                        h.GetRate().SetValue(0, VolumePerTimeUnit.mL_Per_min);
                        driver.engine.ProcessAction(h);
                        break;
                    }
                #endregion

                #region IV
                case PulseAction.StartIVBag:
                    {
                        SESubstanceCompoundInfusion sci = new SESubstanceCompoundInfusion();
                        sci.SetSubstanceCompound(Compound.Saline);
                        sci.GetBagVolume().SetValue(500, VolumeUnit.mL);
                        sci.GetRate().SetValue(75, VolumePerTimeUnit.mL_Per_s);
                        driver.engine.ProcessAction(sci);
                        break;
                    }
                case PulseAction.StopIVBag:
                    {
                        SESubstanceCompoundInfusion sci = new SESubstanceCompoundInfusion();
                        sci.SetSubstanceCompound(Compound.Saline);
                        sci.GetBagVolume().SetValue(0, VolumeUnit.mL);
                        sci.GetRate().SetValue(0, VolumePerTimeUnit.mL_Per_s);
                        driver.engine.ProcessAction(sci);
                        break;
                    }
                #endregion

                #region Morphine
                case PulseAction.InjectMorphine:
                    {
                        SESubstanceBolus bo = new SESubstanceBolus();
                        bo.SetSubstance(Substance.Morphine);
                        bo.GetConcentration().SetValue(100, MassPerVolumeUnit.mg_Per_mL);
                        bo.GetDose().SetValue(1, VolumeUnit.mL);
                        bo.SetAdminRoute(SESubstanceBolus.eAdministration.Intravenous);
                        driver.engine.ProcessAction(bo);
                        break;
                    }
                #endregion

                #region Pneumothorax
                case PulseAction.TensionPneumothorax:
                    {
                        SETensionPneumothorax tp = new SETensionPneumothorax();
                        tp.SetSide(eSide.Left);
                        tp.SetType(eGate.Open);
                        tp.GetSeverity().SetValue(pneumothoraxSeverity);
                        driver.engine.ProcessAction(tp);
                        break;
                    }
                case PulseAction.EndTensionPneumothorax:
                    {
                        SETensionPneumothorax tp = new SETensionPneumothorax();
                        tp.SetSide(eSide.Left);
                        tp.SetType(eGate.Open);
                        tp.GetSeverity().SetValue(0);
                        driver.engine.ProcessAction(tp);
                        break;
                    }
                #endregion

                #region Chest Dressing
                //   case PulseAction.ChestOcclusiveDresing:
                //       {
                //           
                //           SEChestOcclusiveDressing od = new SEChestOcclusiveDressing();
                //           od.SetSide(eSide.Left);
                //           od.SetState(eSwitch.On);
                //           driver.engine.ProcessAction(od);
                //           break;
                //
                //       }
                #endregion

                #region Needle Decompression
                case PulseAction.NeedleDecompressions:
                    {
                        SENeedleDecompression nd = new SENeedleDecompression();
                        nd.SetSide(eSide.Left);
                        nd.SetState(eSwitch.On);
                        driver.engine.ProcessAction(nd);
                        break;
                    }
                #endregion

                #region Airway Obstruction
                case PulseAction.StartAirwayObstruction:
                    {
                        SEAirwayObstruction ao = new SEAirwayObstruction();
                        ao.GetSeverity().SetValue(0.7);
                        driver.engine.ProcessAction(ao);
                        break;
                    }
                case PulseAction.StopAirwayObstruction:
                    {
                        SEAirwayObstruction ao = new SEAirwayObstruction();
                        ao.GetSeverity().SetValue(0.0);
                        driver.engine.ProcessAction(ao);
                        break;
                    }
                #endregion

                #region Intubation
                case PulseAction.StartIntubation:
                    {
                        SEIntubation tub = new SEIntubation();
                        tub.SetType(SEIntubation.eType.Tracheal);
                        driver.engine.ProcessAction(tub);
                        break;
                    }
                case PulseAction.StopIntubation:
                    {
                        SEIntubation tub = new SEIntubation();
                        tub.SetType(SEIntubation.eType.Off);
                        driver.engine.ProcessAction(tub);
                        break;
                    }
                #endregion

                #region Ventilation
                case PulseAction.VentilateIntubation:
                    {
                        SEAnesthesiaMachineConfiguration am = new SEAnesthesiaMachineConfiguration();
                        am.GetConfiguration().SetConnection(SEAnesthesiaMachine.Connection.Tube);
                        am.GetConfiguration().GetInletFlow().SetValue(5, VolumePerTimeUnit.L_Per_min);
                        am.GetConfiguration().GetInspiratoryExpiratoryRatio().SetValue(0.5);
                        am.GetConfiguration().GetOxygenFraction().SetValue(0.23);
                        am.GetConfiguration().SetOxygenSource(SEAnesthesiaMachine.OxygenSource.Wall);
                        am.GetConfiguration().GetPositiveEndExpiredPressure().SetValue(1, PressureUnit.cmH2O);
                        am.GetConfiguration().SetPrimaryGas(SEAnesthesiaMachine.PrimaryGas.Nitrogen);
                        am.GetConfiguration().GetRespiratoryRate().SetValue(16, FrequencyUnit.Per_min);
                       // am.GetConfiguration().GetPeakInspiratoryPressure().SetValue(10.5, PressureUnit.cmH2O);
                        driver.engine.ProcessAction(am);
                        break;
                    }
                
                case PulseAction.VentilateMask:
                    {
                        SEAnesthesiaMachineConfiguration am = new SEAnesthesiaMachineConfiguration();
                        am.GetConfiguration().SetConnection(SEAnesthesiaMachine.Connection.Mask);
                        am.GetConfiguration().GetInletFlow().SetValue(5, VolumePerTimeUnit.L_Per_min);
                        am.GetConfiguration().GetInspiratoryExpiratoryRatio().SetValue(0.5);
                        am.GetConfiguration().GetOxygenFraction().SetValue(0.23);
                        am.GetConfiguration().SetOxygenSource(SEAnesthesiaMachine.OxygenSource.Wall);
                        am.GetConfiguration().GetPositiveEndExpiredPressure().SetValue(1, PressureUnit.cmH2O);
                        am.GetConfiguration().SetPrimaryGas(SEAnesthesiaMachine.PrimaryGas.Nitrogen);
                        am.GetConfiguration().GetRespiratoryRate().SetValue(16, FrequencyUnit.Per_min);
                      //  am.GetConfiguration().GetPeakInspiratoryPressure().SetValue(10.5, PressureUnit.cmH2O);
                        driver.engine.ProcessAction(am);
                        break;
                    }
                    #endregion
            }
        }
        #endregion

        #region Functions | Administer Drug
        public void AdministerDrug()
        {
            switch(drug)
            {
                #region Drugs | Succinylcholine
                case Drug.StartSuccinylcholineInfusion:
                    {
                        SESubstanceInfusion si = new SESubstanceInfusion();
                        si.SetSubstance(Substance.Succinylcholine);
                        si.GetConcentration().SetValue(5000, MassPerVolumeUnit.ug_Per_mL);
                        si.GetRate().SetValue(100, VolumePerTimeUnit.mL_Per_min);
                        driver.engine.ProcessAction(si);
                        break;

                        //SESubstanceBolus bo = new SESubstanceBolus();
                        //bo.SetSubstance(Substance.Succinylcholine);
                        //bo.GetConcentration().SetValue(1, MassPerVolumeUnit.mg_Per_L);
                        //bo.GetDose().SetValue(1, VolumeUnit.mL);
                        //bo.SetAdminRoute(SESubstanceBolus.eAdministration.Intravenous);
                        //driver.engine.ProcessAction(bo);
                        //break;
                    }
                case Drug.StopSuccinylcholineInfusion:
                    {
                        SESubstanceInfusion si = new SESubstanceInfusion();
                        si.SetSubstance(Substance.Succinylcholine);
                        si.GetRate().SetValue(0, VolumePerTimeUnit.mL_Per_min);
                        driver.engine.ProcessAction(si);
                        break;
                    }
                #endregion
               
                #region Drugs | Epinephrine
                    //TODO: update amounts to match drive doc
                case Drug.StartEpinephrineInfusion:
                    {
                       //SESubstanceInfusion si = new SESubstanceInfusion();
                       //si.SetSubstance(Substance.Epinephrine);
                       //si.GetConcentration().SetValue(5f, MassPerVolumeUnit.ug_Per_mL);
                       //si.GetRate().SetValue(100, VolumePerTimeUnit.mL_Per_min);
                       //driver.engine.ProcessAction(si);
                       //break;

                        SESubstanceBolus bo = new SESubstanceBolus();
                        bo.SetSubstance(Substance.Epinephrine);
                        bo.GetConcentration().SetValue(1, MassPerVolumeUnit.ug_Per_mL);
                        bo.GetDose().SetValue(3, VolumeUnit.mL);
                        bo.SetAdminRoute(SESubstanceBolus.eAdministration.Intravenous);
                        driver.engine.ProcessAction(bo);
                        break;
                    }
                case Drug.StopEpinephrineInfusion:
                    {
                        SESubstanceInfusion si = new SESubstanceInfusion();
                        si.SetSubstance(Substance.Epinephrine);
                        si.GetRate().SetValue(0, VolumePerTimeUnit.mL_Per_min);
                        driver.engine.ProcessAction(si);
                        break;
                    }
                #endregion

                #region Drugs | Propofol
                //TODO: update amounts to match drive doc
                case Drug.StartPropofolInfusion:
                    {
                        //SESubstanceInfusion si = new SESubstanceInfusion();
                        //si.SetSubstance(Substance.Propofol);
                        //si.GetConcentration().SetValue(7050, MassPerVolumeUnit.ug_Per_mL);
                        //si.GetRate().SetValue(100, VolumePerTimeUnit.mL_Per_min);
                        //driver.engine.ProcessAction(si);
                        //break;

                        SESubstanceBolus bo = new SESubstanceBolus();
                        bo.SetSubstance(Substance.Propofol);
                        bo.GetConcentration().SetValue(10, MassPerVolumeUnit.mg_Per_mL);
                        bo.GetDose().SetValue(1, VolumeUnit.mL);
                        bo.SetAdminRoute(SESubstanceBolus.eAdministration.Intravenous);
                        driver.engine.ProcessAction(bo);
                        break;
                    }
                case Drug.StopPropofolInfusion:
                    {
                        SESubstanceInfusion si = new SESubstanceInfusion();
                        si.SetSubstance(Substance.Propofol);
                        si.GetRate().SetValue(0, VolumePerTimeUnit.mL_Per_min);
                        driver.engine.ProcessAction(si);
                        break;
                    }
                #endregion

                #region Drugs | Rocuronium
                //TODO: update amounts to match drive doc
                case Drug.StartRocuroniumInfusion:
                    {
                        SESubstanceInfusion si = new SESubstanceInfusion();
                        si.SetSubstance(Substance.Rocuronium);
                        si.GetConcentration().SetValue(2700, MassPerVolumeUnit.ug_Per_mL);
                        si.GetRate().SetValue(100, VolumePerTimeUnit.mL_Per_min);
                        driver.engine.ProcessAction(si);
                        break;
                    }
                case Drug.StopRocuroniumInfusion:
                    {
                        SESubstanceInfusion si = new SESubstanceInfusion();
                        si.SetSubstance(Substance.Rocuronium);
                        si.GetRate().SetValue(0, VolumePerTimeUnit.mL_Per_min);
                        driver.engine.ProcessAction(si);
                        break;
                    }
                    #endregion

            }
        }
        #endregion
    }
}
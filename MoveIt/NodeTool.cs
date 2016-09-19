using ColossalFramework;
using ColossalFramework.Globalization;
using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;


namespace FineRoadTool
{
    public class NodeTool : DefaultTool
    {
        public NetNode.Flags flag = NetNode.Flags.All;

        protected override void OnToolGUI(Event e)
        {
            bool isInsideUI = this.m_toolController.IsInsideUI;
            if (e.type == EventType.MouseDown)
            {
                if (!isInsideUI && e.button == 0)
                {
                    InstanceID hoverInstance = this.m_hoverInstance;
                    InstanceID hoverInstance2 = this.m_hoverInstance2;
                    if (this.m_selectErrors == ToolBase.ToolErrors.None)
                    {

                    }
                }
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                // Cancel Tool
            }
        }

        protected override void OnEnable()
        {
            if(m_toolController == null)
                m_toolController = GameObject.FindObjectOfType<ToolController>();
            base.OnEnable();
        }

        protected override bool EnableMouseLight()
        {
            return false;
        }

        public override bool GetTerrainIgnore()
        {
            return true;
        }

        public override NetNode.Flags GetNodeIgnoreFlags()
        {
            return flag;
        }

        public override NetSegment.Flags GetSegmentIgnoreFlags()
        {
            return NetSegment.Flags.All;
        }

        public override Building.Flags GetBuildingIgnoreFlags()
        {
            return Building.Flags.All;
        }

        public override global::TreeInstance.Flags GetTreeIgnoreFlags()
        {
            return global::TreeInstance.Flags.All;
        }

        public override PropInstance.Flags GetPropIgnoreFlags()
        {
            return PropInstance.Flags.All;
        }

        public override Vehicle.Flags GetVehicleIgnoreFlags()
        {
            return Vehicle.Flags.All;
        }

        public override VehicleParked.Flags GetParkedVehicleIgnoreFlags()
        {
            return VehicleParked.Flags.All;
        }

        public override CitizenInstance.Flags GetCitizenIgnoreFlags()
        {
            return CitizenInstance.Flags.All;
        }

        public override TransportLine.Flags GetTransportIgnoreFlags()
        {
            return TransportLine.Flags.All;
        }

        public override District.Flags GetDistrictIgnoreFlags()
        {
            return District.Flags.All;
        }
    }
}

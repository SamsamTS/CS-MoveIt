using ColossalFramework.Math;
using ColossalFramework.UI;
using ColossalFramework;
using System;
using UnityEngine;

namespace MoveIt
{
    public partial class MoveItTool : ToolBase
    {
        private void OnLeftMouseDown()
        {
            DebugUtils.Log("OnLeftMouseDown: " + ToolState);

            if (ToolState == ToolStates.Default)
            {
                if (marqueeSelection && (m_hoverInstance == null || !Action.selection.Contains(m_hoverInstance)))
                {
                    m_selection = default;
                    m_marqueeInstances = null;

                    ToolState = ToolStates.DrawingSelection;
                }

                m_lastInstance = m_hoverInstance;

                m_sensitivityTogglePosAbs = m_clickPositionAbs = RaycastMouseLocation();
                m_sensitivityDistanceOffset = Vector3.zero;
            }
            else if (ToolState == ToolStates.MouseDragging)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                m_dragStartRelative = action.moveDelta;
                UpdateSensitivityMode();

                m_sensitivityTogglePosAbs = m_clickPositionAbs = RaycastMouseLocation();
                m_sensitivityDistanceOffset = Vector3.zero;
            }
            else if (ToolState == ToolStates.Cloning)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;
                action.followTerrain = followTerrain;

                if (!POProcessing)
                {
                    ToolState = ToolStates.Default;
                    m_nextAction = ToolAction.Do;

                    UpdateSensitivityMode();
                    m_sensitivityDistanceOffset = Vector3.zero;
                }
            }
        }

        private void OnRightMouseDown()
        {
            DebugUtils.Log("OnRightMouseDown: " + ToolState);

            if (ToolState == ToolStates.Default)
            {
                m_mouseStartX = Input.mousePosition.x;
            }
            else if (ToolState == ToolStates.MouseDragging)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                m_startAngle = action.angleDelta;

                m_mouseStartX = Input.mousePosition.x;
                m_sensitivityAngleOffset = 0f;
            }
            else if (ToolState == ToolStates.Cloning)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;
                m_startAngle = action.angleDelta;

                m_mouseStartX = Input.mousePosition.x;
                m_sensitivityAngleOffset = 0f;
            }
        }

        private void OnLeftMouseUp()
        {
            DebugUtils.Log("OnLeftMouseUp: " + ToolState);

            if (ToolState == ToolStates.DrawingSelection)
            {
                ToolState = ToolStates.Default;

                Event e = Event.current;

                if (m_marqueeInstances == null || m_marqueeInstances.Count == 0 ||
                    (e.alt && !Action.selection.Overlaps(m_marqueeInstances)) ||
                    (e.shift && Action.selection.IsSupersetOf(m_marqueeInstances))
                    ) return;

                if (!(ActionQueue.instance.current is SelectAction action))
                {
                    action = new SelectAction(e.shift);
                    ActionQueue.instance.Push(action);
                }
                else
                {
                    ActionQueue.instance.Invalidate();
                }

                if (e.alt)
                {
                    Action.selection.ExceptWith(m_marqueeInstances);
                    //PO.SelectionRemove(m_marqueeInstances);
                    m_debugPanel.UpdatePanel();
                }
                else
                {
                    if (!e.shift)
                    {
                        Action.selection.Clear();
                    }
                    Action.selection.UnionWith(m_marqueeInstances);
                    //PO.SelectionAdd(m_marqueeInstances);
                    m_debugPanel.UpdatePanel();
                }

                m_marqueeInstances = null;
            }
        }

        private void OnRightMouseUp()
        { }

        private void OnLeftClick()
        {
            DebugUtils.Log("OnLeftClick: " + ToolState);

            if (ToolState == ToolStates.Default || ToolState == ToolStates.DrawingSelection)
            {
                Event e = Event.current;
                if (m_hoverInstance == null) return;

                #region Debug Ouput
                //Instance instance = m_hoverInstance;
                //InstanceID instanceID = instance.id;
                //Debug.Log($"instance:{(instance == null ? "null" : instance.GetType().ToString())}");

                //if (instanceID.Building > 0)
                //{
                //    MoveableBuilding mb = (MoveableBuilding)instance;
                //    string msg = $"{mb.id.Building}:{mb.Info.Name}\n";
                //    //Debug.Log(msg);
                //    foreach (Instance subInstance in mb.subInstances)
                //    {
                //        msg += $" - {subInstance.id.Building}/{subInstance.id.NetNode}: {subInstance.Info.Name}\n";
                //        //Debug.Log(msg);
                //        if (subInstance.id.Building > 0)
                //        {
                //            foreach (Instance subSubInstance in ((MoveableBuilding)subInstance).subInstances)
                //            {
                //                msg += $"    - {subSubInstance.id.Building}/{subSubInstance.id.NetNode}: {subSubInstance.Info.Name}\n";
                //                //Debug.Log(msg);
                //            }
                //        }
                //    }
                //    msg += "End";
                //    Debug.Log(msg);
                //}
                #endregion

                if (!(ActionQueue.instance.current is SelectAction))
                {
                    ActionQueue.instance.Push(new SelectAction(e.shift));
                }
                else
                {
                    ActionQueue.instance.Invalidate();
                }

                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) //if (e.shift) - apparently fails in Linux
                {
                    if (e.alt && m_hoverInstance is MoveableSegment ms && FindOwnerBuilding(ms.id.NetSegment, 363f) == 0)
                    {
                        MoveableNode closest = ms.GetNodeByDistance();
                        MoveableNode furthest = ms.GetNodeByDistance(true);

                        if (!Action.selection.Contains(closest))
                        {
                            Action.selection.Add(closest);
                        }
                        else if (!Action.selection.Contains(furthest))
                        {
                            Action.selection.Add(furthest);
                        }
                        else
                        {
                            Action.selection.Remove(furthest);
                        }
                    }
                    else
                    {
                        if (Action.selection.Contains(m_hoverInstance))
                        {
                            Action.selection.Remove(m_hoverInstance);
                            //PO.SelectionRemove(m_hoverInstance);
                        }
                        else
                        {
                            Action.selection.Add(m_hoverInstance);
                            //PO.SelectionAdd(m_hoverInstance);
                        }
                    }
                }
                else
                {
                    if (e.alt && m_hoverInstance is MoveableSegment ms && FindOwnerBuilding(ms.id.NetSegment, 363f) == 0)
                    {
                        MoveableNode closest = ms.GetNodeByDistance();
                        MoveableNode furthest = ms.GetNodeByDistance(true);

                        if (Action.selection.Contains(closest) && !Action.selection.Contains(furthest))
                        {
                            Action.selection.Clear();
                            Action.selection.Add(furthest);
                        }
                        else
                        {
                            Action.selection.Clear();
                            Action.selection.Add(closest);
                        }
                    }
                    else
                    {
                        Action.selection.Clear();
                        Action.selection.Add(m_hoverInstance);
                    }
                }

                m_debugPanel.UpdatePanel();
                ToolState = ToolStates.Default;
            }
            else if (ToolState == ToolStates.Aligning)
            {
                if (AlignMode == AlignModes.Height)
                {
                    ToolState = ToolStates.Default;
                    AlignMode = AlignModes.Off;

                    AlignHeightAction action = new AlignHeightAction();
                    if (m_hoverInstance != null)
                    {
                        action.height = m_hoverInstance.position.y;
                        ActionQueue.instance.Push(action);

                        m_nextAction = ToolAction.Do;
                    }

                    UIMoreTools.UpdateMoreTools();
                }
                if (AlignMode == AlignModes.Mirror)
                {
                    ToolState = ToolStates.Default;
                    AlignMode = AlignModes.Off;

                    AlignMirrorAction action = new AlignMirrorAction();
                    if (m_hoverInstance != null && m_hoverInstance is MoveableSegment ms)
                    {
                        NetSegment[] segmentBuffer = Singleton<NetManager>.instance.m_segments.m_buffer;

                        Vector3 startPos = NetManager.instance.m_nodes.m_buffer[segmentBuffer[ms.id.NetSegment].m_startNode].m_position;
                        Vector3 endPos = NetManager.instance.m_nodes.m_buffer[segmentBuffer[ms.id.NetSegment].m_endNode].m_position;

                        action.mirrorPivot = ((endPos - startPos) / 2) + startPos;
                        action.mirrorAngle = -Mathf.Atan2(endPos.x - startPos.x, endPos.z - startPos.z);
                        action.followTerrain = followTerrain;

                        ActionQueue.instance.Push(action);
                        ActionQueue.instance.Do();
                    }

                    UIMoreTools.UpdateMoreTools();
                }
                else if (AlignMode == AlignModes.Inplace || AlignMode == AlignModes.Group)
                {
                    float angle;

                    if (m_hoverInstance is MoveableBuilding mb)
                    {
                        angle = Singleton<BuildingManager>.instance.m_buildings.m_buffer[mb.id.Building].m_angle;
                    }
                    else if (m_hoverInstance is MoveableProp mp)
                    {
                        angle = Singleton<PropManager>.instance.m_props.m_buffer[mp.id.Prop].Angle;
                    }
                    else if (m_hoverInstance is MoveableProc mpo)
                    {
                        angle = PO.GetProcObj(mpo.id.NetLane).Angle;
                    }
                    else if (m_hoverInstance is MoveableSegment ms)
                    {
                        NetSegment[] segmentBuffer = Singleton<NetManager>.instance.m_segments.m_buffer;

                        Vector3 startPos = NetManager.instance.m_nodes.m_buffer[segmentBuffer[ms.id.NetSegment].m_startNode].m_position;
                        Vector3 endPos = NetManager.instance.m_nodes.m_buffer[segmentBuffer[ms.id.NetSegment].m_endNode].m_position;

                        angle = (float)Math.Atan2(endPos.z - startPos.z, endPos.x - startPos.x);
                    }
                    else
                    {
                        return;
                    }

                    // Add action to queue, also enables Undo/Redo
                    AlignRotationAction action;
                    switch (AlignMode)
                    {
                        case AlignModes.Group:
                            action = new AlignGroupAction();
                            break;

                        default:
                            action = new AlignIndividualAction();
                            break;
                    }
                    action.newAngle = angle;
                    action.followTerrain = followTerrain;
                    ActionQueue.instance.Push(action);
                    m_nextAction = ToolAction.Do;

                    DeactivateTool(false);
                }
                else if (AlignMode == AlignModes.Slope)
                {
                    if (m_hoverInstance == null) return;

                    AlignSlopeAction action;
                    switch (AlignToolPhase)
                    {
                        case 1: // Point A selected, prepare for Point B
                            AlignToolPhase++;
                            action = new AlignSlopeAction();
                            action.PointA = m_hoverInstance;
                            ActionQueue.instance.Push(action);
                            UIMoreTools.UpdateMoreTools();
                            break;

                        case 2: // Point B selected, fire action
                            AlignToolPhase++;
                            action = ActionQueue.instance.current as AlignSlopeAction;
                            action.PointB = m_hoverInstance;
                            action.followTerrain = followTerrain;
                            m_nextAction = ToolAction.Do;
                            DeactivateTool();
                            break;
                    }
                }
            }
            else if (ToolState == ToolStates.Picking)
            {
                if (m_hoverInstance == null) return;

                Filters.Picker = new PickerFilter(m_hoverInstance.Info.Prefab);
                Filters.SetFilter("Picker", true);
                UIFilters.UpdatePickerButton(1);

                foreach (UICheckBox cb in UIFilters.FilterCBs)
                {
                    if (cb.name != "Picker")
                    {
                        cb.isChecked = false;
                        Filters.SetFilter(cb.name, false);
                    }
                    else
                    {
                        cb.isChecked = true;
                    }
                }
                UIFilters.RefreshFilters();

                ToolState = ToolStates.Default;
            }
        }

        private void OnRightClick()
        {
            DebugUtils.Log("OnRightClick: " + ToolState);

            if (ToolState == ToolStates.Default)
            {
                if (!(ActionQueue.instance.current is SelectAction action))
                {
                    action = new SelectAction();
                    ActionQueue.instance.Push(action);
                }
                else
                {
                    Action.selection.Clear();
                    ActionQueue.instance.Invalidate();
                    m_debugPanel.UpdatePanel();
                }
            }
            else if (ToolState == ToolStates.Cloning)
            {
                if (rmbCancelsCloning.value)
                {
                    StopCloning();
                }
                else
                {
                    // Rotate 45° clockwise
                    CloneAction action = ActionQueue.instance.current as CloneAction;
                    action.angleDelta -= Mathf.PI / 4;
                }
            }
            else if (ToolState == ToolStates.Aligning)
            {
                DeactivateTool();
            }
            else if (ToolState == ToolStates.Picking)
            {
                UIFilters.UpdatePickerButton(1);
                ToolState = ToolStates.Default;
            }
            else if (ToolState != ToolStates.MouseDragging)
            {
                ToolState = ToolStates.Default;
            }
        }

        private void OnLeftDrag()
        {
            DebugUtils.Log("OnLeftDrag: " + ToolState);

            if (ToolState == ToolStates.Default)
            {
                if (m_lastInstance == null) return;

                TransformAction action;
                if (Action.selection.Contains(m_lastInstance))
                {
                    action = ActionQueue.instance.current as TransformAction;
                    if (action == null)
                    {
                        action = new TransformAction();
                        ActionQueue.instance.Push(action);
                    }
                }
                else
                {
                    ActionQueue.instance.Push(new SelectAction());
                    Action.selection.Add(m_lastInstance);

                    action = new TransformAction();
                    ActionQueue.instance.Push(action);
                }

                m_dragStartRelative = action.moveDelta;
                UpdateSensitivityMode();

                ToolState = ToolStates.MouseDragging;
                m_debugPanel.UpdatePanel();
                action.InitialiseDrag();
            }
        }

        private void OnRightDrag()
        {
            DebugUtils.Log("OnRightDrag: " + ToolState);

            if (ToolState == ToolStates.Default)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                if (action == null)
                {
                    if (Action.selection.Count == 0) return;

                    action = new TransformAction();
                    ActionQueue.instance.Push(action);
                }

                m_startAngle = action.angleDelta;
                ToolState = ToolStates.MouseDragging;

                action.InitialiseDrag();
            }
            else if (ToolState == ToolStates.Cloning)
            {
                ToolState = ToolStates.RightDraggingClone;
            }
        }

        private void OnLeftDragStop()
        {
            DebugUtils.Log("OnLeftDragStop: " + ToolState);

            if (ToolState == ToolStates.MouseDragging && m_rightClickTime == 0)
            {
                UpdateSensitivityMode();

                ToolState = ToolStates.Default;
                ((TransformAction)ActionQueue.instance.current).FinaliseDrag();

                UIToolOptionPanel.RefreshSnapButton();
            }
        }

        private void OnRightDragStop()
        {
            DebugUtils.Log("OnRightDragStop: " + ToolState);

            if (ToolState == ToolStates.MouseDragging && m_leftClickTime == 0)
            {
                ToolState = ToolStates.Default;
                ((TransformAction)ActionQueue.instance.current).FinaliseDrag();

                UIToolOptionPanel.RefreshSnapButton();
            }
            else if (ToolState == ToolStates.RightDraggingClone)
            {
                ToolState = ToolStates.Cloning;
            }
        }

        private void UpdateSensitivityMode()
        {
            if (Event.current.control)
            {
                if (!m_isLowSensitivity)
                {
                    ProcessSensitivityMode(true);
                }
            }
            else
            {
                if (m_isLowSensitivity)
                {
                    ProcessSensitivityMode(false);
                    skipLowSensitivity = false;
                }
            }
        }

        //private void ForceUpdateSensitivityMode()
        //{
        //    if (Event.current.control)
        //        ProcessSensitivityMode(true);
        //    else
        //        ProcessSensitivityMode(false);
        //}

        private void ProcessSensitivityMode(bool on)
        {
            if (ActionQueue.instance.current is TransformAction || ActionQueue.instance.current is CloneAction)
            {
                Vector3 mousePos = RaycastMouseLocation();
                Vector3 mouseTravel = mousePos - m_sensitivityTogglePosAbs;

                //Vector3 oldSensTogglePos = m_sensitivityTogglePosAbs;
                //Vector3 oldSensOffset = m_sensitivityDistanceOffset;

                if (m_isLowSensitivity && !skipLowSensitivity)
                {
                    m_sensitivityDistanceOffset += (mouseTravel * 0.8f);
                }
                m_sensitivityTogglePosAbs = mousePos;

                m_isLowSensitivity = on;

                //Debug.Log($"AAA m_isLowSensitivity:{m_isLowSensitivity}\n");
                //Debug.Log($"AAA m_isLowSensitivity:{m_isLowSensitivity}\n" +
                //    $"m_sensitivityTogglePosAbs:{m_sensitivityTogglePosAbs} (was:{oldSensTogglePos})\n" +
                //    $"m_sensitivityTogglePosAbs delta:{m_sensitivityTogglePosAbs - oldSensTogglePos}\n" +
                //    $"m_sensitivityDistanceOffset:{m_sensitivityDistanceOffset} (was:{oldSensOffset})\n" +
                //    $"m_sensitivityDistanceOffset delta:{m_sensitivityDistanceOffset - oldSensOffset})\n" +
                //    $"m_clickPositionAbs:{m_clickPositionAbs}");
            }
        }
    }
}

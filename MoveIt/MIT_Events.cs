using ColossalFramework.Math;
using System;
using UnityEngine;

namespace MoveIt
{
    public partial class MoveItTool : ToolBase
    {
        private void OnLeftMouseDown()
        {
            DebugUtils.Log("OnLeftMouseDown: " + m_toolState);

            if (m_toolState == ToolStates.Default)
            {
                if (marqueeSelection && (m_hoverInstance == null || !Action.selection.Contains(m_hoverInstance)))
                {
                    m_selection = default(Quad3);
                    m_marqueeInstances = null;

                    m_toolState = ToolStates.DrawingSelection;
                }

                m_lastInstance = m_hoverInstance;

                Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                m_mouseStartPosition = RaycastMouseLocation(mouseRay);
            }
            else if (m_toolState == ToolStates.MouseDragging)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                m_startPosition = action.moveDelta;

                Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                m_mouseStartPosition = RaycastMouseLocation(mouseRay);
            }
            else if (m_toolState == ToolStates.Cloning)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;
                action.followTerrain = followTerrain;

                m_toolState = ToolStates.Default;
                m_nextAction = ToolAction.Do;
            }
        }

        private void OnRightMouseDown()
        {
            DebugUtils.Log("OnRightMouseDown: " + m_toolState);

            if (m_toolState == ToolStates.Default)
            {
                m_mouseStartX = Input.mousePosition.x;
            }
            else if (m_toolState == ToolStates.MouseDragging)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                m_startAngle = action.angleDelta;

                m_mouseStartX = Input.mousePosition.x;
            }
            else if (m_toolState == ToolStates.Cloning)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;
                m_startAngle = action.angleDelta;

                m_mouseStartX = Input.mousePosition.x;
            }
        }

        private void OnLeftMouseUp()
        {
            DebugUtils.Log("OnLeftMouseUp: " + m_toolState);

            if (m_toolState == ToolStates.DrawingSelection)
            {
                m_toolState = ToolStates.Default;

                Event e = Event.current;

                if (m_marqueeInstances == null ||
                    m_marqueeInstances.Count == 0 ||
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
                }
                else
                {
                    if (!e.shift)
                    {
                        Action.selection.Clear();
                    }
                    Action.selection.UnionWith(m_marqueeInstances);
                }

                m_marqueeInstances = null;
            }
        }

        private void OnRightMouseUp()
        { }

        private void OnRightClick()
        {
            DebugUtils.Log("OnRightClick: " + m_toolState);

            if (m_toolState == ToolStates.Default)
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
                }
            }
            else if (m_toolState == ToolStates.Cloning)
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
            else if (m_toolState == ToolStates.Aligning)
            {
                DeactivateAlignTool();
            }
            else if (m_toolState != ToolStates.MouseDragging)
            {
                m_toolState = ToolStates.Default;
            }
        }

        private void OnLeftClick()
        {
            DebugUtils.Log("OnLeftClick: " + m_toolState);

            if (m_toolState == ToolStates.Default || m_toolState == ToolStates.DrawingSelection)
            {
                Event e = Event.current;
                if (m_hoverInstance == null) return;

                if (!(ActionQueue.instance.current is SelectAction action))
                {
                    ActionQueue.instance.Push(new SelectAction(e.shift));
                }
                else
                {
                    ActionQueue.instance.Invalidate();
                }

                if (e.shift)
                {
                    if (Action.selection.Contains(m_hoverInstance))
                    {
                        Action.selection.Remove(m_hoverInstance);
                    }
                    else
                    {
                        Action.selection.Add(m_hoverInstance);
                    }
                }
                else
                {
                    Action.selection.Clear();
                    Action.selection.Add(m_hoverInstance);
                }

                m_toolState = ToolStates.Default;
            }
            else if (m_toolState == ToolStates.Aligning)
            {
                if (m_alignMode == AlignModes.Height)
                {
                    m_toolState = ToolStates.Default;
                    m_alignMode = AlignModes.Off;

                    AlignHeightAction action = new AlignHeightAction();
                    if (m_hoverInstance != null)
                    {
                        action.height = m_hoverInstance.position.y;
                        ActionQueue.instance.Push(action);

                        m_nextAction = ToolAction.Do;
                    }

                    UIAlignTools.UpdateAlignTools();
                }
                else if (m_alignMode == AlignModes.Inplace || m_alignMode == AlignModes.Group)
                {
                    float angle;

                    if (m_hoverInstance is MoveableBuilding mb)
                    {
                        angle = BuildingManager.instance.m_buildings.m_buffer[mb.id.Building].m_angle;
                    }
                    else if (m_hoverInstance is MoveableProp mp)
                    {
                        angle = PropManager.instance.m_props.m_buffer[mp.id.Prop].Angle;
                    }
                    else if (m_hoverInstance is MoveableSegment ms)
                    {
                        NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;

                        Vector3 startPos = NetManager.instance.m_nodes.m_buffer[segmentBuffer[ms.id.NetSegment].m_startNode].m_position;
                        Vector3 endPos = NetManager.instance.m_nodes.m_buffer[segmentBuffer[ms.id.NetSegment].m_endNode].m_position;

                        //Debug.Log($"Vector:{endPos.x - startPos.x},{endPos.z - startPos.z} Start:{startPos.x},{startPos.z} End:{endPos.x},{endPos.z}");
                        angle = (float)Math.Atan2(endPos.z - startPos.z, endPos.x - startPos.x);
                    }
                    else
                    {
                        //Debug.Log($"Wrong hover asset type <{___m_hoverInstance.GetType()}>");
                        return;
                    }

                    // Add action to queue, also enables Undo/Redo
                    AlignRotationAction action;
                    switch (m_alignMode)
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

                    //Debug.Log($"Angle:{angle}, from {___m_hoverInstance}");
                    DeactivateAlignTool(false);
                }
                else if (m_alignMode == AlignModes.Slope)
                {
                    if (m_hoverInstance == null) return;

                    AlignSlopeAction action;
                    switch (m_alignToolPhase)
                    {
                        case 1: // Point A selected, prepare for Point B
                            m_alignToolPhase++;
                            action = new AlignSlopeAction();
                            action.PointA = m_hoverInstance;
                            ActionQueue.instance.Push(action);
                            UIAlignTools.UpdateAlignTools();
                            break;

                        case 2: // Point B selected, fire action
                            m_alignToolPhase++;
                            action = ActionQueue.instance.current as AlignSlopeAction;
                            action.PointB = m_hoverInstance;
                            action.followTerrain = followTerrain;
                            m_nextAction = ToolAction.Do;
                            DeactivateAlignTool();
                            //UIAlignTools.UpdateAlignTools();
                            break;
                    }
                }
            }
        }

        private void OnLeftDrag()
        {
            DebugUtils.Log("OnLeftDrag: " + m_toolState);

            if (m_toolState == ToolStates.Default)
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

                m_startPosition = action.moveDelta;

                m_toolState = ToolStates.MouseDragging;
            }
        }

        private void OnRightDrag()
        {
            DebugUtils.Log("OnRightDrag: " + m_toolState);

            if (m_toolState == ToolStates.Default)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                if (action == null)
                {
                    if (Action.selection.Count == 0) return;

                    action = new TransformAction();
                    ActionQueue.instance.Push(action);
                }

                m_startAngle = action.angleDelta;
                m_toolState = ToolStates.MouseDragging;
            }
            else if (m_toolState == ToolStates.Cloning)
            {
                m_toolState = ToolStates.RightDraggingClone;
            }
        }

        private void OnLeftDragStop()
        {
            DebugUtils.Log("OnLeftDragStop: " + m_toolState);

            if (m_toolState == ToolStates.MouseDragging && m_rightClickTime == 0)
            {
                m_toolState = ToolStates.Default;

                UIToolOptionPanel.RefreshSnapButton();
            }
        }

        private void OnRightDragStop()
        {
            DebugUtils.Log("OnRightDragStop: " + m_toolState);

            if (m_toolState == ToolStates.MouseDragging && m_leftClickTime == 0)
            {
                m_toolState = ToolStates.Default;

                UIToolOptionPanel.RefreshSnapButton();
            }
            else if (m_toolState == ToolStates.RightDraggingClone)
            {
                m_toolState = ToolStates.Cloning;
            }
        }
    }
}

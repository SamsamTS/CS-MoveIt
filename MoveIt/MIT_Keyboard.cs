using ColossalFramework.UI;
using UnityEngine;

namespace MoveIt
{
    public partial class MoveItTool : ToolBase
    {
        protected override void OnToolGUI(Event e)
        {
            if (UIView.HasModalInput() || UIView.HasInputFocus()) return;

            lock (ActionQueue.instance)
            {
                if (ToolState == ToolStates.Default || ToolState == ToolStates.ToolActive)
                {
                    if (OptionsKeymapping.undo.IsPressed(e))
                    {
                        m_nextAction = ToolAction.Undo;
                    }
                    else if (OptionsKeymapping.redo.IsPressed(e))
                    {
                        m_nextAction = ToolAction.Redo;
                    }
                }

                if (OptionsKeymapping.clone.IsPressed(e))
                {
                    if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
                    {
                        StopCloning();
                    }
                    else
                    {
                        StartCloning();
                    }
                }
                else if (OptionsKeymapping.bulldoze.IsPressed(e))
                {
                    StartBulldoze();
                }
                else if (OptionsKeymapping.reset.IsPressed(e))
                {
                    StartReset();
                }
                else if (OptionsKeymapping.viewGrid.IsPressed(e))
                {
                    if (gridVisible)
                    {
                        gridVisible = false;
                        UIToolOptionPanel.instance.grid.activeStateIndex = 0;
                    }
                    else
                    {
                        gridVisible = true;
                        UIToolOptionPanel.instance.grid.activeStateIndex = 1;
                    }
                }
                else if (OptionsKeymapping.viewUnderground.IsPressed(e))
                {
                    if (tunnelVisible)
                    {
                        tunnelVisible = false;
                        UIToolOptionPanel.instance.underground.activeStateIndex = 0;
                    }
                    else
                    {
                        tunnelVisible = true;
                        UIToolOptionPanel.instance.underground.activeStateIndex = 1;
                    }
                }
                else if (OptionsKeymapping.viewDebug.IsPressed(e))
                {
                    showDebugPanel.value = !showDebugPanel;
                    if (m_debugPanel != null)
                    {
                        ClearDebugOverlays();
                        m_debugPanel.Visible(showDebugPanel);
                    }
                }
                else if (OptionsKeymapping.activatePO.IsPressed(e))
                {
                    PO.InitialiseTool();
                }
                else if (OptionsKeymapping.convertToPO.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_ConvertToPOBtn");
                }
                else if (OptionsKeymapping.deselectAll.IsPressed(e))
                {
                    if (ToolState == ToolStates.Cloning)
                    {
                        StopCloning();
                    }
                    else if (ToolState == ToolStates.Aligning || ToolState == ToolStates.ToolActive || ToolState == ToolStates.Picking || ToolState != ToolStates.MouseDragging)
                    {
                        if (ToolState == ToolStates.Picking)
                        {
                            UIFilters.UpdatePickerButton(1);
                        }
                        DeactivateTool();
                    }

                    if (!(ActionQueue.instance.current is SelectAction))
                    {
                        SelectAction action = new SelectAction();
                        ActionQueue.instance.Push(action);
                    }
                    else
                    {
                        Action.selection.Clear();
                        ActionQueue.instance.Invalidate();
                    }
                    m_debugPanel.UpdatePanel();
                }
                else if (OptionsKeymapping.alignHeights.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_AlignHeightBtn");
                }
                else if (OptionsKeymapping.alignMirror.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_AlignMirrorBtn");
                }
                else if (OptionsKeymapping.alignLine.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_AlignLineBtn");
                }
                else if (OptionsKeymapping.alignLineUnspaced.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_AlignLineBtn", false, true);
                }
                else if (OptionsKeymapping.alignSlope.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_AlignSlopeBtn");
                }
                else if (OptionsKeymapping.alignSlopeQuick.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_AlignSlopeBtn", true);
                }
                else if (OptionsKeymapping.alignSlopeFull.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_AlignSlopeBtn", false, true);
                }
                else if (OptionsKeymapping.alignInplace.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_AlignIndividualBtn");
                }
                else if (OptionsKeymapping.alignGroup.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_AlignGroupBtn");
                }
                else if (OptionsKeymapping.alignRandom.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_AlignRandomBtn");
                }
                else if (OptionsKeymapping.alignMoveTo.IsPressed(e))
                {
                    UIMoreTools.MoreToolsClicked("MoveIt_MoveToBtn");
                }

                if (ToolState == ToolStates.Cloning)
                {
                    if (ProcessMoveKeys(e, out Vector3 direction, out float angle))
                    {
                        CloneAction action = ActionQueue.instance.current as CloneAction;

                        action.moveDelta.y += direction.y * YFACTOR;
                        action.angleDelta += angle;
                    }
                }
                else if (ToolState == ToolStates.Default && Action.selection.Count > 0)
                {
                    if (ProcessMoveKeys(e, out Vector3 direction, out float angle))
                    {
                        if (!(ActionQueue.instance.current is TransformAction action))
                        {
                            action = new TransformAction();
                            ActionQueue.instance.Push(action);
                        }

                        if (direction != Vector3.zero)
                        {
                            direction.x *= XFACTOR;
                            direction.y *= YFACTOR;
                            direction.z *= ZFACTOR;

                            if (!useCardinalMoves)
                            {
                                Matrix4x4 matrix4x = default;
                                matrix4x.SetTRS(Vector3.zero, Quaternion.AngleAxis(Camera.main.transform.localEulerAngles.y, Vector3.up), Vector3.one);

                                direction = matrix4x.MultiplyVector(direction);
                            }
                        }

                        action.moveDelta += direction;
                        action.angleDelta += angle;
                        action.followTerrain = followTerrain;

                        m_nextAction = ToolAction.Do;
                    }
                    else if (ProcessScaleKeys(e, out float magnitude))
                    {
                        if (!(ActionQueue.instance.current is ScaleAction action))
                        {
                            action = new ScaleAction();
                            ActionQueue.instance.Push(action);
                        }

                        action.magnitude += magnitude;
                        action.followTerrain = followTerrain;

                        m_nextAction = ToolAction.Do;
                    }
                }
            }
        }
    }
}

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
                if (ToolState == ToolStates.Default)
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
                        m_debugPanel.Visible(showDebugPanel);
                    }
                }
                else if (OptionsKeymapping.activatePO.IsPressed(e))
                {
                    if (PO.Active == false)
                    {
                        PO.Active = true;
                        UIToolOptionPanel.instance.PO_button.activeStateIndex = 1;
                        PO.ToolEnabled();
                    }
                    else
                    {
                        PO.Active = false;
                        UIToolOptionPanel.instance.PO_button.activeStateIndex = 0;
                    }
                    UIFilters.POToggled();
                }
                else if (OptionsKeymapping.convertToPO.IsPressed(e))
                {
                    if (PO.Enabled && ToolState == ToolStates.Default)
                    {
                        if (PO.Active == false)
                        {
                            PO.Active = true;
                            UIToolOptionPanel.instance.PO_button.activeStateIndex = 1;
                            PO.ToolEnabled();
                            UIFilters.POToggled();
                        }

                        ConvertToPOAction convertAction = new ConvertToPOAction();
                        ActionQueue.instance.Push(convertAction);
                        ActionQueue.instance.Do();
                    }
                }
                else if (OptionsKeymapping.alignHeights.IsPressed(e))
                {
                    ProcessAligning(AlignModes.Height);
                }
                else if (OptionsKeymapping.alignSlope.IsPressed(e))
                {
                    ProcessAligning(AlignModes.Slope);
                }
                else if (OptionsKeymapping.alignMirror.IsPressed(e))
                {
                    ProcessAligning(AlignModes.Mirror);
                }
                else if (OptionsKeymapping.alignSlopeQuick.IsPressed(e))
                {
                    AlignMode = AlignModes.SlopeNode;

                    if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
                    {
                        StopCloning();
                    }

                    AlignSlopeAction asa = new AlignSlopeAction
                    {
                        followTerrain = followTerrain,
                        IsQuick = true
                    };
                    ActionQueue.instance.Push(asa);
                    ActionQueue.instance.Do();
                    if (autoCloseAlignTools) UIMoreTools.MoreToolsPanel.isVisible = false;
                    DeactivateTool();
                }
                else if (OptionsKeymapping.alignInplace.IsPressed(e))
                {
                    ProcessAligning(AlignModes.Inplace);
                }
                else if (OptionsKeymapping.alignGroup.IsPressed(e))
                {
                    ProcessAligning(AlignModes.Group);
                }
                else if (OptionsKeymapping.alignRandom.IsPressed(e))
                {
                    AlignMode = AlignModes.Random;

                    if (ToolState == ToolStates.Cloning || ToolState == ToolStates.RightDraggingClone)
                    {
                        StopCloning();
                    }

                    AlignRandomAction action = new AlignRandomAction();
                    action.followTerrain = followTerrain;
                    ActionQueue.instance.Push(action);
                    ActionQueue.instance.Do();
                    DeactivateTool();
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
                            direction.x = direction.x * XFACTOR;
                            direction.y = direction.y * YFACTOR;
                            direction.z = direction.z * ZFACTOR;

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
                }
            }
        }
    }
}

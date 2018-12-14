using ColossalFramework.Math;
using UnityEngine;

namespace MoveIt
{
    public partial class MoveItTool : ToolBase
    {
        private void OnLeftMouseDown()
        {
            DebugUtils.Log("OnLeftMouseDown: " + toolState);

            if (toolState == ToolState.Default)
            {
                if (marqueeSelection && (m_hoverInstance == null || !Action.selection.Contains(m_hoverInstance)))
                {
                    m_selection = default(Quad3);
                    m_marqueeInstances = null;

                    toolState = ToolState.DrawingSelection;
                }

                m_lastInstance = m_hoverInstance;

                Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                m_mouseStartPosition = RaycastMouseLocation(mouseRay);
            }
            else if (toolState == ToolState.MouseDragging)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                m_startPosition = action.moveDelta;

                Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                m_mouseStartPosition = RaycastMouseLocation(mouseRay);
            }
            else if (toolState == ToolState.Cloning)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;
                action.followTerrain = followTerrain;

                toolState = ToolState.Default;
                m_nextAction = ToolAction.Do;
            }
        }

        private void OnRightMouseDown()
        {
            DebugUtils.Log("OnRightMouseDown: " + toolState);

            if (toolState == ToolState.Default)
            {
                m_mouseStartX = Input.mousePosition.x;
            }
            else if (toolState == ToolState.MouseDragging)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                m_startAngle = action.angleDelta;

                m_mouseStartX = Input.mousePosition.x;
            }
            else if (toolState == ToolState.Cloning)
            {
                CloneAction action = ActionQueue.instance.current as CloneAction;
                m_startAngle = action.angleDelta;

                m_mouseStartX = Input.mousePosition.x;
            }
        }

        private void OnLeftMouseUp()
        {
            DebugUtils.Log("OnLeftMouseUp: " + toolState);

            if (toolState == ToolState.DrawingSelection)
            {
                toolState = ToolState.Default;

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
            DebugUtils.Log("OnRightClick: " + toolState);

            if (toolState == ToolState.Default)
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
            else if (toolState == ToolState.Cloning)
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
            else if (toolState != ToolState.MouseDragging)
            {
                toolState = ToolState.Default;
            }
        }

        private void OnLeftClick()
        {
            DebugUtils.Log("OnLeftClick: " + toolState);

            if (toolState == ToolState.Default || toolState == ToolState.DrawingSelection)
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

                toolState = ToolState.Default;
            }
            else if (toolState == ToolState.AligningHeights)
            {
                toolState = ToolState.Default;

                AlignHeightAction action = new AlignHeightAction();
                if (m_hoverInstance != null)
                {
                    action.height = m_hoverInstance.position.y;
                    ActionQueue.instance.Push(action);

                    m_nextAction = ToolAction.Do;
                }

                UIToolOptionPanel.RefreshAlignHeightButton();
            }
        }

        private void OnLeftDrag()
        {
            DebugUtils.Log("OnLeftDrag: " + toolState);

            if (toolState == ToolState.Default)
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

                toolState = ToolState.MouseDragging;
            }
        }

        private void OnRightDrag()
        {
            DebugUtils.Log("OnRightDrag: " + toolState);

            if (toolState == ToolState.Default)
            {
                TransformAction action = ActionQueue.instance.current as TransformAction;
                if (action == null)
                {
                    if (Action.selection.Count == 0) return;

                    action = new TransformAction();
                    ActionQueue.instance.Push(action);
                }

                m_startAngle = action.angleDelta;
                toolState = ToolState.MouseDragging;
            }
            else if (toolState == ToolState.Cloning)
            {
                toolState = ToolState.RightDraggingClone;
            }
        }

        private void OnLeftDragStop()
        {
            DebugUtils.Log("OnLeftDragStop: " + toolState);

            if (toolState == ToolState.MouseDragging && m_rightClickTime == 0)
            {
                toolState = ToolState.Default;

                UIToolOptionPanel.RefreshSnapButton();
            }
        }

        private void OnRightDragStop()
        {
            DebugUtils.Log("OnRightDragStop: " + toolState);

            if (toolState == ToolState.MouseDragging && m_leftClickTime == 0)
            {
                toolState = ToolState.Default;

                UIToolOptionPanel.RefreshSnapButton();
            }
            else if (toolState == ToolState.RightDraggingClone)
            {
                toolState = ToolState.Cloning;
            }
        }

    }
}

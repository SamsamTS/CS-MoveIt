using ColossalFramework;
using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoveIt
{
    public partial class MoveItTool : ToolBase
    {
        static bool _stepProcessed = false;

        private void RaycastHoverInstance(Ray mouseRay)
        {
            Vector3 origin = mouseRay.origin;
            Vector3 normalized = mouseRay.direction.normalized;
            Vector3 vector = mouseRay.origin + normalized * Camera.main.farClipPlane;
            Segment3 ray = new Segment3(origin, vector);

            Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
            PropInstance[] propBuffer = PropManager.instance.m_props.m_buffer;
            NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;
            NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;
            TreeInstance[] treeBuffer = TreeManager.instance.m_trees.m_buffer;

            Vector3 location = RaycastMouseLocation(mouseRay);

            InstanceID id = InstanceID.Empty;

            ItemClass.Layer itemLayers = GetItemLayers();

            bool selectBuilding = true;
            bool selectProps = true;
            bool selectDecals = true;
            bool selectSurfaces = true;
            bool selectNodes = true;
            bool selectSegments = true;
            bool selectTrees = true;
            bool selectProc = true;

            bool repeatSearch = false;

            if (marqueeSelection)
            {
                selectBuilding = filterBuildings;
                selectProps = filterProps;
                selectDecals = filterDecals;
                selectSurfaces = filterSurfaces;
                selectNodes = filterNodes;
                selectSegments = filterSegments;
                selectTrees = filterTrees;
            }

            float smallestDist = 640000f;

            do
            {
                int gridMinX = Mathf.Max((int)((location.x - 16f) / 64f + 135f) - 1, 0);
                int gridMinZ = Mathf.Max((int)((location.z - 16f) / 64f + 135f) - 1, 0);
                int gridMaxX = Mathf.Min((int)((location.x + 16f) / 64f + 135f) + 1, 269);
                int gridMaxZ = Mathf.Min((int)((location.z + 16f) / 64f + 135f) + 1, 269);

                for (int i = gridMinZ; i <= gridMaxZ; i++)
                {
                    for (int j = gridMinX; j <= gridMaxX; j++)
                    {
                        if (selectBuilding || selectSurfaces)
                        {
                            ushort building = BuildingManager.instance.m_buildingGrid[i * 270 + j];
                            int count = 0;
                            while (building != 0u)
                            {
                                if (stepOver.isValidB(building) && IsBuildingValid(ref buildingBuffer[building], itemLayers) && buildingBuffer[building].RayCast(building, ray, out float t) && t < smallestDist)
                                {
                                    //Debug.Log($"Building:{building}");

                                    if (Filters.Filter(buildingBuffer[building].Info, true))
                                    {
                                        id.Building = Building.FindParentBuilding(building);
                                        if (id.Building == 0) id.Building = building;
                                        smallestDist = t;
                                    }
                                }
                                building = buildingBuffer[building].m_nextGridBuilding;

                                if (++count > 49152)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                    break;
                                }
                            }
                        }

                        if (selectProps || selectDecals || selectSurfaces)
                        {
                            ushort prop = PropManager.instance.m_propGrid[i * 270 + j];
                            int count = 0;
                            while (prop != 0u)
                            {
                                if (stepOver.isValidP(prop) && Filters.Filter(propBuffer[prop].Info))
                                {
                                    //Debug.Log($"Prop:{prop}");
                                    if (propBuffer[prop].RayCast(prop, ray, out float t, out float targetSqr) && t < smallestDist)
                                    {
                                        id.Prop = prop;
                                        smallestDist = t;
                                    }
                                }

                                prop = propBuffer[prop].m_nextGridProp;

                                if (++count > 65536)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }

                        if (selectNodes || selectBuilding)
                        {
                            ushort node = NetManager.instance.m_nodeGrid[i * 270 + j];
                            int count = 0;
                            while (node != 0u)
                            {
                                if (stepOver.isValidN(node) && IsNodeValid(ref nodeBuffer[node], itemLayers) && RayCastNode(ref nodeBuffer[node], ray, -1000f, out float t, out float priority) && t < smallestDist)
                                {
                                    //Debug.Log($"Node:{node}");
                                    ushort building = 0;
                                    if (!Event.current.alt)
                                    {
                                        building = NetNode.FindOwnerBuilding(node, 363f);
                                    }

                                    if (building != 0)
                                    {
                                        if (selectBuilding)
                                        {
                                            id.Building = Building.FindParentBuilding(building);
                                            if (id.Building == 0) id.Building = building;
                                            smallestDist = t;
                                        }
                                    }
                                    else if (selectNodes)
                                    {
                                        if (Filters.Filter(nodeBuffer[node]))
                                        {
                                            id.NetNode = node;
                                            smallestDist = t;
                                        }
                                    }
                                }
                                node = nodeBuffer[node].m_nextGridNode;

                                if (++count > 32768)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }

                        if (selectSegments || selectBuilding)
                        {
                            ushort segment = NetManager.instance.m_segmentGrid[i * 270 + j];
                            int count = 0;
                            while (segment != 0u)
                            {
                                if (stepOver.isValidS(segment) && IsSegmentValid(ref segmentBuffer[segment], itemLayers) &&
                                    segmentBuffer[segment].RayCast(segment, ray, -1000f, false, out float t, out float priority) && t < smallestDist)
                                {
                                    //Debug.Log($"Segment:{segment}");
                                    ushort building = 0;
                                    if (!Event.current.alt)
                                    {
                                        building = MoveItTool.FindOwnerBuilding(segment, 363f);
                                    }

                                    if (building != 0)
                                    {
                                        if (selectBuilding)
                                        {
                                            id.Building = Building.FindParentBuilding(building);
                                            if (id.Building == 0) id.Building = building;
                                            smallestDist = t;
                                        }
                                    }
                                    else if (selectSegments)
                                    {
                                        if (!selectNodes || (
                                            !RayCastNode(ref nodeBuffer[segmentBuffer[segment].m_startNode], ray, -1000f, out float t2, out priority) &&
                                            !RayCastNode(ref nodeBuffer[segmentBuffer[segment].m_endNode], ray, -1000f, out t2, out priority)
                                        ))
                                        {
                                            if (Filters.Filter(segmentBuffer[segment]))
                                            {
                                                id.NetSegment = segment;
                                                smallestDist = t;
                                            }
                                        }
                                    }
                                }
                                segment = segmentBuffer[segment].m_nextGridSegment;

                                if (++count > 36864)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }
                    }
                }

                if (selectProc)
                {
                }

                if (selectTrees)
                {
                    gridMinX = Mathf.Max((int)((location.x - 8f) / 32f + 270f), 0);
                    gridMinZ = Mathf.Max((int)((location.z - 8f) / 32f + 270f), 0);
                    gridMaxX = Mathf.Min((int)((location.x + 8f) / 32f + 270f), 539);
                    gridMaxZ = Mathf.Min((int)((location.z + 8f) / 32f + 270f), 539);

                    for (int i = gridMinZ; i <= gridMaxZ; i++)
                    {
                        for (int j = gridMinX; j <= gridMaxX; j++)
                        {
                            uint tree = TreeManager.instance.m_treeGrid[i * 540 + j];
                            int count = 0;
                            while (tree != 0)
                            {
                                if (stepOver.isValidT(tree) && treeBuffer[tree].RayCast(tree, ray, out float t, out float targetSqr) && t < smallestDist)
                                {
                                    id.Tree = tree;
                                    smallestDist = t;
                                }
                                tree = treeBuffer[tree].m_nextGridTree;

                                if (++count > 262144)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }
                    }
                }

                repeatSearch = false;
                if (OptionsKeymapping.stepOverKey.IsPressed())
                {
                    if (!_stepProcessed)
                    {
                        _stepProcessed = true;
                        repeatSearch = true;
                        stepOver.Add(id);
                    }
                }
                else
                {
                    _stepProcessed = false;
                }
            }
            while (repeatSearch);

            //Debug.Log($"ID=({id.Building},{id.Prop},{id.NetNode},{id.NetSegment},{id.Tree})");
            if (debugPanel != null)
            {
                debugPanel.Update(id);
            }
            m_hoverInstance = id;
        }


        private HashSet<Instance> GetMarqueeList(Ray mouseRay)
        {
            HashSet<Instance> list = new HashSet<Instance>();

            Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
            PropInstance[] propBuffer = PropManager.instance.m_props.m_buffer;
            NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;
            NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;
            TreeInstance[] treeBuffer = TreeManager.instance.m_trees.m_buffer;

            m_selection.a = m_mouseStartPosition;
            m_selection.c = RaycastMouseLocation(mouseRay);

            if (m_selection.a.x == m_selection.c.x && m_selection.a.z == m_selection.c.z)
            {
                m_selection = default(Quad3);
            }
            else
            {
                float angle = Camera.main.transform.localEulerAngles.y * Mathf.Deg2Rad;
                Vector3 down = new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle));
                Vector3 right = new Vector3(-down.z, 0, down.x);

                Vector3 a = m_selection.c - m_selection.a;
                float dotDown = Vector3.Dot(a, down);
                float dotRight = Vector3.Dot(a, right);

                if ((dotDown > 0 && dotRight > 0) || (dotDown <= 0 && dotRight <= 0))
                {
                    m_selection.b = m_selection.a + dotDown * down;
                    m_selection.d = m_selection.a + dotRight * right;
                }
                else
                {
                    m_selection.b = m_selection.a + dotRight * right;
                    m_selection.d = m_selection.a + dotDown * down;
                }

                Vector3 min = m_selection.Min();
                Vector3 max = m_selection.Max();

                int gridMinX = Mathf.Max((int)((min.x - 16f) / 64f + 135f), 0);
                int gridMinZ = Mathf.Max((int)((min.z - 16f) / 64f + 135f), 0);
                int gridMaxX = Mathf.Min((int)((max.x + 16f) / 64f + 135f), 269);
                int gridMaxZ = Mathf.Min((int)((max.z + 16f) / 64f + 135f), 269);

                InstanceID id = new InstanceID();
                ItemClass.Layer itemLayers = GetItemLayers();

                for (int i = gridMinZ; i <= gridMaxZ; i++)
                {
                    for (int j = gridMinX; j <= gridMaxX; j++)
                    {
                        if (filterBuildings || filterSurfaces)
                        {
                            ushort building = BuildingManager.instance.m_buildingGrid[i * 270 + j];
                            int count = 0;
                            while (building != 0u)
                            {
                                //Debug.Log($"Building:{building}");
                                if (IsBuildingValid(ref buildingBuffer[building], itemLayers) && PointInRectangle(m_selection, buildingBuffer[building].m_position))
                                {
                                    if (Filters.Filter(buildingBuffer[building].Info))
                                    {
                                        id.Building = Building.FindParentBuilding(building);
                                        if (id.Building == 0) id.Building = building;
                                        list.Add(id);
                                    }
                                }
                                building = buildingBuffer[building].m_nextGridBuilding;

                                if (++count > 49152)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                    break;
                                }
                            }
                        }

                        if (filterProps || filterDecals || filterSurfaces)
                        {
                            ushort prop = PropManager.instance.m_propGrid[i * 270 + j];
                            int count = 0;
                            while (prop != 0u)
                            {
                                //Debug.Log($"Prop:{prop}");
                                if (Filters.Filter(propBuffer[prop].Info))
                                {
                                    if (PointInRectangle(m_selection, propBuffer[prop].Position))
                                    {
                                        id.Prop = prop;
                                        list.Add(id);
                                    }
                                }

                                prop = propBuffer[prop].m_nextGridProp;

                                if (++count > 65536)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }

                        if (filterNodes || filterBuildings)
                        {
                            ushort node = NetManager.instance.m_nodeGrid[i * 270 + j];
                            int count = 0;
                            while (node != 0u)
                            {
                                //Debug.Log($"Node:{node}");
                                if (IsNodeValid(ref nodeBuffer[node], itemLayers) && PointInRectangle(m_selection, nodeBuffer[node].m_position))
                                {
                                    ushort building = NetNode.FindOwnerBuilding(node, 363f);

                                    if (building != 0)
                                    {
                                        if (filterBuildings)
                                        {
                                            id.Building = Building.FindParentBuilding(building);
                                            if (id.Building == 0) id.Building = building;
                                            list.Add(id);
                                        }
                                    }
                                    else if (filterNodes)
                                    {
                                        if (Filters.Filter(nodeBuffer[node]))
                                        {
                                            //Debug.Log($"Node:{node}");
                                            id.NetNode = node;
                                            list.Add(id);
                                        }
                                    }
                                }
                                node = nodeBuffer[node].m_nextGridNode;

                                if (++count > 32768)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }

                        if (filterSegments || filterBuildings)
                        {
                            ushort segment = NetManager.instance.m_segmentGrid[i * 270 + j];
                            int count = 0;
                            while (segment != 0u)
                            {
                                //Debug.Log($"Segment:{segment}");
                                if (IsSegmentValid(ref segmentBuffer[segment], itemLayers) && PointInRectangle(m_selection, segmentBuffer[segment].m_bounds.center))
                                {
                                    ushort building = FindOwnerBuilding(segment, 363f);

                                    if (building != 0)
                                    {
                                        if (filterBuildings)
                                        {
                                            id.Building = Building.FindParentBuilding(building);
                                            if (id.Building == 0) id.Building = building;
                                            list.Add(id);
                                        }
                                    }
                                    else if (filterSegments)
                                    {
                                        if (Filters.Filter(segmentBuffer[segment]))
                                        {
                                            id.NetSegment = segment;
                                            list.Add(id);
                                        }
                                    }
                                }
                                segment = segmentBuffer[segment].m_nextGridSegment;

                                if (++count > 36864)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }
                    }
                }

                if (filterTrees)
                {
                    gridMinX = Mathf.Max((int)((min.x - 8f) / 32f + 270f), 0);
                    gridMinZ = Mathf.Max((int)((min.z - 8f) / 32f + 270f), 0);
                    gridMaxX = Mathf.Min((int)((max.x + 8f) / 32f + 270f), 539);
                    gridMaxZ = Mathf.Min((int)((max.z + 8f) / 32f + 270f), 539);

                    for (int i = gridMinZ; i <= gridMaxZ; i++)
                    {
                        for (int j = gridMinX; j <= gridMaxX; j++)
                        {
                            uint tree = TreeManager.instance.m_treeGrid[i * 540 + j];
                            int count = 0;
                            while (tree != 0)
                            {
                                //Debug.Log($"Tree:{tree}");
                                if (PointInRectangle(m_selection, treeBuffer[tree].Position))
                                {
                                    id.Tree = tree;
                                    list.Add(id);
                                }
                                tree = treeBuffer[tree].m_nextGridTree;

                                if (++count > 262144)
                                {
                                    CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                }
                            }
                        }
                    }
                }
            }

            return list;
        }


        public static ushort FindOwnerBuilding(ushort segment, float maxDistance)
        {
            Building[] buildingBuffer = BuildingManager.instance.m_buildings.m_buffer;
            ushort[] buildingGrid = BuildingManager.instance.m_buildingGrid;
            NetNode[] nodeBuffer = NetManager.instance.m_nodes.m_buffer;
            NetSegment[] segmentBuffer = NetManager.instance.m_segments.m_buffer;

            ushort startNode = segmentBuffer[segment].m_startNode;
            ushort endNode = segmentBuffer[segment].m_endNode;
            Vector3 startPosition = nodeBuffer[startNode].m_position;
            Vector3 endPosition = nodeBuffer[endNode].m_position;
            Vector3 vector = Vector3.Min(startPosition, endPosition);
            Vector3 vector2 = Vector3.Max(startPosition, endPosition);
            int gridMinX = Mathf.Max((int)((vector.x - maxDistance) / 64f + 135f), 0);
            int gridMinZ = Mathf.Max((int)((vector.z - maxDistance) / 64f + 135f), 0);
            int gridMaxX = Mathf.Min((int)((vector2.x + maxDistance) / 64f + 135f), 269);
            int gridMaxZ = Mathf.Min((int)((vector2.z + maxDistance) / 64f + 135f), 269);

            ushort result = 0;
            float maxDistSqr = maxDistance * maxDistance;
            for (int i = gridMinZ; i <= gridMaxZ; i++)
            {
                for (int j = gridMinX; j <= gridMaxX; j++)
                {
                    ushort building = buildingGrid[i * 270 + j];
                    int count = 0;
                    while (building != 0)
                    {
                        Vector3 position2 = buildingBuffer[building].m_position;
                        float num8 = position2.x - startPosition.x;
                        float num9 = position2.z - startPosition.z;
                        float num10 = num8 * num8 + num9 * num9;
                        if (num10 < maxDistSqr && buildingBuffer[building].ContainsNode(startNode) && buildingBuffer[building].ContainsNode(endNode))
                        {
                            return building;
                        }
                        building = buildingBuffer[building].m_nextGridBuilding;
                        if (++count >= 49152)
                        {
                            CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                            break;
                        }
                    }
                }
            }
            return result;
        }

        private bool isLeft(Vector3 P0, Vector3 P1, Vector3 P2)
        {
            return ((P1.x - P0.x) * (P2.z - P0.z) - (P2.x - P0.x) * (P1.z - P0.z)) > 0;
        }

        private bool PointInRectangle(Quad3 rectangle, Vector3 p)
        {
            return isLeft(rectangle.a, rectangle.b, p) && isLeft(rectangle.b, rectangle.c, p) && isLeft(rectangle.c, rectangle.d, p) && isLeft(rectangle.d, rectangle.a, p);
        }

        private ItemClass.Layer GetItemLayers()
        {
            ItemClass.Layer itemLayers = ItemClass.Layer.Default;

            if (InfoManager.instance.CurrentMode == InfoManager.InfoMode.Water)
            {
                itemLayers = itemLayers | ItemClass.Layer.WaterPipes;
            }

            if (InfoManager.instance.CurrentMode == InfoManager.InfoMode.Traffic || InfoManager.instance.CurrentMode == InfoManager.InfoMode.Transport)
            {
                itemLayers = itemLayers | ItemClass.Layer.MetroTunnels;
            }

            if (InfoManager.instance.CurrentMode == InfoManager.InfoMode.Underground)
            {
                itemLayers = ItemClass.Layer.MetroTunnels;
            }

            return itemLayers;
        }

        private bool IsDecal(PropInfo prop)
        {
            if (prop != null && prop.m_material != null)
            {
                return (prop.m_material.shader == shaderBlend || prop.m_material.shader == shaderSolid);
            }

            return false;
        }

        private bool IsBuildingValid(ref Building building, ItemClass.Layer itemLayers)
        {
            if ((building.m_flags & Building.Flags.Created) == Building.Flags.Created)
            {
                return (building.Info.m_class.m_layer & itemLayers) != ItemClass.Layer.None;
            }

            return false;
        }

        private bool IsNodeValid(ref NetNode node, ItemClass.Layer itemLayers)
        {
            if ((node.m_flags & NetNode.Flags.Created) == NetNode.Flags.Created)
            {
                return (node.Info.GetConnectionClass().m_layer & itemLayers) != ItemClass.Layer.None;
            }

            return false;
        }

        private bool IsSegmentValid(ref NetSegment segment, ItemClass.Layer itemLayers)
        {
            if ((segment.m_flags & NetSegment.Flags.Created) == NetSegment.Flags.Created)
            {
                return (segment.Info.GetConnectionClass().m_layer & itemLayers) != ItemClass.Layer.None;
            }

            return false;
        }

        private static bool RayCastNode(ref NetNode node, Segment3 ray, float snapElevation, out float t, out float priority)
        {
            NetInfo info = node.Info;
            float num = (float)node.m_elevation + info.m_netAI.GetSnapElevation();
            float t2;
            if (info.m_netAI.IsUnderground())
            {
                t2 = Mathf.Clamp01(Mathf.Abs(snapElevation + num) / 12f);
            }
            else
            {
                t2 = Mathf.Clamp01(Mathf.Abs(snapElevation - num) / 12f);
            }
            float collisionHalfWidth = Mathf.Max(3f, info.m_netAI.GetCollisionHalfWidth());
            float num2 = Mathf.Lerp(info.GetMinNodeDistance(), collisionHalfWidth, t2);
            if (Segment1.Intersect(ray.a.y, ray.b.y, node.m_position.y, out t))
            {
                float num3 = Vector3.Distance(ray.Position(t), node.m_position);
                if (num3 < num2)
                {
                    priority = Mathf.Max(0f, num3 - collisionHalfWidth);
                    return true;
                }
            }
            t = 0f;
            priority = 0f;
            return false;
        }
    }
}

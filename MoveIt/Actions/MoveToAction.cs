using UnityEngine;

namespace MoveIt
{
    public class MoveToAction : BaseTransformAction
    {
        internal Vector3 Original, Position;
        internal float AngleOriginal, Angle;
        internal bool AngleActive, HeightActive;
    }
}

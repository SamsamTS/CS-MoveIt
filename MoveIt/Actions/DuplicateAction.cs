using UnityEngine;
using ColossalFramework;
using System.Collections.Generic;

namespace MoveIt
{
    public class DuplicateAction : CloneAction
    {
        public DuplicateAction() : base()
        {
            angleDelta = 0f;
            moveDelta = Vector3.zero;
        }
    }
}

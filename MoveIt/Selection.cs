using UnityEngine;

using System.Xml.Serialization;

namespace MoveIt
{
    public class Selection
    {
        public Vector3 center;

        [XmlElement("state")]
        public InstanceState[] states;
    }
}

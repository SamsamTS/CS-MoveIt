using System.Xml.Serialization;
using UnityEngine;

namespace MoveIt
{
    public class Selection
    {
        public Vector3 center;
        public string version;
        public bool includesPO;

        [XmlElement("state")]
        public InstanceState[] states;
    }
}

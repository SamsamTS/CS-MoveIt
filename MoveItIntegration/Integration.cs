using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoveItIntegration
{
    public interface IMoveItIntegrationFactory
    {
        string Name { get; }
        string Description { get; }
        IMoveItIntegration GetInstance();
    }

    /// <summary>
    /// implementation of IMoveItIntegrationFactory is required to get instance of IMoveItIntegration.
    /// </summary>
    public interface IMoveItIntegration
    {
        /// <summary>
        /// unique ID to identify the integration. must not change for the sake of backward compatibility.
        /// </summary>
        string ID { get; }

        /// <summary>
        /// the version of data that can be read later for backward compatibility.
        /// </summary>
        Version DataVersion { get; }

        /// <summary>converts data to base 64 string.</summary>
        /// <param name="record">record returned by <see cref="CopyNode(ushort)"/> 
        /// or <see cref="CopySegment(ushort)"/></param>
        string Encode64(object record);

        /// <summary>decode the record encoded by <see cref="Encode64(object)".</summary>
        /// <param name="dataVersion"><see cref="DataVersion"/> when data was stored</param>
        object Decode64(string base64Data, Version dataVersion);

        object CopySegment(ushort sourceSegmentID);
        object CopyNode(ushort sourceNodeID);

        /// <summary>Paste segment data</summary>
        /// <param name="record">data returned by <see cref="CopySegment(ushort)"/></param>
        /// <param name="map">a dictionary of source instance ID to target instance ID.
        /// this maps all the nodes, segments and lanes. 
        /// please contact mod owner if you need buildings, props, etc to be mapped as well</param>
        void PasteSegment(ushort targetSegmentID, object record, Dictionary<InstanceID, InstanceID> map);


        /// <summary>Paste node data</summary>
        /// <param name="record">data returned by <see cref="CopyNode(ushort)(ushort)"/></param>
        /// <param name="map">a dictionary of source instance ID to target instance ID.
        /// this maps all the nodes, segments and lanes. 
        /// please contact mod owner if you need buildings, props, etc to be mapped as well</param>
        void PasteNode(ushort targetNodeID, object record, Dictionary<InstanceID, InstanceID> map);
    }

    public static class IntegrationHelper
    {
        public static List<IMoveItIntegration> GetIntegrations()
        {
            var integrations = new List<IMoveItIntegration>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetExportedTypes())
                    {
                        if (type.IsClass && typeof(IMoveItIntegrationFactory).IsAssignableFrom(type))
                        {
                            var factory = (IMoveItIntegrationFactory)Activator.CreateInstance(type);
                            var instance = factory.GetInstance();
                            integrations.Add(instance);
                        }
                    }
                }
                catch { }
            }

            return integrations;
        }
    }
}

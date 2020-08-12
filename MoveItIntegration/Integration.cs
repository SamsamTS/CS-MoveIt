using System;
using System.Collections.Generic;

namespace MoveItIntegration
{
    public interface IMoveItIntegrationFactory
    {

        MoveItIntegrationBase GetInstance();
    }

    /// <summary>
    /// implementation of <see cref="IMoveItIntegrationFactory"/> is required to get instance of this class.
    /// </summary>
    public abstract class MoveItIntegrationBase
    {
        /// <summary>
        /// unique ID to identify the integration. must not change for the sake of backward compatibility.
        /// </summary>
        public abstract string ID { get; }

        /// <summary>
        /// (future feature)
        /// Display name in move it options. if null, The integration will not be added to the MoveIT options.
        /// </summary>
        public virtual string Name => null;

        /// <summary>
        /// (future feature)
        /// Description of the integration item in move it options. if null, no description is displayed.
        /// </summary>
        public virtual string Description => null;

        /// <summary>
        /// the version of data that can be read later for backward compatibility.
        /// </summary>
        public abstract Version DataVersion { get; }

        public abstract object Copy(InstanceID sourceInstanceID);

        /// <summary>Paste segment data</summary>
        /// <param name="record">data returned by <see cref="CopySegment(ushort)"/></param>
        /// <param name="map">a dictionary of source instance ID to target instance ID.
        /// this maps all the nodes, segments and lanes. 
        /// please contact mod owner if you need buildings, props, etc to be mapped as well</param>
        ///         public abstract object Paste(InstanceID instanceID);
        public abstract object Paste(InstanceID targetrInstanceID, object record, Dictionary<InstanceID, InstanceID> map);


        /// <summary>converts data to base 64 string.</summary>
        /// <param name="record">record returned by <see cref="Copy(ushort)"/> </param>
        public abstract string Encode64(object record);

        /// <summary>decode the record encoded by <see cref="Encode64(object)".</summary>
        /// <param name="dataVersion"><see cref="DataVersion"/> when data was stored</param>
        public abstract object Decode64(string base64Data, Version dataVersion);

    }

    public static class IntegrationHelper
    {
        public static List<MoveItIntegrationBase> GetIntegrations()
        {
            var integrations = new List<MoveItIntegrationBase>();

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

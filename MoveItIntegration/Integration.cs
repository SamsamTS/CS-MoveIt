using System;
using System.Collections.Generic;

namespace MoveItIntegration
{
    /// <summary>
    /// Factory class to get the <see cref="MoveItIntegrationBase"/> instance
    /// </summary>
    public interface IMoveItIntegrationFactory
    {
        /// <summary>
        /// Get the <see cref="MoveItIntegrationBase"/> instance
        /// </summary>
        /// <returns>Instance that handles integration</returns>
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
        /// The version of data that can be read later for backward compatibility.
        /// </summary>
        public abstract Version DataVersion { get; }

        /// <summary>Copy object data</summary>
        /// <param name="sourceInstanceID"><see cref="InstanceID"/> of object being cloned</param>
        public abstract object Copy(InstanceID sourceInstanceID);

        /// <summary>Paste object data</summary>
        /// <param name="targetInstanceID"><see cref="InstanceID"/> of new object</param>
        /// <param name="record">data returned by <see cref="Copy(InstanceID)"/></param>
        /// <param name="map">a dictionary of source instance ID to target instance ID.
        /// this maps all the nodes, segments and lanes. 
        /// please contact mod owner if you need buildings, props, etc to be mapped as well</param>
        public abstract void Paste(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> map);

        /// <summary>Paste object data, with segment ends needing reversed</summary>
        /// <param name="targetInstanceID"><see cref="InstanceID"/> of new object</param>
        /// <param name="record">data returned by <see cref="Copy(InstanceID)"/></param>
        /// <param name="map">a dictionary of source instance ID to target instance ID.
        /// this maps all the nodes, segments and lanes. 
        /// please contact mod owner if you need buildings, props, etc to be mapped as well</param>
        public virtual void Mirror(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> map)
        {
            Paste(targetInstanceID, record, map);
        }

        /// <summary>Converts data to base 64 string.</summary>
        /// <param name="record">record returned by <see cref="Copy(InstanceID)"/> </param>
        public abstract string Encode64(object record);

        /// <summary>Decode the record encoded by <see cref="Encode64(object)"/>.</summary>
        /// <param name="base64Data">The base 64 string that was encoded in <see cref="Encode64(object)"/></param>
        /// <param name="dataVersion"><see cref="DataVersion"/> when data was stored</param>
        public abstract object Decode64(string base64Data, Version dataVersion);
    }

    /// <summary>
    /// Used by Move It to find integrated mods
    /// </summary>
    public static class IntegrationHelper
    {
        /// <summary>
        /// Search for mods with Move It integration (assemblies which contain <see cref="MoveItIntegrationBase"/> implementations
        /// </summary>
        /// <returns>List of <see cref="MoveItIntegrationBase"/> instances, one from each integrationed mod</returns>
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

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
    public interface IMoveItIntegration
    {
        string Encode64(object record);
        object Decode64(string base64Data);

        object CopySegment(ushort segmentId);
        void PasteSegment(ushort segmentId, object record, Dictionary<InstanceID, InstanceID> map);

        object CopyNode(ushort nodeID);
        void PasteNode(ushort nodeID, object record);
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

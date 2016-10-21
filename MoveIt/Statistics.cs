using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoveIt
{
    public class Statistics
    {
        public class stats
        {
            public long start;

            public long totalCount;
            public int count;

            long average
            {
                get
                {
                    return totalCount / count;
                }
            }
        }

        public static Dictionary<string, stats> counters = new Dictionary<string,stats>();

        public static void StartCounter(string name)
        {
            lock (counters)
            {
                if (!counters.ContainsKey(name))
                {
                    counters.Add(name, new stats());
                }

                stats stat = counters[name];
                if (stat.start == 0)
                {
                    stat.start = Stopwatch.GetTimestamp();
                }
            }
        }

        public static void StopCounter(string name)
        {
            lock (counters)
            {
                if (counters.ContainsKey(name))
                {
                    stats stat = counters[name];
                    if (stat.start > 0)
                    {
                        stat.totalCount = stat.totalCount + (Stopwatch.GetTimestamp() - stat.start);
                        stat.count++;
                        stat.start = 0;
                    }
                }
            }
        }
    }
}

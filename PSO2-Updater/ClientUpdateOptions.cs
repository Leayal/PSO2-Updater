using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.PSO2.Updater
{
    public class ClientUpdateOptions : IDisposable
    {
        public ClientUpdateOptions()
        {
            this.Profile = UpdaterProfile.Balanced;
            this.ChecksumCache = null;
            this.ParallelOptions = new ParallelOptions();
            // Limit default to 4 or lower
            this.MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4);
        }

        public UpdaterProfile Profile { get; set; }
        public ChecksumCache.ChecksumCache ChecksumCache { get; set; }
        internal ParallelOptions ParallelOptions { get; set; }
        public int MaxDegreeOfParallelism { get => this.ParallelOptions.MaxDegreeOfParallelism; set => this.ParallelOptions.MaxDegreeOfParallelism = value; }

        public void Dispose()
        {
            this.ChecksumCache.Dispose();
        }
    }
}

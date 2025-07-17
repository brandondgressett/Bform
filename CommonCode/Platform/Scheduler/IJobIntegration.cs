using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFormDomain.CommonCode.Platform.Scheduler
{
    public interface IJobIntegration
    {
        public Task Execute();
        
    }
}

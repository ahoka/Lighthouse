using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lighthouse.Configuration
{
    public class RaftConfiguration
    {
        public Uri Address { get; set; }
        public IEnumerable<Uri> Join { get; set; }
    }
}

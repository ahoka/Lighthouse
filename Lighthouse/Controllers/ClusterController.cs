using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Lighthouse.State;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/[controller]")]
    [ApiController]
    public class ClusterController : ControllerBase
    {
        private Cluster Cluster { get; }

        public ClusterController(Cluster cluster)
        {
            Cluster = cluster;
        }

        [HttpGet("members")]
        public IActionResult Members()
        {
            return Ok(new
            {
                Members = Cluster.Members.Select(m => new { Id = m.NodeId, Address = m.Address })
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Doikham.API.Data;
using Doikham.Shared.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Doikham.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MasterController : ControllerBase
    {
        private IMaster repo { get; set; }

        public MasterController(IMaster master)
        {
            repo = master;
        }

        [HttpPost("[action]")]
        public async Task<ActionResult> Sync()
        {
            RESPONSE response = new RESPONSE();
            try
            {
                await Task.Run(() => repo.SyncMaster());
                response.TYPE = "S";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = "Success";
                return Ok(response);
            }
            catch (Exception e)
            {
                response.TYPE = "E";
                response.PIMSGID = DateTime.Now.ToString("yyyyMMddHHmmss");
                response.MESSAGE = e.Message;
                return StatusCode((int)HttpStatusCode.InternalServerError, response);
            }
        }

    }
}
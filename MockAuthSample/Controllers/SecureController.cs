using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AccountManager.Controllers
{
    [ApiController]
    public class SecureController : ControllerBase
    {
        // GET: api/Secure
        [HttpGet("Secure")]
        [Authorize]
        public ActionResult GetSecure()
        {
            return Ok();
        }
    }
}

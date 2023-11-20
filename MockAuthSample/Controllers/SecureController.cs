using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

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
            var user = Request.HttpContext.User;

            foreach (var c in user.Claims) {
                Debug.WriteLine($"GetSecure: {c.Type} -> {c.Value}");
            }

            return Ok();
        }
    }
}

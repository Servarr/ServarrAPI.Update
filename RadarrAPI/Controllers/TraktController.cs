using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RadarrAPI.Database;
using RadarrAPI.Database.Models;
using TraktApiSharp;
using TraktApiSharp.Exceptions;

namespace RadarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class TraktController : Controller
    {
        private readonly DatabaseContext _database;

        private readonly TraktClient _trakt;

        public TraktController(DatabaseContext database, TraktClient trakt)
        {
            _database = database;
            _trakt = trakt;
        }

        [Route("redirect")]
        [HttpGet]
        public async Task<IActionResult> RedirectToTrakt([FromQuery(Name = "target")] string target)
        {
            var validTarget = Uri.TryCreate(target, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == "http" || uriResult.Scheme == "https");
            if (!validTarget)
            {
                return BadRequest("Invalid target specified.");
            }

            var traktEntity = new TraktEntity
            {
                State = Guid.NewGuid(),
                Target = target,
                CreatedAt = DateTime.UtcNow
            };

            _database.Add(traktEntity);
            await _database.SaveChangesAsync();
            
            return Redirect(_trakt.OAuth.CreateAuthorizationUrl(_trakt.ClientId, GetRedirectUri(), traktEntity.State.ToString()));
        }

        [Route("callback")]
        [HttpGet]
        public async Task<IActionResult> TraktCallback([FromQuery(Name = "code")] string code, [FromQuery(Name = "state")] string stateStr)
        {
            if (!Guid.TryParse(stateStr, out Guid state))
            {
                return BadRequest("Invalid state specified.");
            }

            var traktEntity = _database.TraktEntities.FirstOrDefault(x => x.State.Equals(state));
            if (traktEntity == null)
            {
                return BadRequest("Unknown state specified.");
            }

            _database.Remove(traktEntity);
            await _database.SaveChangesAsync();

            var traktAuth = await _trakt.OAuth.GetAuthorizationAsync(code, _trakt.ClientId, _trakt.ClientSecret, GetRedirectUri());
            if (!traktAuth.IsValid)
            {
                return BadRequest("Received trakt token was invalid.");
            }

            return Redirect($"{traktEntity.Target}?access={traktAuth.AccessToken}&refresh={traktAuth.RefreshToken}");
        }

        [Route("refresh")]
        [HttpGet]
        public async Task<IActionResult> TraktCallback([FromQuery(Name = "refresh")] string refresh)
        {
            if (string.IsNullOrWhiteSpace(refresh))
            {
                return BadRequest("Invalid refresh code specified.");
            }
            
            try
            {
                var traktAuth = await _trakt.OAuth.RefreshAuthorizationAsync(refresh, _trakt.ClientId, _trakt.ClientSecret, GetRedirectUri());
                if (!traktAuth.IsValid)
                {
                    return BadRequest("Received trakt token was invalid.");
                }

                return Ok(traktAuth);
            }
            catch (TraktAuthenticationException e)
            {
                return StatusCode(401, new
                {
                    Message = "Invalid refresh token specified.",
                    MessageTrakt = e.Message.Replace("\n", ", ")
                });
            }
        }

        private string GetRedirectUri()
        {
            var redirectUri = new StringBuilder();
            redirectUri.Append(HttpContext.Request.IsHttps ? "https://" : "http://");
            redirectUri.Append(HttpContext.Request.Host);
            redirectUri.Append("/v1/trakt/callback");

            return redirectUri.ToString();
        }
    }
}

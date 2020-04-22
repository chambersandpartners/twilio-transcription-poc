using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TwilioMediaStreams.Models;
using Twilio.TwiML;
using Twilio.TwiML.Voice;

namespace TwilioMediaStreams.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasicController : ControllerBase
    {
        private readonly ProjectSettings _projectSettings;

        public BasicController (IOptions<ProjectSettings> projectSettings)
        {
            _projectSettings = projectSettings.Value;
        }

        [HttpGet]
        [Route("/handshake")]
        public IActionResult HandShake()
        {
            var response = new VoiceResponse();
            var start = new Start();
            var stream = new Stream(url: _projectSettings.TwilioMediaStreamWebhookUri, track: Stream.TrackEnum.BothTracks);
            start.Append(stream);
            response.Append(start);

            var say = new Say("Please record a new message.");
            response.Append(say);
            response.Pause(length: 60);

            // TODO: Dial outgoing number
            //var dial = new Dial(number: "+44");
            //response.Append(dial);

            return new Twilio.AspNet.Core.TwiMLResult(response.ToString());
        }
    }
}
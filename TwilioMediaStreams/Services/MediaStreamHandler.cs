using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using g711audio;
using Microsoft.Extensions.Options;

using WebSocketManager;
using TwilioMediaStreams.Models;

namespace TwilioMediaStreams.Services
{
    public class MediaStreamHandler : WebSocketHandler
    {
        private readonly ProjectSettings _projectSettings;

        private Dictionary<string, TranscriptionEngine> _transcriptionEngines = new Dictionary<string, TranscriptionEngine>();
      
        public MediaStreamHandler(WebSocketConnectionManager webSocketConnectionManager, IOptions<ProjectSettings> projectSettings) : base(webSocketConnectionManager)
        {
            _projectSettings = projectSettings.Value;
        }

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);
            string socketId = WebSocketConnectionManager.GetId(socket);
            AddSocketTranscriptionEngine(socketId);
        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            string socketId = WebSocketConnectionManager.GetId(socket);

            using (JsonDocument jsonDocument = JsonDocument.Parse(Encoding.UTF8.GetString(buffer, 0, result.Count)))
            {
                string eventMessage = jsonDocument.RootElement.GetProperty("event").GetString();

                switch (eventMessage)
                {
                    case "connected":
                        break;
                    case "start":
                        await StartSpeechTranscriptionEngine(socketId);
                        break;
                    case "media":
                        string payload = jsonDocument.RootElement.GetProperty("media").GetProperty("payload").GetString();
                        //string sequenceNumber = jsonDocument.RootElement.GetProperty("media").GetProperty("chunk").GetString();
                        //await WritePayloadToDisk(sequenceNumber, payload);
                        await ProcessAudioForTranscriptionAsync(socketId, payload);
                        break;
                    case "stop":
                        await OnConnectionFinishedAsync(socket, socketId);
                        break;
                }
            }
        }

        private async Task WritePayloadToDisk(string sequenceNumber, string payload)
        {
            var path = $"TwilioOutput/{sequenceNumber}.txt";
            await File.WriteAllTextAsync(path, payload);
        }

        private void AddSocketTranscriptionEngine(string socketId)
        {
            var transcriptionEngine = new TranscriptionEngine(socketId, _projectSettings);
            _transcriptionEngines.Add(socketId, transcriptionEngine);
        }

        private async Task StartSpeechTranscriptionEngine(string socketId)
        {
            var transcriptionEngine = GetSocketTranscriptionEngine(socketId);
            await transcriptionEngine.Start();
        }

        private async Task ProcessAudioForTranscriptionAsync(string socketId, string payload)
        {
            byte[] payloadByteArray = Convert.FromBase64String(payload);
            byte[] decoded;

            MuLawDecoder.MuLawDecode(payloadByteArray, out decoded);
            var transcriptionEngine = GetSocketTranscriptionEngine(socketId);
            await transcriptionEngine.Transcribe(decoded);

        }

        private async Task OnConnectionFinishedAsync(WebSocket socket, string socketId)
        {
            // instruct the server to actually close the socket connection
            await OnDisconnected(socket);

            // clean up
            var transcriptionEngine = GetSocketTranscriptionEngine(socketId);
            await transcriptionEngine.Stop();
            _transcriptionEngines.Remove(socketId);
        }

        private TranscriptionEngine GetSocketTranscriptionEngine(string socketId)
        {
            var speechClient = _transcriptionEngines[socketId];

            if (speechClient == null)
            {
                throw new Exception($"Cannot find socket with Id: {socketId}");
            }

            return speechClient;
        }
    }
}

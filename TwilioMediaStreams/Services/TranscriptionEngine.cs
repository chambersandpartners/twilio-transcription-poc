using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Options;
using TwilioMediaStreams.Models;

namespace TwilioMediaStreams.Services
{
    public class TranscriptionEngine : IDisposable
    {
        private readonly ProjectSettings _projectSettings;
        private string _socketId;
        private SpeechRecognizer _recognizer;
        private PushAudioInputStream _inputStream;
        private AudioConfig _audioInput;

        public TranscriptionEngine(string socketId, ProjectSettings projectSettings)
        {
            _projectSettings = projectSettings;
            _socketId = socketId;
        }

        public async Task Start()
        {
            var config = SpeechConfig.FromSubscription(_projectSettings.AzureSpeechServiceSubscriptionKey, _projectSettings.AzureSpeechServiceRegionName);
            
            // using MULAW as Twilio uses this format
            var audioFormat = AudioStreamFormat.GetCompressedFormat(AudioStreamContainerFormat.MULAW);

            _inputStream = AudioInputStream.CreatePushStream(audioFormat);
            _audioInput = AudioConfig.FromStreamInput(_inputStream);

            _recognizer = new SpeechRecognizer(config, _audioInput);
            _recognizer.SessionStarted += RecognizerStarted;
            _recognizer.Recognized += RecognizerRecognized;
            _recognizer.Canceled += RecognizerCancelled;

            await _recognizer.StartContinuousRecognitionAsync();
        }

        public async Task Transcribe(byte[] audioBytes)
        {
            _inputStream.Write(audioBytes);
        }

        private void RecognizerCancelled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            var cancellation = CancellationDetails.FromResult(e.Result);
            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

            if (cancellation.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                Console.WriteLine($"CANCELED: Did you update the subscription info?");
            }
        }

        private void RecognizerStarted(object sender, SessionEventArgs e)
        {
            Debug.WriteLine($"{e.SessionId} > Starting");
        }

        private void RecognizerRecognized(object sender, SpeechRecognitionEventArgs e)
        {
            Debug.WriteLine($"{e.SessionId} > Final result: {e.Result.Text}");
        }

        public async Task Stop()
        {
            _inputStream.Close();
            if (_recognizer != null)
            {
                _recognizer.SessionStarted -= RecognizerStarted;
                _recognizer.Recognized -= RecognizerRecognized;
                await _recognizer.StopContinuousRecognitionAsync();
            }
        }

        public void Dispose()
        {
            _inputStream.Dispose();
            _audioInput.Dispose();
            _recognizer?.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
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

        public int counter { get; set; }
        private Dictionary<string, List<byte[]>> _dictionaryTempByteList = new Dictionary<string, List<byte[]>>();

        public TranscriptionEngine(string socketId, ProjectSettings projectSettings)
        {
            _projectSettings = projectSettings;
            _socketId = socketId;
            _dictionaryTempByteList.Add(socketId, new List<byte[]>());
        }

        public async Task Start()
        {
            var config = SpeechConfig.FromSubscription(_projectSettings.AzureSpeechServiceSubscriptionKey, _projectSettings.AzureSpeechServiceRegionName);

            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(8000, 16, 1);

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
            _dictionaryTempByteList[_socketId].Add(audioBytes);

            if (counter % 20 == 0)
            {
                byte[] completeAudioBuffer = CreateAudioByteArray(_dictionaryTempByteList);

                _inputStream.Write(completeAudioBuffer);

                _dictionaryTempByteList[_socketId].Clear();
            }

            counter++;
        }

        private void RecognizerCancelled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            var cancellation = CancellationDetails.FromResult(e.Result);
            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

            if (cancellation.Reason == CancellationReason.Error)
            {
                Debug.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                Debug.WriteLine($"CANCELED: Did you update the subscription info?");
            }
        }

        private void RecognizerStarted(object sender, SessionEventArgs e)
        {
            Debug.WriteLine($"{e.SessionId} > Starting");
        }

        private void RecognizerRecognized(object sender, SpeechRecognitionEventArgs e)
        {
            // Will eventually push to users screen via SignalR
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

        private byte[] CreateAudioByteArray(Dictionary<string, List<byte[]>> dictionaryByteList)
        {
            //get the relevant dictionary entry
            List<byte[]> byteList = dictionaryByteList[_socketId];

            //create new byte array that will represent the "flattened" array
            List<byte> completeAudioByteArray = new List<byte>();

            foreach (byte[] byteArray in byteList)
            {
                foreach (byte singleByte in byteArray)
                {
                    completeAudioByteArray.Add(singleByte);
                }
            }

            //collate the List<T> of byte arrays into a single large byte array
            byte[] buffer = completeAudioByteArray.ToArray();
            return buffer;
        }
    }
}

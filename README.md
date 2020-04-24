This solution tries to take Twilio audio stream and convert it to text via Azure Cognitive Speech Service.

** Twilio & .Net Connection Manager taken from
https://blogs.siliconorchid.com/post/coding-inspiration/twilio-mediastreams/
** Speech API examples taken from  
https://github.com/Azure-Samples/cognitive-services-speech-sdk/blob/master/samples/csharp/sharedcontent/console/speech_recognition_samples.cs
https://www.nexmo.com/blog/2019/12/30/building-a-real-time-net-transcription-service-dr
https://codez.deedx.cz/posts/continuous-speech-to-text/


As Twilio sends MULAW audio and Azure Speech API only accepts WAV/PCM we are trying to convert via the newly supported compression on Windows by Azure.
https://docs.microsoft.com/bs-latn-ba/azure/cognitive-services/Speech-Service/how-to-use-codec-compressed-audio-input-streams?pivots=programming-language-csharp&tabs=debian

Setup:

1. Required to have GStreamer for Windows, download https://gstreamer.freedesktop.org/data/pkg/windows/1.14.4/gstreamer-1.0-x86_64-1.14.4.msi and install. Note the target location!
Add the GStreamer bin directory to your system path. For example, if GStreamer was installed to C:\gstreamer then add C:\gstreamer\1.0\x86_64\bin to PATH (open Control Panel, go to System - Advanced system settings - Environment Variables, and modify PATH).
2. Add Azure Cognitive Speech Service (there is a Free Tier) API key and region to appsettings.json
3. Add your own NGrok key to TwilioMediaStreamWebhookUri in appsettings.json
4. Add a webhook in your phone number in Twilio to http://<YOUR-NGNROK>.ngrok.io/handshake
5. Call the number and will see audio flowing through to Speech Service

Note: to simplify testing have added a Say to Handshake in BasicController but but need the Dial below it to replace it to test a 2-way conversation.





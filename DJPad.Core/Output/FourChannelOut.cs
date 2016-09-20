namespace DJPad.Output.DirectSound
{
    using System.Collections.Generic;
    using SharpDX.DirectSound;

    public class FourChannelOut : BaseOutput
    {
        public FourChannelOut()
        {
            this.Init();
        }

        private void Init()
        {
            List<DeviceInformation> devices = DirectSound.GetDevices();
            var device = new DirectSound(devices[0].DriverGuid);

            SpeakerConfiguration configuration;
            SpeakerGeometry geometry;

            device.GetSpeakerConfiguration(out configuration, out geometry);
        }

        public void Play()
        {
        }

        public void Stop()
        {
        }
    }
}
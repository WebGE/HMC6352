using System;
using System.Threading;
using Microsoft.SPOT;

using testMicroToolsKit.Hardware.Sensors;

namespace Netduino
{
    public class Program
    {
        public static void Main()
        {
            byte sla = 0x21;
            UInt16 I2Cfrequency = 100; // kHz

            HMC6352 compass = new HMC6352(sla, I2Cfrequency);

            float lastHeading = (int)compass.GetHeading();
            byte ver = compass.ReadEeprom(HMC6352.EEPROMAddress.SoftwareVersion);
            Debug.Print("Software Version = " + ver.ToString());
            byte i2cAddr = compass.ReadEeprom(HMC6352.EEPROMAddress.SlaveAddress);
            Debug.Print("Slave Address = " + i2cAddr.ToString());

            HMC6352.OperationalMode mode = compass.GetOperationalMode();
            Debug.Print("Operational Mode = " + mode.ToString());

            HMC6352.Frequency frequency = compass.GetFrequency();
            Debug.Print("Frequency = " + frequency.ToString());

            Thread.Sleep(2000);

            while (true)
            {
                
                float heading = compass.GetHeading();

                if (heading != lastHeading)
                {
                    lastHeading = heading;
                    Debug.Print(heading.ToString("N2"));
                }

                Thread.Sleep(500);

            }
        }

    }
}

using System;

using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using BigBlueSparx.NetMF.FEZ.HMC6352Compass;
using BigBlueSparx.NetMF.FEZ;

namespace NetduinoHM6352
{
    public class Program
    {
        public static void Main()
        {
            // Paramètres du bus I2C
            byte addCompass_I2C = 0x21; // Adresse (7 bits) du circuit HMC6352
            UInt16 Freq = 100;

            // Création d'un objet boussole HMC6352
            HMC6352Compass compass = new HMC6352Compass(addCompass_I2C, Freq);

            // Affichage de la version du software et de l'adresse de la boussole
            float lastHeading = (int)compass.GetHeading();
            byte ver = compass.ReadEeprom(HMC6352Compass.EEPROMAddress.SoftwareVersion);
            Debug.Print("Software Version = " + ver.ToString());
            byte i2cAddr = compass.ReadEeprom(HMC6352Compass.EEPROMAddress.SlaveAddress);
            Debug.Print("Slave Address = " + i2cAddr.ToString());

            HMC6352Compass.OperationalMode mode = compass.GetOperationalMode();
            Debug.Print("Operational Mode = " + mode.ToString());

            HMC6352Compass.Frequency frequency = compass.GetFrequency();
            Debug.Print("Frequency = " + frequency.ToString());

            Thread.Sleep(2000);

            while (true)
            {
                Thread.Sleep(500);

                float heading = compass.GetHeading();

                if (heading != lastHeading)
                {
                    lastHeading = heading;
                    Debug.Print(heading.ToString("N2"));
                }
            } 
        }
    }
}

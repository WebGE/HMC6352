using System;
using System.Threading;
using Microsoft.SPOT;

using ToolBoxes;


namespace TestNetduinoHMC6352
{
    public class Program
    {
        public static void Main()
        {
            // Paramètres du bus I2C
            byte addCompass_I2C = 0x21; // Adresse (7 bits) du circuit HMC6352
            UInt16 Freq = 100;

            // Création d'un objet boussole HMC6352
            HMC6352 compass = new HMC6352(addCompass_I2C, Freq);

            // Affichage de la version du software et de l'adresse de la boussole
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

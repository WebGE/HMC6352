using System;
using Microsoft.SPOT.Hardware;

namespace ToolBoxes
{
    public class HMC6352
    {
        /* Compass Modes: 
         *  Standby: Factory Default.  Measurements are made and the results are returned AFTER the next read command.
         *  Query: Measurements are made and data registers are updated.  A new measurement is started immediately.
         *  Continuous: Continuous measurements are performed and the most recent result are available in the registers.
         * */
        // Attributs
        public enum OperationalMode { Standby = 0, Query = 1, Continuous = 2 }

        /* Continuous mode frequency: 
         *  The frequency of measurements may be set to 1, 5, 10, or 20 Hz
         */

        public enum Frequency { f1Hz = 0, f5Hz = 1, f10Hz = 2, f20Hz = 3 }

        /* Output Data Modes:
         *  Heading: Report 1/10th degrees from 0 to 3599
         *  RawX: ADC Voltage mesurements
         *  RawY: ADC Voltage mesurements
         *  MagnetometerX: Compensated ADC Voltage mesurements
         *  MagnetometerY: Compensated ADC Voltage mesurements
         */

        public enum OutputMode { Heading = 0, RawX = 1, RawY = 2, MagnetometerX = 3, MagnetometerY = 4 }

        /* Commands:
         *  w - write to EEPROM
         *  r - read from EEPROM
         *  G - write to RAM
         *  g - read from RAM
         *  S - (Sleep) Enter Sleep Mode
         *  W - (Wake Up) Exit Sleep Mode 
         *  O - Update bridge offsets
         *  C - Enter Callibration Mode
         *  E - Exit Callibration Mode
         *  L - Save Operation Mode to RAM
         *  A - Get Data, Compensate and calculate new heading
         */

        public enum Command
        {
            WriteEEPROM = 0x77, ReadEEPROM = 0x72,
            WriteRAM = 0x47, ReadRAM = 0x67,
            Sleep = 0x53, WakeUp = 0x57, UpdateOffsets = 0x4f,
            StartCallibration = 0x43, EndCallibration = 0x45,
            SaveOperationMode = 0x4c, GetData = 0x41
        }

        /* EEPROM Contents */
        public enum EEPROMAddress
        {
            SlaveAddress = 0,
            MagXOffsetMSB = 1,
            MagXOffsetLSB = 2,
            MagYOffsetMSB = 3,
            MagYOffsetLSB = 4,
            TimeDelay = 5,
            NumberOfMeasurements = 6,
            SoftwareVersion = 7,
            OperationalModeByte = 8
        }

        // Private
        private I2CDevice BusI2C;
        I2CDevice.Configuration ConfigHM6352;
        I2CDevice.I2CTransaction[] myI2Command = new I2CDevice.I2CTransaction[2];
        byte[] myReadBuffer = new byte[2];
        byte[] myReadEEPROM = new byte[1];
        int myBytesTransmitted = 0;


        // this constructor assumes the default factory 2*Slave Address + R/W = 0x42
        public HMC6352()
        {
            ConfigHM6352 = new I2CDevice.Configuration(0x21, 100);
        }


        // This constructor allows user to specify the Slave Address, bus frequency = 100khz 
        public HMC6352(UInt16 I2C_Add_7bits)
        {
            ConfigHM6352 = new I2CDevice.Configuration(I2C_Add_7bits, 100);           
        }

        // This constructor allows user to specify the Slave Address and bus frequency
        public HMC6352(UInt16 I2C_Add_7bits, UInt16 FreqBusI2C)
        {
            ConfigHM6352 = new I2CDevice.Configuration(I2C_Add_7bits, FreqBusI2C);
        }

        public float GetHeading()
        {
            myI2Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte) Command.GetData });
            myI2Command[1] = I2CDevice.CreateReadTransaction(myReadBuffer);
            // Exécution de la transaction
            BusI2C = new I2CDevice(ConfigHM6352); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
            myBytesTransmitted = BusI2C.Execute(myI2Command, 100);
            BusI2C.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
            float heading = ((myReadBuffer[0] << 8) + myReadBuffer[1]) / 10f;
            return (heading);
        }

        // After starting Callibration, the user should rotate the sensor through 2 or more complete rotations.
        // The optimal time is 2 rotations over 20 seconds.
        public void StartCalibration()
        {
            myI2Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.StartCallibration });
            // Exécution de la transaction
            BusI2C = new I2CDevice(ConfigHM6352); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
            myBytesTransmitted = BusI2C.Execute(myI2Command, 100);
            BusI2C.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
        }

        // Call this method to end the callibration Sequence.
        public void EndCallibration()
        {
            myI2Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.EndCallibration });
            // Exécution de la transaction
            BusI2C = new I2CDevice(ConfigHM6352); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
            myBytesTransmitted = BusI2C.Execute(myI2Command, 100);
            BusI2C.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
        }

        public byte ReadEeprom(EEPROMAddress addr)
        {
            myI2Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.ReadEEPROM, (byte) addr });
            myI2Command[1] = I2CDevice.CreateReadTransaction(myReadEEPROM);
            // Exécution de la transaction
            BusI2C = new I2CDevice(ConfigHM6352); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
            myBytesTransmitted = BusI2C.Execute(myI2Command, 100);
            BusI2C.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
            return myReadEEPROM[0];

        }

        /* There is no generic public Write EEPROM command.   
         * Specific commands are available for setting EEPROM values 
         * where appropriate to the device. */
        void WriteEeprom(EEPROMAddress addr, byte data)
        {
            myI2Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.WriteEEPROM, (byte)addr, data });
            // Exécution de la transaction
            BusI2C = new I2CDevice(ConfigHM6352); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
            myBytesTransmitted = BusI2C.Execute(myI2Command, 100);
            BusI2C.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
        }

        // Be Careful - this command will change the I2C Slave address of the device.
        // Valid values are 0x10 to 0xF6.  The least significant bit must be 0.
        // After changing the slave address,, you should dispose of the currrent Compass 
        // and construct a new one with the new address.
        public void SetSlaveAddress(byte addr)
        {
            WriteEeprom(EEPROMAddress.SlaveAddress, addr);
        }

        // Set the number of measurements to average when reporting current data.
        // Valid values are 1 to 16.  Factory default
        public void SetNumberOfMeasurements(byte count)
        {
            WriteEeprom(EEPROMAddress.NumberOfMeasurements, count);
        }

        public void Sleep()
        {
            myI2Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.Sleep});
            // Exécution de la transaction
            BusI2C = new I2CDevice(ConfigHM6352); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
            myBytesTransmitted = BusI2C.Execute(myI2Command, 100);
            BusI2C.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
        }

        public void WakeUp()
        {
            myI2Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.WakeUp });
            // Exécution de la transaction
            BusI2C = new I2CDevice(ConfigHM6352); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
            myBytesTransmitted = BusI2C.Execute(myI2Command, 100);
            BusI2C.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
        }

        // Set the operational mode.
        // Mode = Standby, Query, Continuous
        // Frequency = 1, 45, 10, 20 Hz
        // Periodic reset = true/false
        public void SetOperationalMode(OperationalMode mode, Frequency freq, Boolean periodicReset)
        {
            byte r = periodicReset ? (byte)(0x01 << 3) : (byte) 0;
            byte f = (byte)((byte)freq << 5);
            byte op = (byte)((byte)mode | r | f);
            myI2Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.WriteEEPROM, (byte)EEPROMAddress.OperationalModeByte, op });
            // Exécution de la transaction
            BusI2C = new I2CDevice(ConfigHM6352); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
            myBytesTransmitted = BusI2C.Execute(myI2Command, 100);
            BusI2C.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
        }

        public OperationalMode GetOperationalMode()
        {
            byte mode = ReadEeprom(EEPROMAddress.OperationalModeByte);
            return (OperationalMode)(mode & 0x02);
        }

        public Frequency GetFrequency()
        {
            byte freq = ReadEeprom(EEPROMAddress.OperationalModeByte);
            return (Frequency)(freq >> 5);
        }

    }
}

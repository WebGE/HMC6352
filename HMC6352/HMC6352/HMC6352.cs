using System;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT;

namespace testMicroToolsKit
{
    namespace Hardware
    {
        namespace Sensors
        {
            /// <summary>
            /// HMC6352 : Digital Compass Solution
            /// </summary>
            /// <remarks>
            /// You may have some additional information about this class on http://webge.github.io/HMC6352/
            /// </remarks>
            public class HMC6352
            {
                /// <summary>
                /// Transaction time out = 1s before throwing System.IO.IOException 
                /// </summary>
                private UInt16 _transactionTimeOut = 1000;

                /// <summary>
                /// Slave Adress and frequency configuration
                /// </summary>
                private I2CDevice _i2cBus;
                I2CDevice.Configuration _config;

                /// <summary>
                /// 7-bit Slave Adress
                /// </summary>
                private UInt16 _sla;

                I2CDevice.I2CTransaction[] _Command = new I2CDevice.I2CTransaction[2];
                byte[] _ReadBuffer = new byte[2];
                byte[] _ReadEEPROM = new byte[1];
                int myBytesTransmitted = 0;

                /// <summary>
                /// The HMC6352 has three operational modes plus the ability to enter/exit the non-operational (sleep) mode by command. 
                /// </summary>
                public enum OperationalMode : byte
                {
                    /// <summary>
                    /// Factory default mode.  The HMC6352 waits for master device commands or change in operational mode. 
                    /// Receiving an “A” command (get data) will make the HMC6352 perform a measurement of sensors (magnetometers), 
                    /// compute the compensated magnetometer and heading data, and wait for the next read or command. 
                    /// No new measurements are done until another “A” command is sent.
                    /// </summary>
                    /// <remarks>
                    /// This mode is useful to get data on demand or at random intervals as long as the application 
                    /// can withstand the time delay in getting the data.
                    /// </remarks>
                    Standby = 0,
                    /// <summary>
                    /// In this mode the internal processor waits for “A” commands (get data), makes the measurements and computations, 
                    /// and waits for the next read command to output the data. After each read command, the HMC6352 automatically 
                    /// performs another get data routine and updates the data registers. 
                    /// </summary>
                    /// <remarks>
                    /// This mode is designed to get data on demand without repeating “A” commands, and with the master device controlling the timing and data throughput.
                    /// The tradeoff in this mode is the previous query latency for the advantage of an immediate read of data.
                    /// </remarks>
                    Query = 1,
                    /// <summary>
                    /// The HMC6352 performs continuous sensor measurements and data computations at selectable rates 
                    /// of 1Hz, 5Hz, 10Hz, or 20Hz, and updates the output data bytes. Subsequent “A” commands are un-necessary 
                    /// unless re-synchronization to the command is desired. Data reads automatically get the most recent updates. 
                    /// </summary>
                    /// <remarks>
                    /// This mode is useful for data demanding applications. 
                    /// </remarks>
                    Continuous = 2
                }
                /// <summary>
                /// Continuous mode frequency
                /// </summary>
                /// <remarks>
                /// The frequency of measurements may be set to 1, 5, 10, or 20 Hz
                /// </remarks>
                public enum Frequency : byte { f1Hz = 0, f5Hz = 1, f10Hz = 2, f20Hz = 3 }

                /// <summary>
                /// Output Data Modes
                /// </summary>
                /// <remarks>
                /// The read response bytes after an “A” command, will cause the HMC6352 will return two bytes with binary formatted data.
                /// Either heading or magnetometer data can be retrieved depending on the output data selection byte value.
                /// Negative signed magnetometer data will be returned in two’s complement form.This output data control byte is located 
                /// in RAM  register location 4E(hex) and defaults to value zero(heading) at power up.
                /// </remarks>
                public enum OutputMode : byte
                {
                    /// <summary>
                    /// The heading output data will be the value in tenths of degrees from zero to 3599 and provided in binary format over the two bytes. 
                    /// </summary>
                    Heading = 0,
                    /// <summary>
                    /// These X and Y raw magnetometer data readings are the internal sensor values measured at the output
                    /// of amplifiers A and B respectively and are 10-bit 2’s complement binary ADC counts of the analog voltages
                    /// at pins CA1 and CB1. The leading 6-bits on the MSB are zero filled or complemented for negative values. The zero count
                    /// value will be about half of the supply voltage. If measurement averaging is implemented, the most significant bits may
                    /// contain values of the summed readings. 
                    /// </summary>
                    RawX = 1,
                    /// <summary>
                    /// see RawX
                    /// </summary>
                    RawY = 2,
                    /// <summary>
                    /// These X and Y magnetometer data readings are the raw magnetometer readings plus offset and scaling factors applied. 
                    /// The data format is the same as the raw magnetometer data. These compensated data values come from the calibration routine 
                    /// factors plus additional offset factors provided by the set/reset routine. 
                    /// </summary>
                    MagnetometerX = 3,
                    /// <summary>
                    /// see MagnetometerX
                    /// </summary>
                    MagnetometerY = 4
                }

                /// <summary>
                /// Commands
                /// </summary>
                public enum Command : byte
                {
                    /// <summary>
                    /// w - write to EEPROM
                    /// </summary>
                    WriteEEPROM = 0x77,
                    /// <summary>
                    /// r - read from EEPROM
                    /// </summary>
                    ReadEEPROM = 0x72,
                    /// <summary>
                    /// G - write to RAM
                    /// </summary>
                    WriteRAM = 0x47,
                    /// <summary>
                    /// g - read from RAM
                    /// </summary>
                    ReadRAM = 0x67,
                    /// <summary>
                    ///  S - (Sleep) Enter Sleep Mode
                    /// </summary>
                    Sleep = 0x53,
                    /// <summary>
                    /// W - (Wake Up) Exit Sleep Mode 
                    /// </summary>
                    WakeUp = 0x57,
                    /// <summary>
                    /// O - Update bridge offsets
                    /// </summary>
                    UpdateOffsets = 0x4f,
                    /// <summary>
                    /// C - Enter Callibration Mode
                    /// </summary>
                    StartCallibration = 0x43,
                    /// <summary>
                    /// E - Exit Callibration Mode
                    /// </summary>
                    EndCallibration = 0x45,
                    /// <summary>
                    /// L - Save Operation Mode to RAM
                    /// </summary>
                    SaveOperationMode = 0x4c,
                    /// <summary>
                    /// A - Get Data, Compensate and calculate new headin
                    /// </summary>
                    GetData = 0x41
                }

                /// <summary>
                /// EEPROM Contents
                /// </summary>
                public enum EEPROMAddress : byte
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

                /// <summary>
                /// Get or set time before System IO Exception if transaction failed (in ms).
                /// </summary>
                /// <remarks>
                /// 1000ms by default
                /// </remarks>
                public UInt16 TransactionTimeOut
                {
                    get
                    {
                        return _transactionTimeOut;
                    }

                    set
                    {
                        _transactionTimeOut = value;
                    }
                }

                /// <summary>
                /// This constructor assumes the default factory 2*Slave Address + R/W = 0x42
                /// </summary>
                public HMC6352()
                {
                    _config = new I2CDevice.Configuration(0x21, 100);
                }

                /// <summary>
                /// This constructor allows user to specify the Slave Address, bus frequency = 100khz 
                /// </summary>
                /// <param name="SLA"></param>
                public HMC6352(UInt16 SLA)
                {
                    _config = new I2CDevice.Configuration(SLA, 100);
                }

                /// <summary>
                /// This constructor allows user to specify the Slave Address and bus frequency
                /// </summary>
                /// <param name="SLA"></param>
                /// <param name="Frequency"></param>
                public HMC6352(UInt16 SLA, UInt16 Frequency)
                {
                    _config = new I2CDevice.Configuration(SLA, Frequency);
                }

                /// <summary>
                /// 
                /// </summary>
                /// <returns></returns>
                public float GetHeading()
                {
                    _Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.GetData });
                    _Command[1] = I2CDevice.CreateReadTransaction(_ReadBuffer);
                    // Exécution de la transaction
                    _i2cBus = new I2CDevice(_config); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
                    myBytesTransmitted = _i2cBus.Execute(_Command, 100);
                    _i2cBus.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
                    float heading = ((_ReadBuffer[0] << 8) + _ReadBuffer[1]) / 10f;
                    return (heading);
                }

                /// <summary>
                /// After starting Callibration, the user should rotate the sensor through 2 or more complete rotations.
                /// </summary>
                /// <remarks>
                /// The optimal time is 2 rotations over 20 seconds.
                /// </remarks>
                public void StartCalibration()
                {
                    _Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.StartCallibration });
                    // Exécution de la transaction
                    _i2cBus = new I2CDevice(_config); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
                    myBytesTransmitted = _i2cBus.Execute(_Command, 100);
                    _i2cBus.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
                }

                /// <summary>
                /// Call this method to end the callibration Sequence.
                /// </summary>
                public void EndCallibration()
                {
                    _Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.EndCallibration });
                    // Exécution de la transaction
                    _i2cBus = new I2CDevice(_config); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
                    myBytesTransmitted = _i2cBus.Execute(_Command, 100);
                    _i2cBus.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
                }

                /// <summary>
                /// 
                /// </summary>
                /// <param name="addr"></param>
                /// <returns></returns>
                public byte ReadEeprom(EEPROMAddress addr)
                {
                    _Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.ReadEEPROM, (byte)addr });
                    _Command[1] = I2CDevice.CreateReadTransaction(_ReadEEPROM);
                    // Exécution de la transaction
                    _i2cBus = new I2CDevice(_config); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
                    myBytesTransmitted = _i2cBus.Execute(_Command, 100);
                    _i2cBus.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
                    return _ReadEEPROM[0];
                }

                /// <summary>
                /// Be Careful - this command will change the I2C Slave address of the device.
                /// After changing the slave address, you should dispose of the currrent Compass
                /// and construct a new one with the new address.
                /// </summary>
                /// <param name="addr">Valid values are 0x10 to 0xF6.  The least significant bit must be 0.</param>
                public void SetSlaveAddress(byte addr)
                {
                    WriteEeprom(EEPROMAddress.SlaveAddress, addr);
                }


                /// <summary>
                /// Set the number of measurements to average when reporting current data.
                /// </summary>
                /// <param name="count">Valid values are 1 to 16.  Factory default</param>
                public void SetNumberOfMeasurements(byte count)
                {
                    WriteEeprom(EEPROMAddress.NumberOfMeasurements, count);
                }

                /// <summary>
                /// 
                /// </summary>
                public void Sleep()
                {
                    _Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.Sleep });
                    // Exécution de la transaction
                    _i2cBus = new I2CDevice(_config); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
                    myBytesTransmitted = _i2cBus.Execute(_Command, 100);
                    _i2cBus.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
                }

                /// <summary>
                /// 
                /// </summary>
                public void WakeUp()
                {
                    _Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.WakeUp });
                    // Exécution de la transaction
                    _i2cBus = new I2CDevice(_config); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
                    myBytesTransmitted = _i2cBus.Execute(_Command, 100);
                    _i2cBus.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
                }
 
                /// <summary>
                /// Set the operational mode.
                /// </summary>
                /// <param name="mode">Standby, Query, Continuous</param>
                /// <param name="freq">1, 45, 10, 20 Hz</param>
                /// <param name="periodicReset">true/false</param>
                public void SetOperationalMode(OperationalMode mode, Frequency freq, Boolean periodicReset)
                {
                    byte r = periodicReset ? (byte)(0x01 << 3) : (byte)0;
                    byte f = (byte)((byte)freq << 5);
                    byte op = (byte)((byte)mode | r | f);
                    _Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.WriteEEPROM, (byte)EEPROMAddress.OperationalModeByte, op });
                    // Exécution de la transaction
                    _i2cBus = new I2CDevice(_config); // Connexion virtuelle de l'objet HMC6352  au bus I2C 
                    myBytesTransmitted = _i2cBus.Execute(_Command, 100);
                    _i2cBus.Dispose(); // Déconnexion virtuelle de l'objet HMC6352 du bus I2C
                }

                /// <summary>
                /// Get operational mode
                /// </summary>
                /// <returns>Operational mode</returns>
                public OperationalMode GetOperationalMode()
                {
                    byte mode = ReadEeprom(EEPROMAddress.OperationalModeByte);
                    return (OperationalMode)(mode & 0x02);
                }

                /// <summary>
                /// Get Frequency
                /// </summary>
                /// <returns>Frequency</returns>
                public Frequency GetFrequency()
                {
                    byte freq = ReadEeprom(EEPROMAddress.OperationalModeByte);
                    return (Frequency)(freq >> 5);
                }

                /// <summary>
                /// There is no generic public Write EEPROM command.
                /// Specific commands are available for setting EEPROM values where appropriate to the device.
                /// </summary>
                /// <param name="addr"></param>
                /// <param name="data"></param>
                void WriteEeprom(EEPROMAddress addr, byte data)
                {
                    _Command[0] = I2CDevice.CreateWriteTransaction(new byte[] { (byte)Command.WriteEEPROM, (byte)addr, data });
                    _i2cBus = new I2CDevice(_config);  
                    myBytesTransmitted = _i2cBus.Execute(_Command, 100);
                    _i2cBus.Dispose(); 
                }

            }
        }
    }
}
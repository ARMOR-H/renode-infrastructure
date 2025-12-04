using System;
using System.Collections.Generic;

using Antmicro.Renode.Core;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.I2C;
using Antmicro.Renode.Peripherals.Sensor;
using Antmicro.Renode.Utilities;

using Antmicro.Renode.Peripherals.CPU;

namespace Antmicro.Renode.Peripherals.Sensors
{
    public class AS7341 : II2CPeripheral, IPhotodetector, IInterceptable
    {
        public AS7341(IMachine machine)
        {
            this.ConfigureIntercepts(machine, new Dictionary<string, Action<ICPUWithHooks, ulong>>()
            {
                { "PD_I2c_Init", CallVoidFunction(CommInit) },
                { "PD_Power_On", CallVoidFunction(PowerOn) },
                { "PD_Power_Off", CallVoidFunction(PowerOff) },
                { "PD_Measurement_Enable", CallVoidFunction(EnableMeasurement) },
                { "PD_Measurement_Disable", CallVoidFunction(DisableMeasurement) },
                { "PD_Measurement_Enabled", CallBoolFunction(IsMeasurementEnabled) },
                { "PD_Initialize_LED", CallVoidFunction(InitializeLED) },
                { "PD_LED_On", CallVoidFunction(LEDOn) },
                { "PD_LED_Off", CallVoidFunction(LEDOff) },
                { "PD_Get_AuxID", CallByteFunction(GetAuxID) },
                { "PD_Get_RevID", CallByteFunction(GetRevID) },
                { "PD_Get_ID", CallByteFunction(GetDeviceID) },
                { "PD_Get_IntBusy", CallBoolFunction(GetInitializationBusy) },
                { "PD_Get_Ready", CallBoolFunction(GetMeasurementReady) },
                { "PD_Get_Valid", CallBoolFunction(GetMeasurementValid) },
                { "PD_Enable_Interrupt", CallVoidFunction(EnableInterrupt) },
                { "PD_Disable_Interrupt", CallVoidFunction(DisableInterrupt) },
                { "PD_Interrupt_Enabled", CallBoolFunction(IsInterruptEnabled) },
                { "PD_Get_LowInterrupt", CallBoolFunction(GetLowInterrupt) },
                { "PD_Get_HighInterrupt", CallBoolFunction(GetHighInterrupt) },
                { "PD_Set_LowThreshold", CallVoidFunctionWithUShortArg(SetLowInterruptThreshold) },
                { "PD_Set_HighThreshold", CallVoidFunctionWithUShortArg(SetHighInterruptThreshold) },
                { "PD_Get_LowThreshold", CallUShortFunction(GetLowInterruptThreshold) },
                { "PD_Get_HighThreshold", CallUShortFunction(GetHighInterruptThreshold) },
                { "PD_Get_Value", CallUShortFunction(GetValue) },
                { "PD_enable_sleep_after_interrupt", CallVoidFunction(EnableSleepAfterInterrupt) },
                { "PD_disable_sleep_after_interrupt", CallVoidFunction(DisableSleepAfterInterrupt) },
                { "PD_clear_interrupts", CallVoidFunction(ClearInterrupts) },
            });
        }

        public void Reset()
        {
            poweredOn = false;
            measurementEnabled = false;
            ledInitialized = false;
            ledOn = false;
            measurementReady = false;
            measurementValid = false;
            interruptEnabled = false;
            lowInterrupt = false;
            highInterrupt = false;
            lowInterruptThreshold = 0;
            highInterruptThreshold = 0;
            sleepAfterInterrupt = false;
            sleepAfterInterruptActive = false;
            selectedRegister = null;

            Update();
        }

        public void Write(byte[] data)
        {
            var startIndex = 0;
            if(!selectedRegister.HasValue)
            {
                selectedRegister = data[0];
                startIndex = 1;
            }

            for(var i = startIndex; i < data.Length; i++)
            {
                values[selectedRegister.Value] = data[i];
                selectedRegister++;
            }
        }

        public byte[] Read(int count)
        {
            var result = new byte[count];

            if(!selectedRegister.HasValue)
            {
                this.WarningLog("Tried to read but no register set");
            }
            else
            {
                result[0] = values.GetOrDefault(selectedRegister.Value);
                selectedRegister = null;
            }
            return result;
        }

        public void FinishTransmission()
        {
            selectedRegister = null;
        }

        public decimal LightLevel
        {
            get => lightLevel;
            set => lightLevel = value;
        }

        public bool Intercept
        {
            get => intercept;
            set
            {
                intercept = value;
                InterceptChanged?.Invoke(value);
            }
        }

        public event Action<bool> InterceptChanged;

        private Action<ICPUWithHooks, ulong> CallVoidFunction(Action function)
        {
            return (cpu, pc) =>
            {
                function();
                var mcpu = (CortexM)cpu;

                mcpu.PC = mcpu.LR;
            };
        }

        private Action<ICPUWithHooks, ulong> CallBoolFunction(Func<bool> function)
        {
            return (cpu, pc) =>
            {
                var returnValue = function();
                var mcpu = (CortexM)cpu;

                mcpu.PC = mcpu.LR;
                mcpu.SetRegister(0, (RegisterValue)(returnValue ? 1 : 0));
            };
        }

        private Action<ICPUWithHooks, ulong> CallByteFunction(Func<Byte> function)
        {
            return (cpu, pc) =>
            {
                var returnValue = function();
                var mcpu = (CortexM)cpu;

                mcpu.PC = mcpu.LR;
                mcpu.SetRegister(0, (RegisterValue)returnValue);
            };
        }

        private Action<ICPUWithHooks, ulong> CallUShortFunction(Func<UInt16> function)
        {
            return (cpu, pc) =>
            {
                var returnValue = function();
                var mcpu = (CortexM)cpu;

                mcpu.PC = mcpu.LR;
                mcpu.SetRegister(0, (RegisterValue)returnValue);
            };
        }

        private Action<ICPUWithHooks, ulong> CallVoidFunctionWithUShortArg(Action<UInt16> function)
        {
            return (cpu, pc) =>
            {
                var mcpu = (CortexM)cpu;
                var arg = (UInt16)mcpu.GetRegister(0);

                function(arg);

                mcpu.PC = mcpu.LR;
            };
        }

        private void CommInit()
        {
            this.NoisyLog("Comm Init");
        }

        private void PowerOn()
        {
            this.NoisyLog("Power On");
            poweredOn = true;
        }

        private void PowerOff()
        {
            this.NoisyLog("Power Off");
            poweredOn = false;
        }

        private void EnableMeasurement()
        {
            this.NoisyLog("Enable Measurement");
            measurementEnabled = true;
            Update();
            measurementReady = true;
            measurementValid = true;
        }

        private void DisableMeasurement()
        {
            this.NoisyLog("Disable Measurement");
            measurementEnabled = false;
        }

        private bool IsMeasurementEnabled()
        {
            this.NoisyLog("Is Measurement Enabled: {0}", measurementEnabled);
            return measurementEnabled;
        }

        private void InitializeLED()
        {
            this.NoisyLog("Initialize LED");
            ledInitialized = true;
            ledOn = false;
        }

        private void LEDOn()
        {
            if(ledInitialized)
            {
                this.NoisyLog("LED On");
                ledOn = true;
            }
        }

        private void LEDOff()
        {
            if(ledInitialized)
            {
                this.NoisyLog("LED Off");
                ledOn = false;
            }
        }

        private Byte GetAuxID()
        {
            return 0x00;
        }

        private Byte GetRevID()
        {
            return 0x00;
        }

        private Byte GetDeviceID()
        {
            return 0x09;
        }

        private bool GetInitializationBusy()
        {
            this.NoisyLog("Get Initialization Busy");
            return false;
        }

        private bool GetMeasurementReady()
        {
            this.NoisyLog("Get Measurement Ready");
            return measurementReady;
        }

        private bool GetMeasurementValid()
        {
            this.NoisyLog("Get Measurement Valid");
            return measurementValid;
        }

        private void EnableInterrupt()
        {
            this.NoisyLog("Enable Interrupt");
            interruptEnabled = true;
        }

        private void DisableInterrupt()
        {
            this.NoisyLog("Disable Interrupt");
            interruptEnabled = false;
        }

        private bool IsInterruptEnabled()
        {
            this.NoisyLog("Is Interrupt Enabled: {0}", interruptEnabled);
            return interruptEnabled;
        }

        private bool GetLowInterrupt()
        {
            this.NoisyLog("Get Low Interrupt: {0}", lowInterrupt);
            return lowInterrupt;
        }

        private bool GetHighInterrupt()
        {
            this.NoisyLog("Get High Interrupt: {0}", highInterrupt);
            return highInterrupt;
        }

        private void SetLowInterruptThreshold(UInt16 value)
        {
            this.NoisyLog("Set Low Interrupt: {0}", value);
            lowInterruptThreshold = value;
        }

        private void SetHighInterruptThreshold(UInt16 value)
        {
            this.NoisyLog("Set High Interrupt: {0}", value);
            highInterruptThreshold = value;
        }

        private UInt16 GetLowInterruptThreshold()
        {
            this.NoisyLog("Get Low Interrupt Threshold: {0}", lowInterruptThreshold);
            return lowInterruptThreshold;
        }

        private UInt16 GetHighInterruptThreshold()
        {
            this.NoisyLog("Get High Interrupt Threshold: {0}", highInterruptThreshold);
            return highInterruptThreshold;
        }

        private UInt16 GetValue()
        {
            Update();
            this.NoisyLog("Get Value: {0}", readValue);
            return readValue;
        }

        private void EnableSleepAfterInterrupt()
        {
            this.NoisyLog("Enable Sleep After Interrupt");
            sleepAfterInterrupt = true;
        }

        private void DisableSleepAfterInterrupt()
        {
            this.NoisyLog("Disable Sleep After Interrupt");
            sleepAfterInterrupt = false;
        }

        private void ClearInterrupts()
        {
            this.NoisyLog("Clear Interrupts");
            lowInterrupt = false;
            highInterrupt = false;
            sleepAfterInterruptActive = false;

            Update();
        }

        private void Update()
        {
            measurementReady = false;
            measurementValid = false;

            if(!poweredOn || !measurementEnabled)
            {
                return;
            }

            if(sleepAfterInterrupt && sleepAfterInterruptActive)
            {
                return;
            }

            measurementReady = true;
            measurementValid = true;

            var sensorValue = (UInt16)lightLevel;
            readValue = sensorValue;
            this.NoisyLog("Sensor Value: {0}", readValue);
            sleepAfterInterruptActive = sleepAfterInterrupt;

            if(interruptEnabled)
            {
                if(sensorValue < lowInterruptThreshold)
                {
                    lowInterrupt = true;
                }
                else if(sensorValue > highInterruptThreshold)
                {
                    highInterrupt = true;
                }
            }
        }

        private Nullable<Byte> selectedRegister;
        private Dictionary<Byte, Byte> values = new Dictionary<Byte, Byte>();

        private bool poweredOn = false;
        private bool measurementEnabled = false;
        private bool ledInitialized = false;
        private bool ledOn = false;
        private bool measurementReady = false;
        private bool measurementValid = false;
        private bool interruptEnabled = false;

        private bool lowInterrupt = false;
        private bool highInterrupt = false;
        private UInt16 lowInterruptThreshold = 0;
        private UInt16 highInterruptThreshold = 0;
        private bool sleepAfterInterrupt = false;
        private bool sleepAfterInterruptActive = false;
        private decimal lightLevel;
        private UInt16 readValue = 0;

        private bool intercept;

        private enum Registers
        {
            ASTATUS = 0x60,
            CH0DATA_L = 0x61,
            CH0DATA_H = 0x62,
            ITIME_L = 0x63,
            ITIME_M = 0x64,
            ITIME_H = 0x65,

        }
    }
}
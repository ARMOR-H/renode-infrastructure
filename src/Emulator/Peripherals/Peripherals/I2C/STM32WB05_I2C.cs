using System;
using System.Collections.Generic;

using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.CPU;

namespace Antmicro.Renode.Peripherals.I2C
{
    [AllowedTranslations(AllowedTranslation.WordToDoubleWord)]
    public sealed class STM32WB05_I2C : SimpleContainer<II2CPeripheral>, IDoubleWordPeripheral, IBytePeripheral, IKnownSize, IInterceptable
    {
        public STM32WB05_I2C(IMachine machine) : base(machine)
        {
            IRQ = new GPIO();
            CreateRegisters();

            this.ConfigureIntercepts(machine, new Dictionary<string, Action<ICPUWithHooks, ulong>>()
            {
                {"HAL_I2C_Init", ReturnHALOk("HAL_I2C_Init") },
                {"HAL_I2CEx_ConfigAnalogFilter", ReturnHALOk("HAL_I2CEx_ConfigAnalogFilter") },
                {"HAL_I2CEx_ConfigDigitalFilter", ReturnHALOk("HAL_I2CEx_ConfigDigitalFilter") },
                {"HAL_I2C_Mem_Write", (cpu, addr) => MemWriteHook(cpu, addr) },
                {"HAL_I2C_Mem_Read", (cpu, addr) => MemReadHook(cpu, addr) },
                {"HAL_I2C_IsDeviceReady", (cpu, addr) => IsDeviceReadyHook(cpu, addr) }
            });
            Reset();
        }

        public override void Reset()
        {
            IRQ.Unset();
        }

        public uint ReadDoubleWord(long offset)
        {
            return registers.Read(offset);
        }

        public void WriteDoubleWord(long offset, uint value)
        {
            registers.Write(offset, value);
        }

        public byte ReadByte(long offset)
        {
            this.LogUnhandledRead(offset);
            return (byte)0x0;
        }

        public void WriteByte(long offset, byte value)
        {
            this.LogUnhandledWrite(offset, value);
        }

        public GPIO IRQ { get; private set; }

        public long Size => 0x400;

        public bool Intercept
        {
            get => intercept; set
            {
                intercept = value;
                InterceptChanged?.Invoke(value);
            }
        }

        public event Action<bool> InterceptChanged;

        private void CreateRegisters()
        {
            registers = new DoubleWordRegisterCollection(this);

            Registers.Control1.Define(registers, name: "Control1")
                .WithTaggedFlag("PE", 0).WithTaggedFlag("TXIE", 1)
                .WithTaggedFlag("RXIE", 2)
                .WithTaggedFlag("ADDR IE", 3)
                .WithTaggedFlag("NACK IE", 4)
                .WithTaggedFlag("STOP IE", 5)
                .WithTaggedFlag("TCIE", 6)
                .WithTaggedFlag("ERRIE", 7)
                .WithTag("DNF", 8, 4)
                .WithTaggedFlag("ANF OFF", 12)
                .WithReservedBits(13, 1)
                .WithTaggedFlag("TXDMA EN", 14)
                .WithTaggedFlag("RXDMA EN", 15)
                .WithTaggedFlag("SBC", 16)
                .WithTaggedFlag("NOSTRETCH", 17)
                .WithReservedBits(18, 1)
                .WithTaggedFlag("GCEN", 19)
                .WithTaggedFlag("SMBH EN", 20)
                .WithTaggedFlag("SMBD EN", 21)
                .WithTaggedFlag("ALERT EN", 22)
                .WithTaggedFlag("PECEN", 23)
                .WithReservedBits(24, 8);


            Registers.Control2.Define(registers)
                .WithTag("SADD", 0, 10)
                .WithTaggedFlag("RD_WRN", 10)
                .WithTaggedFlag("ADD10", 11)
                .WithTaggedFlag("HEAD 10R", 12)
                .WithTaggedFlag("START", 13)
                .WithTaggedFlag("STOP", 14)
                .WithTaggedFlag("NACK", 15)
                .WithTag("NBYTES", 16, 8)
                .WithTaggedFlag("RELOAD", 24)
                .WithTaggedFlag("AUTOEND", 25)
                .WithTaggedFlag("PEC BYTE", 26)
                .WithReservedBits(27, 5);

            Registers.OwnAddress1.Define(registers)
                .WithTag("OA1", 0, 10)
                .WithTaggedFlag("OA1_Mode", 10)
                .WithReservedBits(11, 4)
                .WithTaggedFlag("OA1EN", 15)
                .WithReservedBits(16, 16);

            Registers.OwnAddress2.Define(registers).WithReservedBits(0, 32);

            Registers.Timing.Define(registers)
                .WithTag("SCLL", 0, 8)
                .WithTag("SCLH", 8, 8)
                .WithTag("SDADEL", 16, 4)
                .WithTag("SCLDEL", 20, 4)
                .WithReservedBits(24, 4)
                .WithTag("PRESC", 28, 4);


            Registers.Timeout.Define(registers).WithReservedBits(0, 32);

            Registers.InterruptAndStatus.Define(registers)
                .WithTaggedFlag("TXE", 0)
                .WithTaggedFlag("TXIS", 1)
                .WithTaggedFlag("RXNE", 2)
                .WithTaggedFlag("ADDR", 3)
                .WithTaggedFlag("NACKF", 4)
                .WithTaggedFlag("STOPF", 5)
                .WithTaggedFlag("TC", 6)
                .WithTaggedFlag("TCR", 7)
                .WithTaggedFlag("BERR", 8)
                .WithTaggedFlag("ARLO", 9)
                .WithTaggedFlag("OVR", 10)
                .WithTaggedFlag("PECERR", 11)
                .WithTaggedFlag("TIMEOUT", 12)
                .WithTaggedFlag("ALERT", 13)
                .WithReservedBits(14, 1)
                .WithTaggedFlag("BUSY", 15)
                .WithTaggedFlag("DIR", 16)
                .WithTag("ADDCODE", 17, 7)
                .WithReservedBits(24, 8)
                ;

            Registers.InterruptClear.Define(registers).WithReservedBits(0, 32);
            Registers.PEC.Define(registers).WithReservedBits(0, 32);
            Registers.ReceiveData.Define(registers)
                .WithTag("RXDATA", 0, 8)
                .WithReservedBits(8, 24);

            Registers.TransmitData.Define(registers)
                .WithTag("TXDATA", 0, 8)
                .WithReservedBits(8, 24);
        }

        private Action<ICPUWithHooks, ulong> ReturnHALOk(string hookName)
        {
            return (cpu, addr) =>
            {
                var mcpu = (CortexM) cpu;
                mcpu.PC = mcpu.LR;
                mcpu.SetRegister(0, (RegisterValue)0); // return HAL_OK

                this.NoisyLog("{0} Hook", hookName);
            };
        }

        private void MemWriteHook(ICPUWithHooks cpu, ulong addr)
        {
            var mcpu = (CortexM) cpu;
            mcpu.PC = mcpu.LR;

            var deviceAddress = ((UInt16)mcpu.GetRegister(1)) >> 1; // dev address
            var memoryAddress = (UInt16)mcpu.GetRegister(2); // mem address
            var memoryAddressSize = (UInt16)mcpu.GetRegister(3); // mem address size
            var stackPointer = (UInt32)mcpu.SP;

            var dataPointer = machine.SystemBus.ReadDoubleWord(stackPointer);
            var dataSize  = (UInt16)machine.SystemBus.ReadDoubleWord(stackPointer + 4);

            var bytes = machine.SystemBus.ReadBytes(dataPointer, dataSize);

            if(ChildCollection.ContainsKey(deviceAddress))
            {
                var device = ChildCollection[deviceAddress];
                byte[] bytesToWrite = new byte[memoryAddressSize + dataSize];
                for(int i = 0; i < memoryAddressSize; i++)
                {
                    bytesToWrite[i] = (byte)(memoryAddress >> (8 * (memoryAddressSize - i - 1)) & 0xFF);
                }
                Array.Copy(bytes, 0, bytesToWrite, memoryAddressSize, dataSize);

                device.Write(bytesToWrite);
                device.FinishTransmission();
                mcpu.SetRegister(0, (RegisterValue)0); // return HAL_OK
                this.NoisyLog("MemWriteHook to device 0x{0:X2}, mem address 0x{1:X4}, size {2}", deviceAddress, memoryAddress, dataSize);
            }
            else
            {
                this.WarningLog("No I2C device found at address 0x{0:X2}", deviceAddress);
                mcpu.SetRegister(0, (RegisterValue)1); // return HAL_ERROR
            }
        }

        private void MemReadHook(ICPUWithHooks cpu, ulong addr)
        {
            var mcpu = (CortexM) cpu;
            mcpu.PC = mcpu.LR;

            var deviceAddress = ((UInt16)mcpu.GetRegister(1)) >> 1; // dev address
            var memoryAddress = (UInt16)mcpu.GetRegister(2); // mem address
            var memoryAddressSize = (UInt16)mcpu.GetRegister(3); // mem address size
            var stackPointer = (UInt32)mcpu.SP;
            var dataPointer = machine.SystemBus.ReadDoubleWord(stackPointer);
            var dataSize  = (UInt16)machine.SystemBus.ReadDoubleWord(stackPointer + 4);

            if(ChildCollection.ContainsKey(deviceAddress))
            {
                var device = ChildCollection[deviceAddress];
                // First, write the memory address to the device
                byte[] memAddressBytes = new byte[memoryAddressSize];
                for(int i = 0; i < memoryAddressSize; i++)
                {
                    memAddressBytes[i] = (byte)(memoryAddress >> (8 * (memoryAddressSize - i - 1)) & 0xFF);
                }
                device.Write(memAddressBytes);

                // Then read the data
                var readData = device.Read(dataSize);
                device.FinishTransmission();
                machine.SystemBus.WriteBytes(readData, dataPointer);

                mcpu.SetRegister(0, (RegisterValue)0); // return HAL_OK
                this.NoisyLog("MemReadHook from device 0x{0:X2}, mem address 0x{1:X4}, size {2}", deviceAddress, memoryAddress, dataSize);
            }
            else
            {
                this.WarningLog("No I2C device found at address 0x{0:X2}", deviceAddress);
                mcpu.SetRegister(0, (RegisterValue)1); // return HAL_ERROR
            }
        }

        private void IsDeviceReadyHook(ICPUWithHooks cpu, ulong addr)
        {
            var mcpu = (CortexM) cpu;
            mcpu.PC = mcpu.LR;


            mcpu.SetRegister(0, (RegisterValue)0); // return HAL_OK

            this.NoisyLog("IsDeviceReadyHook");
        }

        private bool intercept;

        private DoubleWordRegisterCollection registers;

        private enum Registers
        {
            Control1 = 0x00,
            Control2 = 0x04,
            OwnAddress1 = 0x08,
            OwnAddress2 = 0x0C,
            Timing = 0x10,
            Timeout = 0x14,
            InterruptAndStatus = 0x18,
            InterruptClear = 0x1C,
            PEC = 0x20,
            ReceiveData = 0x24,
            TransmitData = 0x28,
        }

    }
}
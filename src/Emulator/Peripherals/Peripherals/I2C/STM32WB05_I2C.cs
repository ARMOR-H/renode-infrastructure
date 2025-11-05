using System;
using System.Collections.Generic;
using System.Linq;

using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;

namespace Antmicro.Renode.Peripherals.I2C
{
    [AllowedTranslations(AllowedTranslation.WordToDoubleWord)]
    public sealed class STM32WB05_I2C : SimpleContainer<II2CPeripheral>, IDoubleWordPeripheral, IBytePeripheral, IKnownSize
    {
        public STM32WB05_I2C(IMachine machine) : base(machine)
        {
            IRQ = new GPIO();
            CreateRegisters();
            Reset();
        }

        public override void Reset()
        {
            IRQ.Unset();

            selectedSlave = null;
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

        private void CreateRegisters()
        {
            registers = new DoubleWordRegisterCollection(this);

            Registers.Control1.Define(registers, name: "Control1")
                .WithFlag(0, out enable, name: "PE", writeCallback: (oldValue, newValue) => { if(!newValue) { Reset(); } })
                .WithTaggedFlag("TXIE", 1)
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
                .WithValueField(0, 10, name: "SADD", writeCallback: (_, newAddress) => SelectSlave(newAddress))
                .WithTaggedFlag("RD_W NR", 10)
                .WithTaggedFlag("ADD10", 11)
                .WithTaggedFlag("HEAD 10R", 12)
                .WithFlag(13, out start, name: "START")
                .WithTaggedFlag("STOP", 14)
                .WithTaggedFlag("NACK", 15)
                .WithValueField(16, 8, out nbytes, name: "NBYTES")
                .WithFlag(24, out reload, name: "RELOAD")
                .WithFlag(25, out autoend, name: "AUTOEND")
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
                .WithFlag(0, name: "TXE", valueProviderCallback: (_) => dataToSend == null)
                .WithFlag(1, name: "TXIS", valueProviderCallback: (_) => dataToSend == null)
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
            Registers.ReceiveData.Define(registers).WithReservedBits(0, 32);

            Registers.TransmitData.Define(registers)
                .WithValueField(0, 8, writeCallback: (oldValue, newValue) =>
                {
                    this.InfoLog("TransmitData {0}", newValue);
                    if(selectedSlave != null)
                    {
                        var toWrite = new List<byte>();
                        toWrite.Add((byte)newValue);
                        selectedSlave.Write(toWrite.ToArray());
                    }
                })
                .WithReservedBits(8, 24);
        }

        private void SelectSlave(ulong address)
        {
            var trimmedAddress = (uint)(address);
            var shiftedAddress = (int)(trimmedAddress >> 1);

            if(ChildCollection.ContainsKey(shiftedAddress))
            {
                selectedSlave = ChildCollection[shiftedAddress];
            }
        }

        private DoubleWordRegisterCollection registers;

        private IFlagRegisterField enable;
        private IFlagRegisterField start;
        private IFlagRegisterField reload;
        private IFlagRegisterField autoend;
        private IValueRegisterField nbytes;
        private II2CPeripheral selectedSlave;

        private Nullable<byte> dataToSend;

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
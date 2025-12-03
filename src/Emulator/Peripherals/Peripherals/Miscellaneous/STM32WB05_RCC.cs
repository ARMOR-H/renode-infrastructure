using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class STM32WB05_RCC : BasicDoubleWordPeripheral, IKnownSize
    {
        public STM32WB05_RCC(IMachine machine) : base(machine)
        {
            IRQ = new GPIO();

            DefineRegisters();
            Reset();
        }

        public override void Reset()
        {
            base.Reset();
            IRQ.Unset();
        }

        public long Size => 0x400;

        public GPIO IRQ { get; }

        private void DefineRegisters()
        {
            Registers.ClockSourceControl.Define(this, 0x00001400)
                .WithReservedBits(0, 2)
                .WithFlag(2, out lsiOn, name: "LSION")
                .WithFlag(3, name: "LSIRDY", mode: FieldMode.Read, valueProviderCallback: (_) => { return lsiOn.Value; })
                .WithFlag(4, out lseOn, name: "LSEON")
                .WithFlag(5, name: "LSERDY", mode: FieldMode.Read, valueProviderCallback: (_) => { return lseOn.Value; })
                .WithFlag(6, name: "LSEBYP")
                .WithTag("LOCKDET_NSTOP", 7, 3)
                .WithFlag(10, name: "HSIRDY", mode: FieldMode.Read, valueProviderCallback: (_) => { return true; })
                .WithReservedBits(11, 1)
                .WithFlag(12, name: "HSEPLLBUFON")
                .WithFlag(13, out hsiPllOn, name: "HSIPLLON")
                .WithFlag(14, name: "HSIPLLRDY", mode: FieldMode.Read, valueProviderCallback: (_) => { return hsiPllOn.Value; })
                .WithReservedBits(15, 1)
                .WithFlag(16, out hseOn, name: "HSEON")
                .WithFlag(17, name: "HSERDY", mode: FieldMode.Read, valueProviderCallback: (_) => { return hseOn.Value; })
                .WithReservedBits(18, 14);
            Registers.ClockConfiguration.Define(this).WithReservedBits(0, 32);

            Registers.ClockSourceSoftwareCalibration.Define(this)

                .WithTaggedFlag("LSISWTRIMEN", 0)
                .WithTag("LSISWBW", 1, 4)
                .WithTag("LSEDRV", 5, 2)
                .WithReservedBits(7, 16)

                .WithTaggedFlag("HSISWTRIMEN", 23)
                .WithTag("HSITRIMSW", 24, 6)
                .WithReservedBits(30, 2);

            Registers.ClockInterruptEnable.Define(this).WithReservedBits(0, 32);
            Registers.ClockInterruptFlagStatus.Define(this).WithReservedBits(0, 32);
            Registers.ClockSwitchCommand.Define(this).WithReservedBits(0, 32);
            Registers.AHB0PeripheralReset.Define(this).WithReservedBits(0, 32);
            Registers.APB0PeripheralReset.Define(this).WithReservedBits(0, 32);
            Registers.APB1PeripheralReset.Define(this).WithReservedBits(0, 32);
            Registers.APB2PeripheralReset.Define(this).WithReservedBits(0, 32);
            Registers.AHB0PeripheralClockEnable.Define(this).WithReservedBits(0, 32);
            Registers.APB0PeripheralClockEnable.Define(this).WithReservedBits(0, 32);
            Registers.APB1PeripheralClockEnable.Define(this).WithReservedBits(0, 32);
            Registers.APB2PeripheralClockEnable.Define(this).WithReservedBits(0, 32);
            Registers.ResetStatus.Define(this).WithReservedBits(0, 32);
            Registers.RfSoftwareHighSpeedExternal.Define(this).WithReservedBits(0, 32);
            Registers.RfHighSpeedExternal.Define(this).WithReservedBits(0, 32);
        }

        private IFlagRegisterField lsiOn;
        private IFlagRegisterField lseOn;
        private IFlagRegisterField hseOn;
        private IFlagRegisterField hsiPllOn;

        private enum Registers
        {
            ClockSourceControl = 0x00,
            // reserved 0x04
            ClockConfiguration = 0x08,
            ClockSourceSoftwareCalibration = 0x0C,
            // reserved 0x10 - 0x14
            ClockInterruptEnable = 0x18,
            ClockInterruptFlagStatus = 0x1C,
            ClockSwitchCommand = 0x20,
            // reserved 0x24 - 0x2C
            AHB0PeripheralReset = 0x30,
            APB0PeripheralReset = 0x34,
            APB1PeripheralReset = 0x38,
            // reserved 0x3C
            APB2PeripheralReset = 0x40,
            // reserved 0x44 - 0x4C
            AHB0PeripheralClockEnable = 0x50,
            APB0PeripheralClockEnable = 0x54,
            APB1PeripheralClockEnable = 0x58,
            // reserved 0x5C
            APB2PeripheralClockEnable = 0x60,
            // reserved 0x64 - 0x90
            ResetStatus = 0x94,
            RfSoftwareHighSpeedExternal = 0x98,
            RfHighSpeedExternal = 0x9C
        }
    }
}
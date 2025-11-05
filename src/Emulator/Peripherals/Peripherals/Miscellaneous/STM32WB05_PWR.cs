using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class STM32WB05_PWR : BasicDoubleWordPeripheral, IKnownSize
    {
        public STM32WB05_PWR(IMachine machine) : base(machine)
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
            Registers.Control1.Define(this, 0b0000_0000_0000_0000_0000_0000_0001_0000)
                .WithTaggedFlag("LPMS", 0)
                .WithTaggedFlag("ENSDNBOR", 1)
                .WithTaggedFlag("IBIAS_RUN_AUTO", 2)
                .WithTaggedFlag("IBIAS_RUN_STATE", 3)
                .WithTaggedFlag("APC", 4)
                .WithReservedBits(5, 27);

            Registers.Control2.Define(this)
                .WithTaggedFlag("PVDE", 0)
                .WithTag("PVDLS", 1, 3)
                .WithTaggedFlag("DBGRET", 4)
                .WithFlag(5, name: "RAMRET1")
                .WithReservedBits(6, 2)
                .WithTaggedFlag("GPIORET", 8)
                .WithReservedBits(9, 23);

            Registers.Control3.Define(this).WithReservedBits(0, 32);
            Registers.Control4.Define(this).WithReservedBits(0, 32);
            Registers.Status1.Define(this).WithReservedBits(0, 32);

            Registers.Status2.Define(this, 0b0000_0000_0000_0000_0000_0011_0000_0110)
                .WithTaggedFlag("SMPSBYPR", 0)
                .WithTaggedFlag("SMPSENR", 1)
                .WithTaggedFlag("SMPSRDY", 2)
                .WithReservedBits(3, 1)
                .WithValueField(4, 4, name: "IOBOOTVAL")
                .WithTaggedFlag("REGLPS", 8)
                .WithReservedBits(9, 2)
                .WithTaggedFlag("PVDO", 11)
                .WithValueField(12, 4, name: "IOBOOTVAL")
                .WithReservedBits(16, 16);

            Registers.Control5.Define(this, 0b0000_0000_0000_0000_0011_0000_0001_0100)
                .WithValueField(0, 4, name: "SMPSLVL")
                .WithValueField(4, 2, name: "SMPSBOMSEL")
                .WithReservedBits(6, 2)
                .WithFlag(8, name: "SMPSLPOEN")
                .WithTaggedFlag("SMPSFBYP", 9)
                .WithTaggedFlag("NOSMPS", 10)
                .WithTaggedFlag("SMPS_ENA_DCM", 11)
                .WithTaggedFlag("CLKDETR_DISABLE", 12)
                .WithValueField(13, 2, name: "SMPS_PRECH_CUR_SEL")
                .WithReservedBits(15, 17);

            Registers.PullUpControlA.Define(this).WithReservedBits(0, 32);
            Registers.PullDownControlA.Define(this).WithReservedBits(0, 32);
            Registers.PullUpControlB.Define(this).WithReservedBits(0, 32);
            Registers.PullDownControlB.Define(this).WithReservedBits(0, 32);
            Registers.Control6.Define(this).WithReservedBits(0, 32);
            Registers.Control7.Define(this).WithReservedBits(0, 32);
            Registers.Status3.Define(this).WithReservedBits(0, 32);
            Registers.Debug.Define(this).WithReservedBits(0, 32);
            Registers.ExtendedStatusAndReset.Define(this).WithReservedBits(0, 32);
            Registers.Trim.Define(this).WithValueField(0, 32);
            Registers.EngineeringTrim.Define(this).WithValueField(0, 32);
        }

        private enum Registers
        {
            Control1 = 0x00,
            Control2 = 0x04,
            Control3 = 0x08,
            Control4 = 0x0C,
            Status1 = 0x10,
            Status2 = 0x14,
            // Reserved 0x18
            Control5 = 0x1C,
            PullUpControlA = 0x20,
            PullDownControlA = 0x24,
            PullUpControlB = 0x28,
            PullDownControlB = 0x2C,
            Control6 = 0x30,
            Control7 = 0x34,
            Status3 = 0x38,
            // Reserved 0x3C - 0x80
            Debug = 0x84,
            ExtendedStatusAndReset = 0x88,
            // Reserved 0x8C
            Trim = 0x90,
            EngineeringTrim = 0x94
        }
    }
}
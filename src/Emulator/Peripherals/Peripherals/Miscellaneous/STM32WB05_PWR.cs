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

            Registers.Control3.Define(this)
                .WithTaggedFlag("EWUP0", 0)
                .WithTaggedFlag("EWUP1", 1)
                .WithTaggedFlag("EWUP2", 2)
                .WithTaggedFlag("EWUP3", 3)
                .WithTaggedFlag("EWUP4", 4)
                .WithTaggedFlag("EWUP5", 5)
                .WithTaggedFlag("EWUP6", 6)
                .WithTaggedFlag("EWUP7", 7)
                .WithTaggedFlag("EWUP8", 8)
                .WithTaggedFlag("EWUP9", 9)
                .WithTaggedFlag("EWUP10", 10)
                .WithTaggedFlag("EWUP11", 11)
                .WithTaggedFlag("EWBLE", 12)
                .WithTaggedFlag("EWBLEHCPU", 13)
                .WithTaggedFlag("EIWL2", 14)
                .WithTaggedFlag("EIWL", 15)
                .WithReservedBits(16, 16);

            Registers.Control4.Define(this)
                .WithTaggedFlag("WUP0", 0)
                .WithTaggedFlag("WUP1", 1)
                .WithTaggedFlag("WUP2", 2)
                .WithTaggedFlag("WUP3", 3)
                .WithTaggedFlag("WUP4", 4)
                .WithTaggedFlag("WUP5", 5)
                .WithTaggedFlag("WUP6", 6)
                .WithTaggedFlag("WUP7", 7)
                .WithTaggedFlag("WUP8", 8)
                .WithTaggedFlag("WUP9", 9)
                .WithTaggedFlag("WUP10", 10)
                .WithTaggedFlag("WUP11", 11)
                .WithReservedBits(12, 20);

            Registers.Status1.Define(this)
                .WithTaggedFlag("WUF0", 0)
                .WithTaggedFlag("WUF1", 1)
                .WithTaggedFlag("WUF2", 2)
                .WithTaggedFlag("WUF3", 3)
                .WithTaggedFlag("WUF4", 4)
                .WithTaggedFlag("WUF5", 5)
                .WithTaggedFlag("WUF6", 6)
                .WithTaggedFlag("WUF7", 7)
                .WithTaggedFlag("WUF8", 8)
                .WithTaggedFlag("WUF9", 9)
                .WithTaggedFlag("WUF10", 10)
                .WithTaggedFlag("WUF11", 11)
                .WithTaggedFlag("WBLEF", 12)
                .WithTaggedFlag("WBLEHCPUF", 13)
                .WithTaggedFlag("IWUF2", 14)
                .WithTaggedFlag("IWUF", 15)
                .WithReservedBits(16, 16);

            Registers.Status2.Define(this, 0b0000_0000_0000_0000_0000_0011_0000_0110)
                .WithFlag(0, name: "SMPSBYPR", valueProviderCallback: (_) => smpsBypass.Value)
                .WithFlag(1, name: "SMPSENR", valueProviderCallback: (_) => !smpsBypass.Value)
                .WithFlag(2, name: "SMPSRDY", valueProviderCallback: (_) => !smpsBypass.Value)
                .WithReservedBits(3, 1)
                .WithTag("IOBOOTVAL2", 4, 4)
                .WithFlag(8, name: "REGLPS", valueProviderCallback: (_) => true)
                .WithReservedBits(9, 2)
                .WithTaggedFlag("PVDO", 11)
                .WithTag("IOBOOTVAL", 12, 4)
                .WithReservedBits(16, 16);

            Registers.Control5.Define(this, 0b0000_0000_0000_0000_0011_0000_0001_0100)
                .WithTag("SMPLVL", 0, 4)
                .WithTag("SMPSBOMSEL", 4, 2)
                .WithReservedBits(6, 2)
                .WithTaggedFlag("SMPSLPOEN", 8)
                .WithFlag(9, out smpsBypass, name: "SMPSFBYP")
                .WithTaggedFlag("NOSMPS", 10)
                .WithTaggedFlag("SMPS_ENA_DCM", 11)
                .WithTaggedFlag("CLKDETR_DISABLE", 12)
                .WithTag("SMPS_PRECH_CUR_SEL", 13, 2)
                .WithReservedBits(15, 17);

            Registers.PullUpControlA.Define(this)
                .WithTaggedFlag("PUA0", 0)
                .WithTaggedFlag("PUA1", 1)
                .WithTaggedFlag("PUA2", 2)
                .WithTaggedFlag("PUA3", 3)
                .WithReservedBits(4, 4)
                .WithTaggedFlag("PUA8", 8)
                .WithTaggedFlag("PUA9", 9)
                .WithTaggedFlag("PUA10", 10)
                .WithTaggedFlag("PUA11", 11)
                .WithReservedBits(12, 20);

            Registers.PullDownControlA.Define(this)
                .WithTaggedFlag("PDA0", 0)
                .WithTaggedFlag("PDA1", 1)
                .WithTaggedFlag("PDA2", 2)
                .WithTaggedFlag("PDA3", 3)
                .WithReservedBits(4, 4)
                .WithTaggedFlag("PDA8", 8)
                .WithTaggedFlag("PDA9", 9)
                .WithTaggedFlag("PDA10", 10)
                .WithTaggedFlag("PDA11", 11)
                .WithReservedBits(12, 20);

            Registers.PullUpControlB.Define(this)
                .WithTaggedFlag("PUB0", 0)
                .WithTaggedFlag("PUB1", 1)
                .WithTaggedFlag("PUB2", 2)
                .WithTaggedFlag("PUB3", 3)
                .WithTaggedFlag("PUB4", 4)
                .WithTaggedFlag("PUB5", 5)
                .WithTaggedFlag("PUB6", 6)
                .WithTaggedFlag("PUB7", 7)
                .WithReservedBits(8, 4)
                .WithTaggedFlag("PUB12", 12)
                .WithTaggedFlag("PUB13", 13)
                .WithTaggedFlag("PUB14", 14)
                .WithTaggedFlag("PUB15", 15)
                .WithReservedBits(16, 16);

            Registers.PullDownControlB.Define(this)
                .WithTaggedFlag("PDB0", 0)
                .WithTaggedFlag("PDB1", 1)
                .WithTaggedFlag("PDB2", 2)
                .WithTaggedFlag("PDB3", 3)
                .WithTaggedFlag("PDB4", 4)
                .WithTaggedFlag("PDB5", 5)
                .WithTaggedFlag("PDB6", 6)
                .WithTaggedFlag("PDB7", 7)
                .WithReservedBits(8, 4)
                .WithTaggedFlag("PDB12", 12)
                .WithTaggedFlag("PDB13", 13)
                .WithTaggedFlag("PDB14", 14)
                .WithTaggedFlag("PDB15", 15)
                .WithReservedBits(16, 16);

            Registers.Control6.Define(this)
                .WithTaggedFlag("EWU12", 0)
                .WithTaggedFlag("EWU13", 1)
                .WithTaggedFlag("EWU14", 2)
                .WithTaggedFlag("EWU15", 3)
                .WithTaggedFlag("EWU16", 4)
                .WithTaggedFlag("EWU17", 5)
                .WithTaggedFlag("EWU18", 6)
                .WithTaggedFlag("EWU19", 7)
                .WithReservedBits(8, 24);

            Registers.Control7.Define(this)
                .WithTaggedFlag("WUP12", 0)
                .WithTaggedFlag("WUP13", 1)
                .WithTaggedFlag("WUP14", 2)
                .WithTaggedFlag("WUP15", 3)
                .WithTaggedFlag("WUP16", 4)
                .WithTaggedFlag("WUP17", 5)
                .WithTaggedFlag("WUP18", 6)
                .WithTaggedFlag("WUP19", 7)
                .WithReservedBits(8, 24);

            Registers.Status3.Define(this)
                .WithTaggedFlag("WUF12", 0)
                .WithTaggedFlag("WUF13", 1)
                .WithTaggedFlag("WUF14", 2)
                .WithTaggedFlag("WUF15", 3)
                .WithTaggedFlag("WUF16", 4)
                .WithTaggedFlag("WUF17", 5)
                .WithTaggedFlag("WUF18", 6)
                .WithTaggedFlag("WUF19", 7)
                .WithReservedBits(8, 24);

            Registers.Debug.Define(this)
                .WithTaggedFlag("DEEPSTOP2", 0)
                .WithReservedBits(1, 12)
                .WithTag("DIS_PRECH", 13, 3)
                .WithReservedBits(16, 16);

            Registers.ExtendedStatusAndReset.Define(this)
                .WithReservedBits(0, 9)
                .WithTaggedFlag("DEEPSTOPF", 9)
                .WithTaggedFlag("RFPHASEF", 10)
                .WithReservedBits(11, 21);

            Registers.Trim.Define(this).WithValueField(0, 32);
            Registers.EngineeringTrim.Define(this).WithValueField(0, 32);
        }

        private IFlagRegisterField smpsBypass;

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
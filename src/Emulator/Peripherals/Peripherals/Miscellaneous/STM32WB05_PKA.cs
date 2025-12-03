using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class STM32WB05_PKA : BasicDoubleWordPeripheral, IKnownSize
    {
        public STM32WB05_PKA(IMachine machine) : base(machine)
        {
            DefineRegisters();
            Reset();
        }

        public long Size => 0x1400;

        private void DefineRegisters()
        {
            Registers.Control.Define(this)
                .WithFlag(0, name: "EN")
                .WithTaggedFlag("START", 1)
                .WithTaggedFlag("SECLVL", 2)
                .WithReservedBits(3, 5)
                .WithTag("MODE", 8, 6)
                .WithReservedBits(14, 3)
                .WithTaggedFlag("PRECENDI", 17)
                .WithReservedBits(18, 1)
                .WithTaggedFlag("RAMERRIE", 19)
                .WithTaggedFlag("ADDRERRIE", 20)
                .WithReservedBits(21, 11);

            Registers.Status.Define(this)
                .WithReservedBits(0, 16)
                .WithTaggedFlag("BUSY", 16)
                .WithTaggedFlag("PROCENDF", 17)
                .WithReservedBits(18, 1)
                .WithTaggedFlag("RAMERRF", 19)
                .WithTaggedFlag("ADDRERRF", 20)
                .WithReservedBits(21, 11);

            Registers.ClearFlag.Define(this)
                .WithReservedBits(0, 16)
                .WithTaggedFlag("PROCENDFC", 17)
                .WithReservedBits(18, 1)
                .WithTaggedFlag("RAMERRFC", 19)
                .WithTaggedFlag("ADDRERRC", 20)
                .WithReservedBits(21, 11);
        }

        private enum Registers
        {
            Control = 0x00,
            Status = 0x04,
            ClearFlag = 0x08,
        }
    }
}
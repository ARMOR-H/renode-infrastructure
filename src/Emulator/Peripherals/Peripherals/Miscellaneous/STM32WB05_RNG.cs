using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;

namespace Antmicro.Renode.Peripherals.Miscellaneous
{
    public class STM32WB05_RNG : BasicDoubleWordPeripheral, IKnownSize
    {
        public STM32WB05_RNG(IMachine machine) : base(machine)
        {
            DefineRegisters();
            Reset();
        }

        public long Size => 0x400;

        private void DefineRegisters()
        {
            Registers.Control.Define(this)
                .WithReservedBits(0, 2)
                .WithFlag(3, out disabled, name: "RNG_DIS")
                .WithFlag(4, name: "TST_CLK", valueProviderCallback: (_) => false)
                .WithReservedBits(5, 27);

            Registers.Status.Define(this)
                .WithFlag(0, name: "RNGRDY", valueProviderCallback: (_) => !disabled.Value)
                .WithFlag(1, name: "REVCLK", valueProviderCallback: (_) => true)
                .WithFlag(2, name: "FAULT", valueProviderCallback: (_) => false)
                .WithReservedBits(3, 28);

            Registers.Value.Define(this)
                .WithTag("RANDOM_VALUE", 0, 16)
                .WithReservedBits(16, 16);
        }

        private IFlagRegisterField disabled;

        private enum Registers
        {
            Control = 0x00,
            Status = 0x04,
            Value = 0x08,
        }
    }
}
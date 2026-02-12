using Antmicro.Renode.Core;
using Antmicro.Renode.Peripherals.CPU;

namespace Antmicro.Renode.Peripherals.Bus
{
    public class UnhandledAccess
    {
        public UnhandledAccess(ICPU cpu, ulong pc, Symbol symbol, Access access, ulong address, SysbusAccessWidth accessWidth, ulong value)
        {
            this.CPU = cpu;
            this.PC = pc;
            this.Symbol = symbol;
            this.Access = access;
            this.Address = address;
            this.AccessWidth = accessWidth;
            this.Value = value;
        }

        public readonly ICPU CPU;
        public readonly ulong PC;
        public readonly Symbol Symbol;
        public readonly Access Access;
        public readonly ulong Address;
        public readonly SysbusAccessWidth AccessWidth;
        public readonly ulong Value;
    }
}
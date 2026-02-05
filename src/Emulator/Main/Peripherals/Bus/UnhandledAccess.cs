using Antmicro.Renode.Core;
using Antmicro.Renode.Peripherals.CPU;

namespace Antmicro.Renode.Peripherals.Bus
{
    public class UnhandledAccess
    {
        public UnhandledAccess(ICPU cpu, ulong pc, Symbol symbol, Access access)
        {
            this.CPU = cpu;
            this.PC = pc;
            this.Symbol = symbol;
            this.Access = access;
        }

        public readonly ICPU CPU;
        public readonly ulong PC;
        public readonly Symbol Symbol;
        public readonly Access Access;
    }
}
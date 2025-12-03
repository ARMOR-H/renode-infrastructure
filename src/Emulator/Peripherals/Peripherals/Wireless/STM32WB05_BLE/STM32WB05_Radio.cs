using Antmicro.Renode.Core;

namespace Antmicro.Renode.Peripherals.Wireless
{
    public class STM32WB05_Radio : BasicDoubleWordPeripheral, IKnownSize
    {
        public STM32WB05_Radio(IMachine machine) : base(machine)
        {
        }

        public long Size => 0x800;
    }
}
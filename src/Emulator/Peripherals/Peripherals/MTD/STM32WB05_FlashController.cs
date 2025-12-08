using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Peripherals.Memory;

namespace Antmicro.Renode.Peripherals.MTD
{
    public class STM32WB05_FlashController : BasicDoubleWordPeripheral, IKnownSize
    {
        public STM32WB05_FlashController(IMachine machine, MappedMemory flash) : base(machine)
        {
            bank = flash;
            DefineRegisters();
            Reset();
        }

        public override void Reset()
        {
            base.Reset();
        }

        public long Size => 0x1000;

        private void DefineRegisters()
        {
            Registers.Command.Define(this).WithReservedBits(0, 32);
            Registers.Config.Define(this)
                .WithReservedBits(0, 0)
                .WithTaggedFlag("REMAP", 1)
                .WithTaggedFlag("DIS_GROUP_WRITE", 2)
                .WithReservedBits(3, 1)
                .WithValueField(4, 2, name: "WAIT_STATES")
                .WithReservedBits(6, 26);
            Registers.InterruptStatus.Define(this).WithReservedBits(0, 32);
            Registers.InterruptMask.Define(this).WithReservedBits(0, 32);
            Registers.RawInterruptStatus.Define(this).WithReservedBits(0, 32);
            Registers.Size.Define(this).WithReservedBits(0, 32);
            Registers.Address.Define(this).WithReservedBits(0, 32);
            Registers.LinearFeedbackShift.Define(this).WithReservedBits(0, 32);
            Registers.PageProtection0.Define(this).WithReservedBits(0, 32);
            Registers.PageProtection1.Define(this).WithReservedBits(0, 32);
            Registers.Data0.Define(this).WithReservedBits(0, 32);
            Registers.Data1.Define(this).WithReservedBits(0, 32);
            Registers.Data2.Define(this).WithReservedBits(0, 32);
            Registers.Data3.Define(this).WithReservedBits(0, 32);
        }

        private readonly MappedMemory bank;

        private enum Registers
        {
            Command = 0x00,
            Config = 0x04,
            InterruptStatus = 0x08,
            InterruptMask = 0x0C,
            RawInterruptStatus = 0x10,
            Size = 0x14,
            Address = 0x18,
            // Intentional gap
            LinearFeedbackShift = 0x24,
            PageProtection0 = 0x34,
            PageProtection1 = 0x38,
            // Intentional gap
            Data0 = 0x40,
            Data1 = 0x44,
            Data2 = 0x48,
            Data3 = 0x4C,

        }
    }
}
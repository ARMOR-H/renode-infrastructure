using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Peripherals.Timers;

namespace Antmicro.Renode.Peripherals.Wireless
{
    public class STM32WB05_Wakeup : BasicDoubleWordPeripheral, IKnownSize
    {
        public STM32WB05_Wakeup(IMachine machine) : base(machine)
        {
            timer = new LimitTimer(machine.ClockSource, 32_000, this, "AbsoluteTime", uint.MaxValue, Time.Direction.Ascending, enabled: true, autoUpdate: true);
            DefineRegisters();
            Reset();
        }

        public override void Reset()
        {
            timer.Reset();
            base.Reset();
        }

        public long Size => 0x400;

        private void DefineRegisters()
        {
            Registers.WakeupOffset.Define(this)
                .WithValueField(0, 8, name: "WAKEUP_OFFSET")
                .WithReservedBits(8, 24);

            Registers.AbsoluteTime.Define(this)
                .WithValueField(0, 32, FieldMode.Read, name: "ABSOLUTE_TIME", valueProviderCallback: (_) => timer.Value);

            Registers.MinimumPeriodLength.Define(this).WithReservedBits(0, 32);
            Registers.AveragePeriodLength.Define(this).WithReservedBits(0, 32);
            Registers.MaximumPeriodLength.Define(this).WithReservedBits(0, 32);
            Registers.StatisticsRestart.Define(this).WithReservedBits(0, 32);
            Registers.BlueWakeupTime.Define(this).WithReservedBits(0, 32);
            Registers.BlueSleepRequestMode.Define(this, 0x00000007).WithReservedBits(0, 32);
            Registers.CM0WakeupTime.Define(this).WithReservedBits(0, 32);
            Registers.CM0SleepRequestMode.Define(this, 0x80000007).WithReservedBits(0, 32);
            Registers.BleInterruptEnable.Define(this).WithReservedBits(0, 32);
            Registers.BleInterruptStatus.Define(this).WithReservedBits(0, 32);
            Registers.CM0InterruptEnable.Define(this).WithReservedBits(0, 32);
            Registers.CM0InterruptStatus.Define(this).WithReservedBits(0, 32);
        }

        private readonly LimitTimer timer;

        private enum Registers
        {
            // Intentional gap
            WakeupOffset = 0x08,
            // Intentional gap
            AbsoluteTime = 0x10,
            MinimumPeriodLength = 0x14,
            AveragePeriodLength = 0x18,
            MaximumPeriodLength = 0x1C,
            StatisticsRestart = 0x20,
            BlueWakeupTime = 0x24,
            BlueSleepRequestMode = 0x28,
            CM0WakeupTime = 0x2C,
            CM0SleepRequestMode = 0x30,
            // Intentional gap
            BleInterruptEnable = 0x40,
            BleInterruptStatus = 0x44,
            CM0InterruptEnable = 0x48,
            CM0InterruptStatus = 0x4C,
        }
    }
}
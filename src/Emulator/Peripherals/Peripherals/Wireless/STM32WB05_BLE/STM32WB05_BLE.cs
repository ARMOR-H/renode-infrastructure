using System;
using System.Collections.Generic;
using System.Linq;

using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.CPU;
using Antmicro.Renode.Peripherals.UART;

namespace Antmicro.Renode.Peripherals.Wireless
{
    public class STM32WB05_BLE : BasicDoubleWordPeripheral, IKnownSize, IInterceptable, IHasOwnLife
    {
        public STM32WB05_BLE(IMachine machine) : base(machine)
        {
            proxy = new ProxyUart(machine);

            this.ConfigureIntercepts(machine, new Dictionary<string, Action<ICPUWithHooks, ulong>>()
            {
                {"ble_SendData", SendDataHook},
                {"aci_gap_set_advertising_data", SendDataHook}
            });
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Start()
        {
            machine.RegisterAsAChildOf(this, proxy, NullRegistrationPoint.Instance);
            machine.SetLocalName(proxy, "proxy");
        }

        public long Size => 0x100;

        public bool Intercept
        {
            get => intercept; set
            {
                intercept = value;
                InterceptChanged?.Invoke(value);
            }
        }

        public bool IsPaused => false;

        public event Action<bool> InterceptChanged;

        private void SendDataHook(ICPUWithHooks cpu, ulong address)
        {
            this.NoisyLog("ble_SendData called at 0x{0:X}", address);

            var mcpu = (CortexM) cpu;
            mcpu.PC = mcpu.LR;

            var data = (UInt32)mcpu.GetRegister(0);
            var bytes = BitConverter.GetBytes(data);
            this.NoisyLog("Transmitting {0} bytes via STM32WB05 USART DMA: {1}", bytes.Length, BitConverter.ToString(bytes.ToArray()));

            foreach(var b in bytes)
            {
                proxy.TransmitCharacter(b);
            }
            mcpu.SetRegister(0, (RegisterValue)0x00000000U); // return BleStatus_Success
        }

        private bool intercept;

        private readonly ProxyUart proxy;
    }

    class ProxyUart : UARTBase
    {
        public ProxyUart(IMachine machine) : base(machine)
        {
        }

        public override uint BaudRate => 115200;

        public override Parity ParityBit => Parity.None;

        public override Bits StopBits => Bits.One;

        public new void TransmitCharacter(byte character)
        {
            base.TransmitCharacter(character);
        }

        protected override void CharWritten()
        {
            this.NoisyLog("Char written to STM32WB05 BLE Proxy UART");
        }

        protected override void QueueEmptied()
        {
            this.NoisyLog("STM32WB05 BLE Proxy UART queue emptied");
        }
    }
}
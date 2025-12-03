using System;
using System.Collections.Generic;
using System.Linq;

using Antmicro.Renode.Core;
using Antmicro.Renode.Core.Structure.Registers;
using Antmicro.Renode.Logging;
using Antmicro.Renode.Peripherals.Bus;
using Antmicro.Renode.Peripherals.CPU;

namespace Antmicro.Renode.Peripherals.UART
{
    [AllowedTranslations(AllowedTranslation.ByteToDoubleWord | AllowedTranslation.WordToDoubleWord)]
    public sealed class STM32WB05_USART : UARTBase, IDoubleWordPeripheral, IKnownSize, IInterceptable
    {
        public STM32WB05_USART(IMachine machine) : base(machine)
        {
            this.machine = machine;
            RegistersCollection = new DoubleWordRegisterCollection(this);
            DefineRegisters();

            this.ConfigureIntercepts(machine, new Dictionary<string, Action<ICPUWithHooks, ulong>>()
            {
                {"HAL_UART_GetState", GetStateHook},
                {"HAL_UART_Transmit_DMA", TransmitHook},
                {"HAL_UART_Transmit", TransmitHook},
                {"HAL_UART_Receive_DMA", ReceiveDmaHook},
            });
        }

        public override void Reset()
        {
            base.Reset();
            RegistersCollection.Reset();
            pendingRead = false;
            readIndex = 0;
            readBuffer = null;
            readToAddress = 0;
        }

        public uint ReadDoubleWord(long offset)
        {
            return RegistersCollection.Read(offset);
        }

        public void WriteDoubleWord(long offset, uint value)
        {
            RegistersCollection.Write(offset, value);
        }

        public override uint BaudRate => 115200;

        public override Parity ParityBit => Parity.None;

        public override Bits StopBits => Bits.One;

        public long Size => 0x400;

        public DoubleWordRegisterCollection RegistersCollection { get; }

        public bool Intercept
        {
            get => intercept; set
            {
                intercept = value;
                InterceptChanged?.Invoke(value);
            }
        }

        public event Action<bool> InterceptChanged;

        protected override void CharWritten()
        {
            this.NoisyLog("Char written to STM32WB05 USART");
            HandleRead();
        }

        protected override void QueueEmptied()
        {
            this.NoisyLog("STM32WB05 USART queue emptied");
        }

        private void DefineRegisters()
        {
            Registers.Control1.Define(RegistersCollection)
                .WithTaggedFlag("UE", 0) // USART Enable
                .WithReservedBits(1, 1)
                .WithTaggedFlag("RE", 2) // Receiver Enable
                .WithTaggedFlag("TE", 3) // Transmitter Enable
                .WithTaggedFlag("IDLEIE", 4) // IDLE Interrupt Enable
                .WithTaggedFlag("RXNEIE", 5) // RXNE Interrupt Enable
                .WithTaggedFlag("TCIE", 6) // Transmission Complete Interrupt Enable
                .WithTaggedFlag("TXEIE", 7) // TXE Interrupt Enable
                .WithTaggedFlag("PEIE", 8) // PE Interrupt Enable
                .WithTaggedFlag("PS", 9) // Parity Selection
                .WithTaggedFlag("PCE", 10) // Parity Control Enable
                .WithTaggedFlag("WAKE", 11) // Wakeup Method
                .WithTaggedFlag("M", 12) // Word Length
                .WithTaggedFlag("MME", 13) // Mute Mode Enable
                .WithTaggedFlag("CMIE", 14) // Character Match Interrupt Enable
                .WithTaggedFlag("OVER8", 15) // Oversampling Mode
                .WithTag("DEDT", 16, 5) // Driver Enable Deassertion Time
                .WithTag("DEAT", 21, 5) // Driver Enable Assertion Time
                .WithTaggedFlag("RTOIE", 26) // Receiver Timeout Interrupt Enable
                .WithTaggedFlag("EOBIE", 27) // End of Block Interrupt Enable
                .WithTaggedFlag("M1", 28) // Word Length
                .WithTaggedFlag("FIFOEN", 29) // FIFO Enable
                .WithTaggedFlag("TXFEIE", 30) // TXFIFO Empty Interrupt Enable
                .WithTaggedFlag("RXFFIE", 31); // RXFIFO Full Interrupt Enable

            Registers.Control2.Define(RegistersCollection)
                .WithTaggedFlag("SLVEN", 0) // Synchronous Slave Enable
                .WithReservedBits(1, 2)
                .WithTaggedFlag("DIS_NSS", 3) // Driver Enable Mode NSS Pin
                .WithTaggedFlag("ADDM", 4) // Address Detection Mode
                .WithTaggedFlag("LBDL", 5) // LIN Break Detection Length
                .WithTaggedFlag("LBDIE", 6) // LIN Break Detection Interrupt Enable
                .WithReservedBits(7, 1)
                .WithTaggedFlag("LBCL", 8) // Last Bit Clock Pulse
                .WithTaggedFlag("CPHA", 9) // Clock Phase
                .WithTaggedFlag("CPOL", 10) // Clock Polarity
                .WithTaggedFlag("CLKEN", 11) // Clock Enable
                .WithTaggedFlag("STOP", 12) // STOP bits
                .WithTaggedFlag("LINEN", 14) // LIN Mode Enable
                .WithTaggedFlag("SWAP", 15) // Swap TX/RX Pins
                .WithTaggedFlag("RXINV", 16) // RX Pin Active Level Inversion
                .WithTaggedFlag("TXINV", 17) // TX Pin Active Level Inversion
                .WithTaggedFlag("DATAINV", 18) // Binary Data Inversion
                .WithTaggedFlag("MSBFIRST", 19) // Most Significant Bit First
                .WithTaggedFlag("ABREN", 20) // Auto Baud Rate Enable
                .WithTag("ABRMOD", 21, 2) // Auto Baud Rate Mode
                .WithTaggedFlag("RTOEN", 23) // Receiver Timeout Enable
                .WithTag("ADD", 24, 8); // Address of the USART node

            Registers.Control3.Define(RegistersCollection)
                .WithTaggedFlag("EIE", 0) // Error Interrupt Enable
                .WithTaggedFlag("IREN", 1) // IrDA Mode Enable
                .WithTaggedFlag("IRLP", 2) // IrDA Low-Power
                .WithTaggedFlag("HDSEL", 3) // Half-Duplex Selection
                .WithTaggedFlag("NACK", 4) // Smartcard NACK Enable
                .WithTaggedFlag("SCEN", 5) // Smartcard Mode Enable
                .WithTaggedFlag("DMAR", 6) // DMA Enable Receiver
                .WithTaggedFlag("DMAT", 7) // DMA Enable Transmitter
                .WithTaggedFlag("RTSE", 8) // RTS Enable
                .WithTaggedFlag("CTSE", 9) // CTS Enable
                .WithTaggedFlag("CTSIE", 10) // CTS Interrupt Enable
                .WithTaggedFlag("ONEBIT", 11) // One Sample Bit Method Enable
                .WithTaggedFlag("OVRDIS", 12) // Overrun Disable
                .WithTaggedFlag("DDRE", 13) // DMA Disable on Reception Error
                .WithTaggedFlag("DEM", 14) // Driver Enable Mode
                .WithTaggedFlag("DEP", 15) // Driver Enable Polarity
                .WithTag("SCARCNT", 17, 3) // Smartcard Auto-Retry Count
                .WithReservedBits(20, 3)
                .WithTaggedFlag("TXFTIE", 23) // TXFIFO Threshold Interrupt Enable
                .WithTaggedFlag("TCBGIE", 24) // Transmission Complete Before Guard Time Interrupt Enable
                .WithTag("RXFTCFG", 25, 3) // RXFIFO Threshold Configuration
                .WithTaggedFlag("RXFTIE", 28) // RXFIFO Threshold Interrupt Enable
                .WithTag("TXFTCFG", 29, 3); // TXFIFO Threshold Configuration

            Registers.BaudRate.Define(RegistersCollection)
                .WithTag("BRR", 0, 16) // Baud Rate Divisor
                .WithReservedBits(16, 16);

            Registers.GuardTimeAndPrescaler.Define(RegistersCollection)
                .WithTag("PSC", 0, 8) // Prescaler Value
                .WithTag("GT", 8, 8) // Guard Time Value
                .WithReservedBits(16, 16);

            Registers.ReceiverTimeout.Define(RegistersCollection)
                .WithTag("RTO", 0, 24) // Receiver Timeout Value
                .WithTag("BLEN", 24, 8); // Block Length

            Registers.Request.Define(RegistersCollection)
                .WithTaggedFlag("ABRRQ", 0) // Auto-Baud Rate Request
                .WithTaggedFlag("SBKRQ", 1) // Send Break Request
                .WithTaggedFlag("MMRQ", 2) // Mute Mode Request
                .WithTaggedFlag("RXFRQ", 3) // Receive Data Flush Request
                .WithTaggedFlag("TXFRQ", 4) // Transmit Data Flush Request
                .WithReservedBits(5, 27);

            Registers.InterruptAndStatus.Define(RegistersCollection)
                .WithTaggedFlag("PE", 0) // Parity Error
                .WithTaggedFlag("FE", 1) // Framing Error
                .WithTaggedFlag("NF", 2) // Noise Flag
                .WithTaggedFlag("ORE", 3) // Overrun Error
                .WithTaggedFlag("IDLE", 4) // IDLE Line Detected
                .WithTaggedFlag("RXNE", 5) // Read Data Register Not Empty
                .WithTaggedFlag("TC", 6) // Transmission Complete
                .WithTaggedFlag("TXE", 7) // Transmit Data Register Empty
                .WithTaggedFlag("LBDF", 8) // LIN Break Detection Flag
                .WithTaggedFlag("CTSIF", 9) // CTS Interrupt Flag
                .WithTaggedFlag("CTS", 10) // CTS Flag
                .WithTaggedFlag("RTOF", 11) // Receiver Timeout Flag
                .WithTaggedFlag("EOBF", 12) // End of Block Flag
                .WithTaggedFlag("UDR", 13) // SPI Underrun Error Flag
                .WithTaggedFlag("ABRE", 14) // Auto-Baud Rate Error
                .WithTaggedFlag("ABRF", 15) // Auto-Baud Rate Flag
                .WithTaggedFlag("BUSY", 16) // Busy Flag
                .WithTaggedFlag("CMF", 17) // Character Match Flag
                .WithTaggedFlag("SBKF", 18) // Send Break Flag
                .WithTaggedFlag("RWU", 19) // Receiver Wakeup from Mute
                .WithReservedBits(20, 1)
                .WithTaggedFlag("TEACK", 21) // Transmitter Enable Acknowledge Flag
                .WithTaggedFlag("REACK", 22) // Receiver Enable Acknowledge Flag
                .WithTaggedFlag("TXFE", 23) // TXFIFO Empty
                .WithTaggedFlag("RXFF", 24) // RXFIFO Full
                .WithTaggedFlag("TCBGT", 25) // Transmission Complete Before Guard Time
                .WithTaggedFlag("RXFT", 26) // RXFIFO Threshold Flag
                .WithTaggedFlag("TXFT", 27) // TXFIFO Threshold Flag
                .WithReservedBits(28, 4);

            Registers.InterruptFlagClear.Define(RegistersCollection)
                .WithTaggedFlag("PECF", 0) // Parity Error Clear Flag
                .WithTaggedFlag("FECF", 1) // Framing Error Clear Flag
                .WithTaggedFlag("NECF", 2) // Noise Error Clear Flag
                .WithTaggedFlag("ORECF", 3) // Overrun Error Clear Flag
                .WithTaggedFlag("IDLECF", 4) // IDLE Line Detected Clear Flag
                .WithTaggedFlag("TXECF", 5) // Transmit Data Register Empty Clear Flag
                .WithTaggedFlag("TCCF", 6) // Transmission Complete Clear Flag
                .WithTaggedFlag("TCBGTCF", 7) // Transmission Complete Before Guard Time Clear Flag
                .WithTaggedFlag("LBDCF", 8) // LIN Break Detection Clear Flag
                .WithTaggedFlag("CTSCF", 9) // CTS Interrupt Clear Flag
                .WithReservedBits(10, 1)
                .WithTaggedFlag("RTOCF", 11) // Receiver Timeout Clear Flag
                .WithTaggedFlag("EOBCF", 12) // End of Block Clear Flag
                .WithTaggedFlag("UDRCF", 13) // SPI Underrun Error Clear Flag
                .WithReservedBits(14, 3)
                .WithTaggedFlag("CMCF", 17) // Character Match Clear Flag
                .WithReservedBits(18, 14);

            Registers.ReceiveData.Define(RegistersCollection)
                .WithTag("RDR", 0, 9) // Receive Data Register
                .WithReservedBits(9, 23);

            Registers.TransmitData.Define(RegistersCollection)
                .WithTag("TDR", 0, 9) // Transmit Data Register
                .WithReservedBits(9, 23);

            Registers.Prescaler.Define(RegistersCollection)
                .WithTag("PRESCALER", 0, 4) // Prescaler Value
                .WithReservedBits(4, 28);
        }

        private void GetStateHook(ICPUWithHooks cpu, ulong address)
        {
            this.NoisyLog("HAL_UART_GetState called at 0x{0:X}", address);

            var mcpu = (CortexM) cpu;
            mcpu.PC = mcpu.LR;

            if(pendingRead)
            {
                mcpu.SetRegister(0, (RegisterValue)0x00000022U); // return HAL_UART_STATE_BUSY_RX
            }
            else
            {
                mcpu.SetRegister(0, (RegisterValue)0x00000020U); // return HAL_UART_STATE_READY
            }
        }

        private void TransmitHook(ICPUWithHooks cpu, ulong address)
        {
            this.NoisyLog("HAL_UART_Transmit_* called at 0x{0:X}", address);

            var mcpu = (CortexM) cpu;
            mcpu.PC = mcpu.LR;
            mcpu.SetRegister(0, (RegisterValue)0x00000000U); // return HAL_OK

            var dataPointer = (UInt32)mcpu.GetRegister(1); // 1st argument: uint8_t *pData
            var dataSize = (UInt16)mcpu.GetRegister(2); // 2nd argument: uint16_t Size

            var bytes = machine.SystemBus.ReadBytes(dataPointer, dataSize);
            this.NoisyLog("Transmitting {0} bytes via STM32WB05 USART DMA: {1}", dataSize, BitConverter.ToString(bytes.ToArray()));
            foreach(var b in bytes)
            {
                base.TransmitCharacter(b);
            }
        }

        private void ReceiveDmaHook(ICPUWithHooks cpu, ulong address)
        {
            this.NoisyLog("HAL_UART_Receive_DMA called at 0x{0:X}", address);

            var mcpu = (CortexM) cpu;
            mcpu.PC = mcpu.LR;
            mcpu.SetRegister(0, (RegisterValue)0x00000000U); // return HAL_OK

            var dataPointer = (UInt32)mcpu.GetRegister(1); // 1st argument: uint8_t *pData
            var dataSize = (UInt16)mcpu.GetRegister(2); // 2nd argument: uint16_t Size

            pendingRead = true;
            readIndex = 0;
            readBuffer = new byte[dataSize];
            readToAddress = dataPointer;

            HandleRead();
        }

        private void HandleRead()
        {
            if(!pendingRead)
            {
                return;
            }

            for(; readIndex < readBuffer.Length; readIndex++)
            {
                if(!TryGetCharacter(out var result))
                {
                    break;
                }
                readBuffer[readIndex] = result;
            }

            if(readIndex >= readBuffer.Length)
            {
                this.NoisyLog("Completed pending read of {0} bytes via STM32WB05 USART DMA: {1}", readBuffer.Length, BitConverter.ToString(readBuffer.ToArray()));
                machine.SystemBus.WriteBytes(readBuffer, readToAddress);
                pendingRead = false;
                readIndex = 0;
            }
        }

        private bool intercept;

        private bool pendingRead = false;
        private int readIndex = 0;
        private byte[] readBuffer;
        private ulong readToAddress;

        private readonly IMachine machine;

        private enum Registers
        {
            Control1 = 0x00,
            Control2 = 0x04,
            Control3 = 0x08,
            BaudRate = 0x0C,
            GuardTimeAndPrescaler = 0x10,
            ReceiverTimeout = 0x14,
            Request = 0x18,
            InterruptAndStatus = 0x1C,
            InterruptFlagClear = 0x20,
            ReceiveData = 0x24,
            TransmitData = 0x28,
            Prescaler = 0x2C
        }
    }
}
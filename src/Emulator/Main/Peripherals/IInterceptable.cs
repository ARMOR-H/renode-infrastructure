using System;
using System.Collections.Generic;

using Antmicro.Renode.Core;
using Antmicro.Renode.Hooks;
using Antmicro.Renode.Peripherals.CPU;

namespace Antmicro.Renode.Peripherals
{
    public interface IInterceptable : IPeripheral
    {
        bool Intercept { get; set; }

        event Action<bool> InterceptChanged;
    }

    public static class IInterceptableExtensions
    {
        public static void ConfigureIntercepts(this IInterceptable @this, IMachine machine, Dictionary<string, Action<ICPUWithHooks, ulong>> hooks)
        {
            Action update = () =>
            {
                if (@this.Intercept)
                {
                    foreach(var symbol in hooks)
                    {
                        machine.SystemBus.AddHookAtSymbol(symbol.Key, symbol.Value);
                    }
                }
                else
                {
                    foreach(var symbol in hooks)
                    {
                        machine.SystemBus.RemoveHookAtSymbol(symbol.Key ,symbol.Value);
                    }
                }
            };

            machine.SystemBus.OnSymbolsChanged += (localMachine) => update();
            @this.InterceptChanged += (intercept) => update();
        }
    }
}
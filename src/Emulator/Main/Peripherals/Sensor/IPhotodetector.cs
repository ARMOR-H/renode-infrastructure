using System;

namespace Antmicro.Renode.Peripherals.Sensor
{
    public interface IPhotodetector : ISensor
    {
        decimal LightLevel { get; set; }
    }
}
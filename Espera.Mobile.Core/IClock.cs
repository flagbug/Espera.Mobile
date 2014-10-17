using System;

namespace Espera.Mobile.Core
{
    public interface IClock
    {
        DateTimeOffset Now { get; }
    }

    public class Clock : IClock
    {
        public DateTimeOffset Now
        {
            get { return DateTimeOffset.Now; }
        }
    }
}
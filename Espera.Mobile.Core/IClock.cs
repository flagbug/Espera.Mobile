using System;

namespace Espera.Mobile.Core
{
    public interface IClock
    {
        DateTime Now { get; }
    }

    public class Clock : IClock
    {
        public DateTime Now
        {
            get { return DateTime.Now; }
        }
    }
}
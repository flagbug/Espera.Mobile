using System.ComponentModel;

namespace Espera.Mobile.Core
{
    internal class Linker
    {
        public Linker()
        {
            var boolConverter = new BooleanConverter();
            var stringConverter = new StringConverter();
            var enumConverter = new EnumConverter(null);
        }
    }
}
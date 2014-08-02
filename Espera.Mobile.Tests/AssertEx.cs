using System;
using System.Threading.Tasks;
using Xunit;

namespace Espera.Android.Tests
{
    public class AssertEx
    {
        public async static Task<T> ThrowsAsync<T>(Func<Task> testCode) where T : Exception
        {
            try
            {
                await testCode();
                Assert.Throws<T>(() => { }); // Use xUnit's default behavior.
            }

            catch (T exception)
            {
                return exception;
            }

            return null;
        }
    }
}
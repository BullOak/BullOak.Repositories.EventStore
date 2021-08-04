using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BullOak.Repositories.EventStore.Test.Integration.Contexts
{
    internal class Record
    {
        public static async Task<Exception> ExceptionAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }

        public static async Task<Exception> ExceptionAsync<T>(Func<Task<T>> function)
        {
            try
            {
                await function();
            }
            catch (Exception ex)
            {
                return ex;
            }

            return null;
        }
    }
}

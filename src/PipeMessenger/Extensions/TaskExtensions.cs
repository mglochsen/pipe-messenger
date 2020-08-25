using System;
using System.Threading.Tasks;

namespace PipeMessenger.Extensions
{
    internal static class TaskExtensions
    {
        public static async void Await(this Task task, Action<Exception> errorHandler = null, Action completedAction = null, bool continueOnCapturedContext = true)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));

            try
            {
                await task.ConfigureAwait(continueOnCapturedContext);
                completedAction?.Invoke();

            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
            }
        }
    }
}

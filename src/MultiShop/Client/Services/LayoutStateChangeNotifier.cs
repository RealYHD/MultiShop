using System;
using System.Threading.Tasks;

namespace MultiShop.Client.Services
{
    public class LayoutStateChangeNotifier
    {
        public event Func<Task> Notify;
        public async Task LayoutHasChanged() {
            await Notify?.Invoke();
        }
    }
}
using System.ComponentModel;
using Castle.DynamicProxy;

namespace System.Windows
{
    public abstract class ViewModel : INotifyPropertyChanged
    {
        static readonly ProxyGenerator proxyFactory = new ProxyGenerator();

        internal class PropertySetterInterceptor : StandardInterceptor
        {
            protected override void PostProceed(IInvocation invocation)
            {
                var method = invocation.Method;
                if (method.Name.StartsWith("set_"))
                {
                    var viewModel = invocation.InvocationTarget as ViewModel;
                    viewModel.OnPropertyChanged(propertyName: method.Name.Substring(4));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public static T Create<T>() where T : ViewModel, new()
        {
            return proxyFactory.CreateClassProxy<T>(new PropertySetterInterceptor());
        }
    }
}

using System.ComponentModel;
using Castle.DynamicProxy;
using System.Diagnostics;
using System.Collections.Generic;
using System;

namespace Wpf.Mvc
{
    public abstract class ViewModel : INotifyPropertyChanged
    {
        internal class PropertySetterInterceptor : StandardInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                invocation.Proceed();
                var method = invocation.Method;
                if (method.Name.StartsWith("set_"))
                {
                    Debug.Assert(method.IsSpecialName);
                    var viewModel = invocation.InvocationTarget as ViewModel;
                    viewModel.OnPropertyChanged(propertyName: method.Name.Substring(4));
                }
            }
        }

        static readonly ProxyGenerator proxyFactory = new ProxyGenerator();

        public static T Create<T>() where T : ViewModel, new()
        {
            return proxyFactory.CreateClassProxy<T>(new PropertySetterInterceptor());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        internal void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        //IDataErrorInfo
        readonly Dictionary<string, string> _erros = new Dictionary<string, string>();
        string IDataErrorInfo.Error { get { throw new NotImplementedException(); } }
        string IDataErrorInfo.this[string propertyName]
        {
            get
            {
                var result = "";
                this._erros.TryGetValue(propertyName, out result);
                return result;
            }
        }

        public void SetError(string propertyName, string message)
        {
            this._erros[propertyName] = message;
            this.OnPropertyChanged(propertyName);
        }
    }
}

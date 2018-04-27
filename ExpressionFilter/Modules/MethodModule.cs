#region

using System;
using System.Collections.Generic;
using ExpressionFilter.Contracts;

#endregion

namespace ExpressionFilter.Modules
{
    public abstract class MethodModule : IMethodModule
    {
        private readonly Dictionary<string, IMethod> _methods = new Dictionary<string, IMethod>();

        public IDictionary<string, IMethod> GetMethods()
        {
            Load();

            return _methods;
        }

        protected abstract void Load();
        
        protected void Register<T>(string name, T instance) where T : IMethod
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            if (_methods.ContainsKey(name))
                throw new InvalidOperationException("Duplicate method registration");

            _methods.Add(name, instance);
        }
    }
}
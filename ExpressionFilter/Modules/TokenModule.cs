#region

using System;
using System.Collections.Generic;
using ExpressionFilter.Contracts;

#endregion

namespace ExpressionFilter.Modules
{
    public abstract class TokenModule : ITokenModule
    {
        private readonly Dictionary<string, IToken> _tokens = new Dictionary<string, IToken>();

        public IDictionary<string, IToken> GetTokens()
        {
            Load();

            return _tokens;
        }

        protected abstract void Load();

        protected void Register<T>(string name, T instance) where T : IToken
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            if (_tokens.ContainsKey(name))
                throw new InvalidOperationException("Duplicate token registration");

            _tokens.Add(name, instance);
        }
    }
}
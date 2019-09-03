using System;

namespace Kelson.Common.Web
{
    public abstract class Option<T>
    {
        public static implicit operator Option<T> (T value) => new Some<T>(value);

        public static implicit operator Option<T> (bool value) => 
            value ? 
                (Option<T>)new Some<T>(default)
              : (Option<T>)new None<T>();

        public static implicit operator Option<T> (Exception e) => new Error<T, Exception>(e);        
    }

    public class Some<T> : Option<T>
    {
        public readonly T Message;

        public Some(T message) => this.Message = message;        
    }

    public class Error<T, TError> : Option<T>
    {
        public readonly TError Value;

        public Error(TError value) => Value = value;
    }

    public class None<T> : Option<T>
    {

    }
}

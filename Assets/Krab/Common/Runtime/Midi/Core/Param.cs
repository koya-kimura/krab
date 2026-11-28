using System;
using System.Collections.Generic;

namespace Krab.Common.Midi.Core
{
    public class Param<T>
    {
        public Param(T defaultValue)
        {
            Value = defaultValue;
        }

        public T Value { get; private set; }

        public event Action<T> Changed;

        public void SetValue(T value)
        {
            if (EqualityComparer<T>.Default.Equals(Value, value))
                return;

            Value = value;
            Changed?.Invoke(Value);
        }
    }
}

using System;
using System.Collections.Generic;

namespace Cleipnir.ObjectDB.Persistency
{
    public sealed class CircularDependencyException : Exception
    {
        public IEnumerable<object> CircularDependency { get; }
        
        public CircularDependencyException(IEnumerable<object> circularDependency) => CircularDependency = circularDependency;

        public CircularDependencyException(string message, IEnumerable<object> circularDependency) : base(message) 
            => CircularDependency = circularDependency;

        public CircularDependencyException(string message, Exception innerException, IEnumerable<object> circularDependency) 
            : base(message, innerException) => CircularDependency = circularDependency;
    }
}
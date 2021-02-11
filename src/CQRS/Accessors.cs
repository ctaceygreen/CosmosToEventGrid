using System;
using System.Collections.Generic;
using System.Text;

namespace CQRS
{
    public interface ICorrelationIdAccessor
    {
        string GetCorrelationId();
    }
    public interface ICausationIdAccessor
    {
        string GetCausationId();
    }
}

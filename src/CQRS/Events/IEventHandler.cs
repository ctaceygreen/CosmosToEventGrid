﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CQRS.Events
{
    public interface IEventHandler<T>
    {
        Task Handle(T evt);
    }
}

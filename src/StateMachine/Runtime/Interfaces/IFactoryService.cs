using System;

namespace Nk7.StateMachine
{
    public interface IStatesFactoryService<TService>
    {
        TService GetService(Type serviceType);
    }
}
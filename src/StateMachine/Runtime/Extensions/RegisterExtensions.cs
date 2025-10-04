using Nk7.Container;
using System;

namespace Nk7.StateMachine
{
    public static class RegisterExtensions
    {
        public static void RegisterStateMachine<TTrigger>(this IBaseDIService diService, IDIContainer container)
            where TTrigger : Enum
        {
            diService.RegisterFactory<IState<TTrigger>>(container);
            diService.RegisterTransient<IStateMachine<TTrigger>, StateMachine<TTrigger>>();
        }
    }
}
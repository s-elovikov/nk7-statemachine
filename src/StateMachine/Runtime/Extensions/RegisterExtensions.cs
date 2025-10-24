using System;

#if NK7_CONTAINER
using Nk7.Container;
#endif

namespace Nk7.StateMachine
{
    public static class RegisterExtensions
    {
#if NK7_CONTAINER
        public static void RegisterStateMachine<TTrigger>(this IBaseDIService diService, IDIContainer container)
            where TTrigger : Enum
        {
            diService.RegisterFactory<IState<TTrigger>>(container);
            diService.RegisterTransient<IStateMachine<TTrigger>, StateMachine<TTrigger>>();
        }
#endif
    }
}
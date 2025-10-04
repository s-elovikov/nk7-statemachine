using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Nk7.StateMachine
{
    public interface IState<TTrigger> where TTrigger : Enum
    {
        UniTask OnBeforeExitAsync(TTrigger currentState, TTrigger nextState, CancellationToken cancellationToken);
        UniTask OnExitAsync(TTrigger currentState, TTrigger nextState, CancellationToken cancellationToken);
    }
}
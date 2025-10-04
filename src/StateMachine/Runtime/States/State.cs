using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Nk7.StateMachine
{
    public class State<TTrigger, TPayload> : IPayloadedState<TTrigger, TPayload>, IDisposable
        where TTrigger : Enum
        where TPayload : IPayload
    {
        public virtual void Dispose() { }

        public virtual UniTask OnBeforeEnterAsync(TTrigger state, TPayload payload, CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask OnEnterAsync(TTrigger state, TPayload payload, CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask OnBeforeExitAsync(TTrigger currentState, TTrigger nextState, CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        public virtual UniTask OnExitAsync(TTrigger currentState, TTrigger nextState, CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }
    }
}
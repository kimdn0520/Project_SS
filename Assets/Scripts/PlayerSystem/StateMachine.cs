public class StateMachine
{
    private IState currentState;
    public string CurrentStateName => currentState?.GetType().Name;

    public void ChangeState(IState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }

    public void Update() => currentState?.Update();
    public void FixedUpdate() => currentState?.FixedUpdate();
}
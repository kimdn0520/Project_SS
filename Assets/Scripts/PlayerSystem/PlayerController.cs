using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform visualsTransform;

    public float MoveSpeed = 5f;

    public Animator Animator { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public Vector2 MoveInput { get; private set; }
    public StateMachine StateMachine { get; private set; }
    public bool IsFacingRight { get; private set; } = true;

    private Camera mainCamera;

    private void Awake()
    {
        Animator = visualsTransform.GetComponent<Animator>();
        //AnimationData = new PlayerAnimationData();
        //AnimationData.Initialize();

        Rb = GetComponent<Rigidbody2D>();
        //SkillManager = GetComponent<SkillManager>();
        StateMachine = new StateMachine();

        mainCamera = Camera.main;
    }

    private void Start()
    {
        //StateMachine.ChangeState(new PlayerIdleState(this, StateMachine));
    }

    private void Update()
    {
        HandleInput();

        StateMachine.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.FixedUpdate();
    }

    private void HandleInput()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        MoveInput = new Vector2(moveX, moveY).normalized; 

        // ... input
    }

    private void Flip()
    {
        //...
    }
}
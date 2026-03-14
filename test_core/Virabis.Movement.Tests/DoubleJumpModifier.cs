using System.Numerics;

namespace Virabis.Movement.Core.Modifiers;

/// <summary>
/// Double jump ability - allows a second jump while airborne.
/// Resets jump counter when on floor, applies jump force when in air with jump input.
/// </summary>
public sealed class DoubleJumpModifier : IMovementModifier
{
    private int _jumpCount = 0;
    private const int MaxJumps = 2;
    private readonly float _jumpForce;
    
    public DoubleJumpModifier(float jumpForce = 5.0f)
    {
        _jumpForce = jumpForce;
    }
    
    public bool IsExpired => false; // Never expires, always active
    
    public string DebugLabel => $"DoubleJump({_jumpCount}/{MaxJumps})";
    
    public Vector3 ModifyVelocity(Vector3 velocity, MovementContext context)
    {
        // Reset counter when on floor
        if (context.IsOnFloor)
        {
            _jumpCount = 0;
            return velocity;
        }
        
        // Apply jump force when in air with jump input and jumps remaining
        if (context.JumpRequested && _jumpCount < MaxJumps && !context.IsOnFloor)
        {
            _jumpCount++;
            return new Vector3(velocity.X, _jumpForce, velocity.Z);
        }
        
        return velocity;
    }
}

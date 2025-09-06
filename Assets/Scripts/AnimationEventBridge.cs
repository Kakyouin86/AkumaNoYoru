using UnityEngine;

public class AnimationEventBridge : MonoBehaviour
{
    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
    }

    public void FinishAttackIdle()
    {
        if (playerController != null)
            playerController.FinishAttackIdle();
    }

    public void FinishAttackCrouch()
    {
        if (playerController != null)
            playerController.FinishAttackCrouch();
    }

    public void FinishAttackJump()
    {
        if (playerController != null)
            playerController.FinishAttackJump();
    }
    
    public void FinishAttackJumpDown()
    {
        if (playerController != null)
            playerController.FinishAttackJumpDown();
    }

    public void FinishAlJumplAttacks()
    {
        if (playerController != null)
        {
            playerController.FinishAttackJump();
            playerController.FinishAttackJumpDown();
        }
    }   
}
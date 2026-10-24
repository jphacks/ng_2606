using System;
using UnityEngine.InputSystem;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// D の DeskStageControls（00_overview.md 4.7）ができるまでの仮の入力。
    /// アクションの名前と割り当ては同じ（Move：左スティックと WASD、Jump：下のボタンとスペース）。
    /// DeskStageControls ができたら、CharacterMotor の入力をそちらに入れ替えて、このファイルは消す。
    /// </summary>
    public class TemporaryControls : IDisposable
    {
        public InputAction Move { get; }
        public InputAction Jump { get; }

        public TemporaryControls()
        {
            Move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            Move.AddBinding("<Gamepad>/leftStick");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            Jump = new InputAction("Jump", InputActionType.Button);
            Jump.AddBinding("<Gamepad>/buttonSouth");
            Jump.AddBinding("<Keyboard>/space");
        }

        public void Enable()
        {
            Move.Enable();
            Jump.Enable();
        }

        public void Disable()
        {
            Move.Disable();
            Jump.Disable();
        }

        public void Dispose()
        {
            Move.Dispose();
            Jump.Dispose();
        }
    }
}

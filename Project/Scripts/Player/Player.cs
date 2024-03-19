namespace LandlessSkies.Core;

using Godot;
using SevenGame.Utility;


[Tool]
[GlobalClass]
public sealed partial class Player : Node {
	public const byte MaxPlayers = 2;
	public static readonly Player?[] Players = new Player[MaxPlayers];



	[Export] public byte PlayerId {
		get => _playerId;
		private set => SetPlayerId(value);
	}
	private byte _playerId;


	[Export] public CameraController3D CameraController { get; private set; } = null!;

	[Export] public Entity Entity { get; private set; } = null!;

	// [Export] public ControlDevice ControlDevice { get; private set; } = null!;



	public Player() : base() { }



	private void SetPlayerId(byte value) {
		if (this.IsInitializationSetterCall()) {
			_playerId = value;
			return;
		}

		UnsetPlayerId();

		if (Players[value] == null) {
			Players[value] = this;
		}
		_playerId = value;

		UpdateConfigurationWarnings();
	}

	private void FindFreeId() {
		// PlayerId is already set to 0 (default) or the PlayerId is already set to this Player.

		if (_playerId != 0 || Players[0] == this)
			return;

		byte index = (byte) System.Array.FindIndex(Players, p => p is null);
		SetPlayerId(index);
	}

	private void UnsetPlayerId() {
		if (Players[_playerId] == this) {
			Players[_playerId] = null;
		}
	}



	public override void _Process(double delta) {
		base._Process(delta);

		if (Engine.IsEditorHint())
			return;

		if (Entity is null || CameraController is null)
			return;

		Entity.HandleInput(new(
			InputManager.CurrentDevice,
			CameraController,
			Entity
		));
	}

	public override void _Ready() {
		base._Ready();

		FindFreeId();

		if (Engine.IsEditorHint())
			return;
	}

	public override void _ExitTree() {
		base._ExitTree();
		if (this.IsEditorExitTree())
			return;

		UnsetPlayerId();
	}


	public readonly struct InputInfo(InputDevice inputDevice, CameraController3D cameraController, Entity entity) {
		public readonly Entity Entity => entity;
		public readonly InputDevice InputDevice => inputDevice;
		public readonly CameraController3D CameraController => cameraController;


		public readonly void RawInputToGroundedMovement(Vector2 moveInput, out Basis camRotation, out Vector3 groundedMovement) {
			Vector3 camRight = CameraController.AbsoluteRotation.X;
			float localAlignment = Mathf.Ceil(Entity.Transform.Basis.Y.Dot(CameraController.LocalRotation.Y));
			Vector3 entityUp = Entity.Transform.Basis.Y * (localAlignment * 2f - 1f);
			Vector3 groundedCamForward = entityUp.Cross(camRight).Normalized();

			camRotation = Basis.LookingAt(groundedCamForward, entityUp);

			groundedMovement = camRotation * new Vector3(moveInput.X, 0, moveInput.Y).ClampMagnitude(1f);
		}
		public readonly void RawInputToCameraRelativeMovement(Vector2 moveInput, out Basis camRotation, out Vector3 cameraRelativeMovement) {
			camRotation = CameraController.AbsoluteRotation;

			cameraRelativeMovement = camRotation * new Vector3(moveInput.X, 0, moveInput.Y);
		}
	}
}